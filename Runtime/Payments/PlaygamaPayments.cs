using MirraGames.SDK.Common;
using Playgama;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using Logger = MirraGames.SDK.Common.Logger;

namespace MirraGames.SDK.Playgama {

    /// <summary>
    /// Playgama purchases. Consumables (see <see cref="PlaygamaPayments_Configuration"/>) are consumed once the save
    /// holding the granted product is stored, and a counter in the save tells a granted but unconsumed purchase apart
    /// from one that was paid for and never granted. Permanent products are restored while their supply count is 0.
    /// </summary>
    [Provider(typeof(IPayments))]
    public class PlaygamaPayments : CommonPayments {

        private const string GrantedUnconsumedPrefix = "playgama-granted-unconsumed-";
        private const float SaveTimeoutSeconds = 30.0f;
        private const float InitializationTimeoutSeconds = 10.0f;

        private bool isInitializationMarked = false;

        private readonly IEventDispatcher eventDispatcher;
        private readonly HashSet<string> consumableTags;
        private readonly Dictionary<string, ProductData> products = new();
        private readonly List<string> purchases = new();

        private readonly PlaygamaCallQueue queue;

        public PlaygamaPayments(PlaygamaPayments_Configuration configuration, IData data, IEventDispatcher eventDispatcher) : base(data) {
            this.eventDispatcher = eventDispatcher;
            queue = new PlaygamaCallQueue(this);
            consumableTags = configuration?.GetConsumableTags() ?? new HashSet<string>();
            Logger.CreateText(this, "Consumable product tags", Naming.Quote(string.Join(", ", consumableTags)));
            if (IsPaymentsAvailable == false) {
                SetInitialized();
                return;
            }
            eventDispatcher.Start += OnStart;
        }

        public override bool IsPaymentsAvailable => Bridge.payments.isSupported;

        private void OnStart() {
            eventDispatcher.StartCoroutine(InitializationTimeoutCoroutine());
            Enqueue(done => Bridge.payments.GetCatalog((isSuccess, catalog) => {
                try {
                    if (isSuccess && catalog != null) {
                        SetCatalog(catalog);
                    }
                    else {
                        Logger.CreateError(this, "Failed to get catalog");
                    }
                }
                catch (Exception exception) {
                    Logger.CreateError(this, nameof(Bridge.payments.GetCatalog), exception);
                }
                finally {
                    done();
                }
            }));
            Enqueue(done => Bridge.payments.GetPurchases((isSuccess, list) => {
                try {
                    if (isSuccess && list != null) {
                        SetPurchases(list);
                    }
                    else {
                        Logger.CreateError(this, "Failed to get purchases");
                    }
                }
                catch (Exception exception) {
                    Logger.CreateError(this, nameof(Bridge.payments.GetPurchases), exception);
                }
                finally {
                    MarkInitialized();
                    done();
                }
            }));
        }

        /// <summary>
        /// MirraSDK reports ready only when every provider is, so a catalog request the platform never answers
        /// must not hold the game; the catalog still fills in if the answer comes later.
        /// </summary>
        private IEnumerator InitializationTimeoutCoroutine() {
            yield return new WaitForSecondsRealtime(InitializationTimeoutSeconds);
            if (isInitializationMarked == false) {
                Logger.CreateWarning(this, $"catalog and purchases not received in {InitializationTimeoutSeconds} seconds, continuing without them");
                MarkInitialized();
            }
        }

        private void MarkInitialized() {
            if (isInitializationMarked) return;
            isInitializationMarked = true;
            SetInitialized();
        }

        private void SetCatalog(List<Dictionary<string, string>> catalog) {
            products.Clear();
            foreach (Dictionary<string, string> item in catalog) {
                if (item == null || !item.TryGetValue("id", out string productTag) || string.IsNullOrEmpty(productTag)) {
                    Logger.CreateError(this, "Catalog item without id skipped");
                    continue;
                }
                item.TryGetValue("priceValue", out string priceValue);
                item.TryGetValue("priceCurrencyCode", out string priceCurrency);
                if (!float.TryParse(priceValue, NumberStyles.Float, CultureInfo.InvariantCulture, out float priceFloat)) {
                    Logger.CreateError(this, $"Catalog item '{productTag}' has unreadable price '{priceValue}'");
                    priceFloat = 0.0f;
                }
                Logger.CreateText(this, $"Product ({productTag}, {priceValue}, {priceCurrency})");
                products[productTag] = new ProductData(productTag, priceFloat, priceCurrency ?? string.Empty);
            }
        }

        private void SetPurchases(List<Dictionary<string, string>> list) {
            purchases.Clear();
            foreach (Dictionary<string, string> purchase in list) {
                if (purchase != null && purchase.TryGetValue("id", out string productTag) && !string.IsNullOrEmpty(productTag)) {
                    purchases.Add(productTag);
                }
            }
            Logger.CreateText(this, "Purchases", Naming.Quote(string.Join(", ", purchases)));
        }

        protected override ProductData GetProductDataImpl(string productTag) {
            if (productTag != null && products.TryGetValue(productTag, out ProductData product)) {
                return product;
            }
            Logger.CreateWarning(this, $"product '{productTag}' is not in the catalog");
            return null;
        }

        protected override bool IsAlreadyPurchasedImpl(string productTag) {
            return purchases.Contains(productTag);
        }

        protected override void PurchaseImpl(string productTag, Action onSuccess, Action onError = null) {
            Enqueue(done => Bridge.payments.Purchase(productTag, (isSuccess, purchase) => {
                if (isSuccess == false) {
                    Logger.CreateText(this, $"purchase '{productTag}' failed or cancelled");
                    try {
                        onError?.Invoke();
                    }
                    catch (Exception exception) {
                        Logger.CreateError(this, nameof(PurchaseImpl), exception);
                    }
                    done();
                    return;
                }
                purchases.Add(productTag);
                bool isConsumable = IsConsumable(productTag);
                if (!TryGrant(productTag, onSuccess, isConsumable) || !isConsumable) {
                    done();
                    return;
                }
                AddGrantedUnconsumed(productTag, 1);
                ConsumeAfterSave(productTag, done);
            }));
        }

        protected override void RestorePurchasesImpl(Action<IRestoreData> onRestoreData) {
            Enqueue(done => Bridge.payments.GetPurchases((isSuccess, list) => {
                try {
                    if (isSuccess && list != null) {
                        SetPurchases(list);
                    }
                    else {
                        Logger.CreateError(this, "Failed to get purchases for restore");
                    }
                    IRestoreData restoreData = isSuccess && list != null
                        ? CreateRestoreData()
                        : new PlaygamaRestoreData(this, Array.Empty<string>(), new Dictionary<string, int>());
                    onRestoreData?.Invoke(restoreData);
                }
                catch (Exception exception) {
                    Logger.CreateError(this, nameof(RestorePurchasesImpl), exception);
                }
                finally {
                    done();
                }
            }));
        }

        private PlaygamaRestoreData CreateRestoreData() {
            Dictionary<string, int> pendingCounts = new();
            Dictionary<string, int> purchaseCounts = purchases.GroupBy(tag => tag).ToDictionary(group => group.Key, group => group.Count());
            // A counter left above zero for a product without open purchases would later hide
            // a purchase that was paid for and never granted, so every consumable is checked.
            foreach (string consumableTag in consumableTags) {
                if (!purchaseCounts.ContainsKey(consumableTag) && GetGrantedUnconsumed(consumableTag) > 0) {
                    SetGrantedUnconsumed(consumableTag, 0);
                }
            }
            foreach (KeyValuePair<string, int> purchaseGroup in purchaseCounts) {
                string productTag = purchaseGroup.Key;
                int purchaseCount = purchaseGroup.Value;
                if (IsConsumable(productTag)) {
                    // Granted units whose consume never reached the platform are only consumed again.
                    int grantedCount = Math.Min(GetGrantedUnconsumed(productTag), purchaseCount);
                    if (grantedCount != GetGrantedUnconsumed(productTag)) {
                        SetGrantedUnconsumed(productTag, grantedCount);
                    }
                    for (int index = 0; index < grantedCount; index++) {
                        Enqueue(done => ConsumeAfterSave(productTag, done));
                    }
                    if (purchaseCount > grantedCount) {
                        pendingCounts[productTag] = purchaseCount - grantedCount;
                    }
                }
                else if (GetSupplyCount(productTag) < 1) {
                    pendingCounts[productTag] = 1;
                }
            }
            foreach (KeyValuePair<string, int> pair in pendingCounts) {
                Logger.CreateText(this, $"product '{pair.Key}' pending '{pair.Value}' times");
            }
            return new PlaygamaRestoreData(this, purchases.ToArray(), pendingCounts);
        }

        internal void RestorePendingProduct(string productTag, int count, Action onProductRestore) {
            if (!IsConsumable(productTag)) {
                TryGrant(productTag, onProductRestore, false);
                return;
            }
            for (int index = 0; index < count; index++) {
                if (!TryGrant(productTag, onProductRestore, true)) {
                    return;
                }
                AddGrantedUnconsumed(productTag, 1);
                Enqueue(done => ConsumeAfterSave(productTag, done));
            }
        }

        private bool IsConsumable(string productTag) {
            return consumableTags.Contains(productTag);
        }

        /// <summary>
        /// Grants the product through the supply ledger. Permanent products count their supply,
        /// which is what tells a restore that they were already granted on this save.
        /// </summary>
        private bool TryGrant(string productTag, Action productSupply, bool isConsumable) {
            try {
                SupplyProduct(productTag, productSupply, countProduct: !isConsumable);
                return true;
            }
            catch (Exception exception) {
                Logger.CreateError(this, $"granting '{productTag}' failed", exception);
                return false;
            }
        }

        private void ConsumeAfterSave(string productTag, Action done) {
            WaitForSave((isSaved) => {
                if (isSaved == false) {
                    Logger.CreateWarning(this, $"consume of '{productTag}' postponed to the next restore, the save was not stored");
                    done();
                    return;
                }
                Bridge.payments.ConsumePurchase(productTag, (isSuccess, purchase) => {
                    try {
                        if (isSuccess) {
                            AddGrantedUnconsumed(productTag, -1);
                            purchases.Remove(productTag);
                            Logger.CreateText(this, $"consumed '{productTag}'");
                        }
                        else {
                            Logger.CreateError(this, $"consume of '{productTag}' failed, retried on the next restore");
                        }
                    }
                    finally {
                        done();
                    }
                });
            });
        }

        private void WaitForSave(Action<bool> onComplete) {
            bool isCompleted = false;
            void Complete(bool isSaved) {
                if (isCompleted) return;
                isCompleted = true;
                onComplete(isSaved);
            }
            if (data is PlaygamaData playgamaData) {
                eventDispatcher.StartCoroutine(SaveTimeoutCoroutine(() => Complete(false)));
                playgamaData.InvokeWhenSaved(Complete);
                return;
            }
            data.Save();
            Complete(true);
        }

        private IEnumerator SaveTimeoutCoroutine(Action onTimeout) {
            yield return new WaitForSecondsRealtime(SaveTimeoutSeconds);
            onTimeout();
        }

        private string GetGrantedUnconsumedKey(string productTag) {
            return GrantedUnconsumedPrefix + productTag;
        }

        private int GetGrantedUnconsumed(string productTag) {
            return data.GetInt(GetGrantedUnconsumedKey(productTag));
        }

        private void SetGrantedUnconsumed(string productTag, int count) {
            data.SetInt(GetGrantedUnconsumedKey(productTag), Math.Max(0, count));
        }

        private void AddGrantedUnconsumed(string productTag, int delta) {
            SetGrantedUnconsumed(productTag, GetGrantedUnconsumed(productTag) + delta);
        }

        private void Enqueue(Action<Action> operation) {
            queue.Enqueue(operation);
        }

    }

}

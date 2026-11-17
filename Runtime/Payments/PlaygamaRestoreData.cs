using MirraGames.SDK.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using Logger = MirraGames.SDK.Common.Logger;

namespace MirraGames.SDK.Playgama {

    /// <summary>
    /// Pending products of a Playgama restore: consumables paid for but never granted,
    /// and permanent products not granted on this save yet.
    /// </summary>
    public class PlaygamaRestoreData : IRestoreData {

        private readonly PlaygamaPayments payments;
        private readonly Dictionary<string, int> pendingCounts;

        internal PlaygamaRestoreData(PlaygamaPayments payments, string[] allPurchases, Dictionary<string, int> pendingCounts) {
            this.payments = payments;
            this.pendingCounts = pendingCounts;
            AllPurchases = allPurchases;
            PendingProducts = pendingCounts.Where(pair => pair.Value > 0).Select(pair => pair.Key).ToArray();
        }

        public string[] AllPurchases { get; }
        public string[] PendingProducts { get; }

        /// <summary>
        /// Grants every pending unit of the product once; repeated calls for the same product do nothing.
        /// </summary>
        public void RestoreProduct(string productTag, Action onProductRestore) {
            if (!pendingCounts.TryGetValue(productTag, out int count) || count <= 0) {
                Logger.CreateWarning(this, $"product '{productTag}' is not pending");
                return;
            }
            pendingCounts[productTag] = 0;
            Logger.CreateText(this, $"restore product '{productTag}' by '{count}' times");
            payments.RestorePendingProduct(productTag, count, onProductRestore);
        }

    }

}

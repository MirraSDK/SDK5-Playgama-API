using MirraGames.SDK.Common;
using Playgama;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Logger = MirraGames.SDK.Common.Logger;

namespace MirraGames.SDK.Playgama {

    [Provider(typeof(IData))]
    public class PlaygamaData : CommonData {

        private const string DataKey = "json-data";

        /// <summary>
        /// Delays before each repeated read. A read that still fails after these leaves the save unread,
        /// so writes stay disabled for the session instead of overwriting a save that may exist.
        /// </summary>
        private static readonly float[] ReadRetryDelays = { 1.0f, 2.0f, 4.0f };
        private const float ReadTimeoutSeconds = 5.0f;

        /// <summary>
        /// Delays before repeating a failed write when no newer save replaced it.
        /// </summary>
        private static readonly float[] WriteRetryDelays = { 2.0f, 5.0f, 10.0f };

        private bool isRead = false;
        private bool isWriteDisabled = false;
        private bool isWriting = false;
        private bool isDirty = false;
        private bool isRetryScheduled = false;
        private string latestJson = null;
        private int writeFailures = 0;

        // Every write request gets the next version; waiters complete once a version at least as new is stored.
        private int requestedVersion = 0;
        private int latestVersion = 0;
        private int storedVersion = 0;
        private readonly List<KeyValuePair<int, Action<bool>>> saveWaiters = new();

        public PlaygamaData(IEventDispatcher dispatcher) : base(dispatcher) {
            dispatcher.Start += OnStart;
        }

        /// <summary>
        /// Requests a save and reports true once the data as it is now has been stored by the platform,
        /// or false right away when saving is disabled for this session.
        /// </summary>
        internal void InvokeWhenSaved(Action<bool> onComplete) {
            if (isWriteDisabled) {
                onComplete?.Invoke(false);
                return;
            }
            saveWaiters.Add(new KeyValuePair<int, Action<bool>>(requestedVersion + 1, onComplete));
            Save();
        }

        private void NotifySaveWaiters() {
            for (int index = saveWaiters.Count - 1; index >= 0; index--) {
                KeyValuePair<int, Action<bool>> waiter = saveWaiters[index];
                if (waiter.Key > storedVersion) continue;
                saveWaiters.RemoveAt(index);
                try {
                    waiter.Value?.Invoke(true);
                }
                catch (Exception exception) {
                    Logger.CreateError(this, nameof(NotifySaveWaiters), exception);
                }
            }
        }

        private void OnStart() {
            eventDispatcher.StartCoroutine(ReadCoroutine());
        }

        private IEnumerator ReadCoroutine() {
            for (int attempt = 0; ; attempt++) {
                bool isDone = false;
                bool isSuccess = false;
                string json = null;
                ReadJson(isSuccess: (success, data) => {
                    if (isDone) return;
                    isSuccess = success;
                    json = data;
                    isDone = true;
                });
                // A read the platform never answers counts as failed, so the game is never held at startup.
                float deadline = Time.realtimeSinceStartup + ReadTimeoutSeconds;
                yield return new WaitUntil(() => isDone || Time.realtimeSinceStartup > deadline);
                isDone = true;
                if (isSuccess) {
                    Initialize(json);
                    yield break;
                }
                if (attempt >= ReadRetryDelays.Length) {
                    break;
                }
                Logger.CreateWarning(this, "Failed to read data, retrying in", ReadRetryDelays[attempt], "seconds");
                yield return new WaitForSecondsRealtime(ReadRetryDelays[attempt]);
            }
            isWriteDisabled = true;
            Logger.CreateError(this, "Failed to read data, playing on empty data with saving disabled for this session");
            Initialize(Naming.EmptyJson);
        }

        private void Initialize(string json) {
            isRead = true;
            // A player without a save gets null from the bridge.
            if (string.IsNullOrEmpty(json)) {
                json = Naming.EmptyJson;
            }
            try {
                ParseContainers(json);
            }
            catch (Exception exception) {
                // Malformed data cannot be recovered, so it is replaced by the next save.
                Logger.CreateError(this, "Failed to parse data, starting on empty data", exception);
                ParseContainers(Naming.EmptyJson);
            }
        }

        private void ReadJson(Action<bool, string> isSuccess) {
            Bridge.storage.Get(DataKey, (success, json) => {
                Logger.CreateText(this, nameof(ReadJson), success, json?.Length ?? 0, "characters");
                isSuccess?.Invoke(success, json);
            });
        }

        protected override void ReadJson(Action<string> jsonRequest) {
            ReadJson(isSuccess: (success, json) => {
                jsonRequest?.Invoke(success && !string.IsNullOrEmpty(json) ? json : Naming.EmptyJson);
            });
        }

        protected override void WriteJson(string json) {
            // Values set before the save is read are replaced by the read data, so writing them
            // would only overwrite the stored save with an incomplete one.
            if (isRead == false) {
                Logger.CreateWarning(this, "Write skipped, the save is not read yet");
                return;
            }
            if (isWriteDisabled) {
                Logger.CreateWarning(this, "Write skipped, the save was never read");
                return;
            }
            requestedVersion++;
            latestVersion = requestedVersion;
            latestJson = json;
            isDirty = true;
            Flush();
        }

        /// <summary>
        /// The bridge drops a second write to the same key while the first one is pending,
        /// so only one write is in flight and it always carries the newest data.
        /// </summary>
        private void Flush() {
            if (isWriting || isRetryScheduled || !isDirty) {
                return;
            }
            isWriting = true;
            isDirty = false;
            string json = latestJson;
            int version = latestVersion;
            Bridge.storage.Set(DataKey, json, (isSuccess) => {
                isWriting = false;
                if (isSuccess) {
                    writeFailures = 0;
                    storedVersion = Math.Max(storedVersion, version);
                    Logger.CreateText(this, nameof(WriteJson), json.Length, "characters");
                    NotifySaveWaiters();
                    Flush();
                    return;
                }
                Logger.CreateError(this, "Failed to write data");
                isDirty = true;
                if (writeFailures < WriteRetryDelays.Length) {
                    isRetryScheduled = true;
                    eventDispatcher.StartCoroutine(RetryCoroutine(WriteRetryDelays[writeFailures]));
                    writeFailures++;
                    return;
                }
                // Retries are spent; the next save request writes the newest data again.
                writeFailures = 0;
            });
        }

        private IEnumerator RetryCoroutine(float delay) {
            yield return new WaitForSecondsRealtime(delay);
            isRetryScheduled = false;
            Flush();
        }

    }

}

using MirraGames.SDK.Common;
using Playgama;
using System;
using System.Collections.Generic;
using System.Globalization;
using Logger = MirraGames.SDK.Common.Logger;

namespace MirraGames.SDK.Playgama {

    [Provider(typeof(IFlags))]
    public class PlaygamaFlags : IFlags {

        private readonly Dictionary<string, string> collection = new();
        private readonly Stack<Action> waiters = new();

        public PlaygamaFlags(IEventDispatcher dispatcher) {
            dispatcher.Start += OnStart;
        }

        public bool IsFlagsInitialized { get; private set; } = false;
        public bool IsFlagsAvailable { get; private set; } = false;

        public void WaitForFlags(Action onInitialized) {
            if (!IsFlagsInitialized) {
                waiters.Push(onInitialized);
                return;
            }
            onInitialized?.Invoke();
        }

        private void OnStart() {
            try {
                if (Bridge.remoteConfig.isSupported == false) {
                    Complete(false);
                    return;
                }
                Bridge.remoteConfig.Get((isSuccess, config) => {
                    collection.Clear();
                    if (isSuccess && config != null) {
                        foreach (KeyValuePair<string, string> pair in config) {
                            collection[pair.Key] = pair.Value;
                        }
                    }
                    else {
                        Logger.CreateError(this, "Failed to get remote config");
                    }
                    Complete(isSuccess && config != null);
                });
            }
            catch (Exception exception) {
                Logger.CreateError(this, exception);
                Complete(false);
            }
        }

        private void Complete(bool isAvailable) {
            IsFlagsAvailable = isAvailable;
            IsFlagsInitialized = true;
            waiters.PopInvokeAll();
        }

        public bool GetBool(string key, bool defaultValue = false) {
            return collection.TryGetValue(key, out string value) && bool.TryParse(value, out bool result) ? result : defaultValue;
        }

        public float GetFloat(string key, float defaultValue = 0) {
            return collection.TryGetValue(key, out string value)
                && float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float result) ? result : defaultValue;
        }

        public int GetInt(string key, int defaultValue = 0) {
            return collection.TryGetValue(key, out string value)
                && int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int result) ? result : defaultValue;
        }

        public string GetString(string key, string defaultValue = "") {
            return collection.TryGetValue(key, out string value) ? value : defaultValue;
        }

        public bool HasKey(string key) {
            return collection.ContainsKey(key);
        }

    }

}

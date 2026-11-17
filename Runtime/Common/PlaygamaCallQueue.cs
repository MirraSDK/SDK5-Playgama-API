using System;
using System.Collections.Generic;
using Logger = MirraGames.SDK.Common.Logger;

namespace MirraGames.SDK.Playgama {

    /// <summary>
    /// Bridge modules keep one callback per kind of call, so a second call made before the first one answers
    /// takes over its callback. Calls that share a module go through one queue and run one at a time;
    /// each operation receives a done action that it must invoke exactly once when the bridge answers.
    /// </summary>
    internal sealed class PlaygamaCallQueue {

        private readonly object owner;
        private readonly Queue<Action<Action>> operations = new();
        private bool isRunning = false;

        public PlaygamaCallQueue(object owner) {
            this.owner = owner;
        }

        public void Enqueue(Action<Action> operation) {
            operations.Enqueue(operation);
            RunNext();
        }

        private void RunNext() {
            if (isRunning || operations.Count == 0) {
                return;
            }
            isRunning = true;
            Action<Action> operation = operations.Dequeue();
            bool isDone = false;
            void Done() {
                if (isDone) return;
                isDone = true;
                isRunning = false;
                RunNext();
            }
            try {
                operation(Done);
            }
            catch (Exception exception) {
                Logger.CreateError(owner, nameof(PlaygamaCallQueue), exception);
                Done();
            }
        }

    }

}

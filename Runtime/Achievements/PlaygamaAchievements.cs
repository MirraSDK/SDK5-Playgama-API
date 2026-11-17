using MirraGames.SDK.Common;
using Playgama;
using Playgama.Modules.Leaderboards;
using Playgama.Modules.Platform;
using System;
using System.Collections.Generic;
using System.Globalization;
using Logger = MirraGames.SDK.Common.Logger;

namespace MirraGames.SDK.Playgama {

    [Provider(typeof(IAchievements))]
    public class PlaygamaAchievements : CommonAchievements {

        private readonly PlaygamaCallQueue leaderboardQueue;
        private readonly PlaygamaCallQueue achievementQueue;

        public PlaygamaAchievements() {
            leaderboardQueue = new PlaygamaCallQueue(this);
            achievementQueue = new PlaygamaCallQueue(this);
            SetInitialized();
        }

        /// <summary>
        /// Entries can only be read from in-game leaderboards; the playgama platform itself has
        /// leaderboards only when Bridge SaaS leaderboards are configured.
        /// </summary>
        private static bool IsEntriesAvailable => Bridge.leaderboards.type == LeaderboardType.InGame;

        private static bool IsScoreAvailable => Bridge.leaderboards.type != LeaderboardType.NotAvailable;

        protected override void GetLeaderboardImpl(string boardId, Action<Leaderboard> onLeaderboard) {
            if (!IsEntriesAvailable) {
                Logger.NotAvailableWarning(this, nameof(GetLeaderboardImpl));
                onLeaderboard?.Invoke(new Leaderboard());
                return;
            }
            GetEntries(boardId, (entries) => {
                Leaderboard leaderboard = new();
                List<PlayerScore> playerScores = new();
                foreach (Dictionary<string, string> entry in entries) {
                    playerScores.Add(new PlayerScore() {
                        displayName = GetValue(entry, "name"),
                        position = GetInt(entry, "rank"),
                        score = GetInt(entry, "score"),
                        profilePictureUrl = GetValue(entry, "photo")
                    });
                }
                leaderboard.players = playerScores.ToArray();
                onLeaderboard?.Invoke(leaderboard);
            });
        }

        protected override void GetScoreImpl(string boardId, Action<int> onScore) {
            if (!IsEntriesAvailable) {
                Logger.NotAvailableWarning(this, nameof(GetScoreImpl));
                onScore?.Invoke(0);
                return;
            }
            GetEntries(boardId, (entries) => {
                string playerId = Bridge.player.id;
                foreach (Dictionary<string, string> entry in entries) {
                    if (!string.IsNullOrEmpty(playerId) && GetValue(entry, "id") == playerId) {
                        onScore?.Invoke(GetInt(entry, "score"));
                        return;
                    }
                }
                onScore?.Invoke(0);
            });
        }

        private void GetEntries(string boardId, Action<List<Dictionary<string, string>>> onEntries) {
            leaderboardQueue.Enqueue(done => Bridge.leaderboards.GetEntries(boardId, (isSuccess, entries) => {
                Logger.CreateText(this, nameof(GetEntries), boardId, isSuccess);
                try {
                    onEntries?.Invoke(isSuccess && entries != null ? entries : new List<Dictionary<string, string>>());
                }
                catch (Exception exception) {
                    Logger.CreateError(this, nameof(GetEntries), exception);
                }
                finally {
                    done();
                }
            }));
        }

        protected override void SetScoreImpl(string boardId, int score) {
            if (!IsScoreAvailable) {
                Logger.NotAvailableWarning(this, nameof(SetScoreImpl));
                return;
            }
            leaderboardQueue.Enqueue(done => Bridge.leaderboards.SetScore(boardId, score, (isSuccess) => {
                Logger.CreateText(this, nameof(SetScoreImpl), boardId, score, isSuccess);
                done();
            }));
        }

        protected override void HappyTimeImpl() {
            Bridge.platform.SendMessage(PlatformMessage.PlayerGotAchievement);
        }

        protected override void UnlockImpl(string achievementId) {
            achievementQueue.Enqueue(done => Bridge.achievements.Unlock(achievementId, (isSuccess) => {
                Logger.CreateText(this, nameof(UnlockImpl), achievementId, isSuccess);
                done();
            }));
        }

        private static string GetValue(Dictionary<string, string> entry, string key) {
            return entry != null && entry.TryGetValue(key, out string value) ? value : string.Empty;
        }

        private static int GetInt(Dictionary<string, string> entry, string key) {
            string value = GetValue(entry, key);
            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int result)) {
                return result;
            }
            return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float floatResult) ? (int)floatResult : 0;
        }

    }

}

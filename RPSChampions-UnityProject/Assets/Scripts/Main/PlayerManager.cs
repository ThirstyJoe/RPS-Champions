namespace ThirstyJoe.RPSChampions
{
    using UnityEngine;
    using System.Collections.Generic;
    using PlayFab.ClientModels;
    using PlayFab;

    public class PlayerManager : Singleton<PlayerManager>
    {
        public static string QuickMatchId; // name of the room player is in
        public static string PlayerName; // name of player currently player (must be logged in)
        public static string OpponentName; // name of the opponent in quickmatch // TODO: save in game data
        public static string OpponentId; // playfab ID of the opponent in quickmatch // TODO: save in game data

        private static PlayerStats playerStats;

        public static PlayerStats PlayerStats
        {
            get
            {
                if (playerStats == null)
                    playerStats = new PlayerStats();

                if (PlayerPrefs.HasKey("screenName"))
                    playerStats.PlayerName = PlayerPrefs.GetString("screenName");

                return playerStats;
            }
        }

        public static List<string> GetPlayerStatList()
        {
            return new List<string>() {
                "ScissorsWins",
                "ScissorsDraws",
                "ScissorsLosses",
                "RockWins",
                "RockDraws",
                "RockLosses",
                "PaperWins",
                "PaperDraws",
                "PaperLosses",
                "Wins",
                "Draws",
                "Losses",
                "Rating"
            };
        }

        public static PlayerStatsData ConstructPlayerStats(List<StatisticValue> statValues)
        {
            statValues.Sort(new StatComparer());

            // update stats data in client
            PlayerStatsData stats = new PlayerStatsData();
            if (statValues.Count >= 13) // ensure stats have been initialized for this account
            {
                stats.TotalWLD.Draws = statValues[0].Value;
                stats.TotalWLD.Losses = statValues[1].Value;
                stats.PaperWLD.Draws = statValues[2].Value;
                stats.PaperWLD.Losses = statValues[3].Value;
                stats.PaperWLD.Wins = statValues[4].Value;
                stats.Rating = statValues[5].Value;
                stats.RockWLD.Draws = statValues[6].Value;
                stats.RockWLD.Losses = statValues[7].Value;
                stats.RockWLD.Wins = statValues[8].Value;
                stats.ScissorsWLD.Draws = statValues[9].Value;
                stats.ScissorsWLD.Losses = statValues[10].Value;
                stats.ScissorsWLD.Wins = statValues[11].Value;
                stats.TotalWLD.Wins = statValues[12].Value;
            }
            return stats;
        }

        public static void UpdatePlayerStats()
        {
            PlayFabClientAPI.GetPlayerStatistics(
                new GetPlayerStatisticsRequest { StatisticNames = GetPlayerStatList() },
                result =>
                {
                    PlayerManager.PlayerStats.data = ConstructPlayerStats(result.Statistics);
                },
                error => Debug.LogError(error.GenerateErrorReport())
            );
        }

        public class StatComparer : IComparer<StatisticValue>
        {
            public int Compare(StatisticValue first, StatisticValue second)
            {
                if (first != null && second != null)
                {
                    // We can compare both properties.
                    return first.StatisticName.CompareTo(second.StatisticName);
                }

                if (first == null && second == null)
                {
                    // We can't compare any properties, so they are essentially equal.
                    return 0;
                }

                if (first != null)
                {
                    // Only the first instance is not null, so prefer that.
                    return -1;
                }

                // Only the second instance is not null, so prefer that.
                return 1;
            }
        }
    }
}

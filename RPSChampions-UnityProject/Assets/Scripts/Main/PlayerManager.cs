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

        public static void UpdatePlayerStats()
        {
            List<string> statNames = new List<string>() {
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


            PlayFabClientAPI.GetPlayerStatistics(
                new GetPlayerStatisticsRequest { StatisticNames = statNames },
                result =>
                {
                    // for predictable ordering
                    result.Statistics.Sort(new StatComparer());

                    // update stats data in client
                    PlayerStatsData stats = new PlayerStatsData();
                    stats.TotalWLD.Draws = result.Statistics[0].Value;
                    stats.TotalWLD.Losses = result.Statistics[1].Value;
                    stats.PaperWLD.Draws = result.Statistics[2].Value;
                    stats.PaperWLD.Losses = result.Statistics[3].Value;
                    stats.PaperWLD.Wins = result.Statistics[4].Value;
                    stats.Rating = result.Statistics[5].Value;
                    stats.RockWLD.Draws = result.Statistics[6].Value;
                    stats.RockWLD.Losses = result.Statistics[7].Value;
                    stats.RockWLD.Wins = result.Statistics[8].Value;
                    stats.ScissorsWLD.Draws = result.Statistics[9].Value;
                    stats.ScissorsWLD.Losses = result.Statistics[10].Value;
                    stats.ScissorsWLD.Wins = result.Statistics[11].Value;
                    stats.TotalWLD.Wins = result.Statistics[12].Value;

                    PlayerManager.PlayerStats.data = stats;
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

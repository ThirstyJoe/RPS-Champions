namespace ThirstyJoe.RPSChampions
{
    using UnityEngine;
    using System.Collections.Generic;
    using PlayFab.ClientModels;
    using PlayFab;

    public class PlayerManager : Singleton<PlayerManager>
    {
        public static string QuickMatchId; // name of the room player is in
        public static string OpponentName; // name of the opponent // TODO: save in game data
        public static string OpponentId; // playfab ID of the opponent // TODO: save in game data

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
            };

            PlayFabClientAPI.GetPlayerStatistics(
                new GetPlayerStatisticsRequest { StatisticNames = statNames },
                result =>
                {
                    // update stats data in client
                    PlayerStatsData stats = new PlayerStatsData();
                    stats.ScissorsWLD.Wins = result.Statistics[0].Value;
                    stats.ScissorsWLD.Draws = result.Statistics[1].Value;
                    stats.ScissorsWLD.Losses = result.Statistics[2].Value;
                    stats.RockWLD.Wins = result.Statistics[3].Value;
                    stats.RockWLD.Draws = result.Statistics[4].Value;
                    stats.RockWLD.Losses = result.Statistics[5].Value;
                    stats.PaperWLD.Wins = result.Statistics[6].Value;
                    stats.PaperWLD.Draws = result.Statistics[7].Value;
                    stats.PaperWLD.Losses = result.Statistics[8].Value;
                    stats.TotalWLD.Wins = result.Statistics[9].Value;
                    stats.TotalWLD.Draws = result.Statistics[10].Value;
                    stats.TotalWLD.Losses = result.Statistics[11].Value;
                    PlayerManager.PlayerStats.data = stats;
                },
                error => Debug.LogError(error.GenerateErrorReport())
            );
        }

    }
}

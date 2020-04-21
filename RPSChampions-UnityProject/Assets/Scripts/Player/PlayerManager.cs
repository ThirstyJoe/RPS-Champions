namespace ThirstyJoe.RPSChampions
{
    using UnityEngine;
    public class PlayerManager : Singleton<PlayerManager>
    {
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
    }
}

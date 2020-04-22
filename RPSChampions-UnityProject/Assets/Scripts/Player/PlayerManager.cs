namespace ThirstyJoe.RPSChampions
{
    using UnityEngine;
    public class PlayerManager : Singleton<PlayerManager>
    {
        public static string Room; // name of the room player is in
        public static string OpponentName; // name of the opponent // TODO: save in game data

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

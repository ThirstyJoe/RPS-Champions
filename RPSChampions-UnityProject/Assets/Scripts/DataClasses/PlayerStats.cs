namespace ThirstyJoe.RPSChampions
{
    using System.Linq;
    using System;

    [Serializable]
    public enum Weapon
    {
        Rock,
        Paper,
        Scissors,
        None,
    }

    public class PlayerStatsData
    {
        public int wins = 0;
        public int totalGames = 0;
        public int totalRock = 0;
        public int totalPaper = 0;
        public int totalScissors = 0;
        public int winsRock = 0;
        public int winsPaper = 0;
        public int winsScissors = 0;
    }

    public class PlayerStats
    {
        private PlayerStatsData data = new PlayerStatsData();
        private string playerName;

        public PlayerStats()
        {
            playerName = RandomGuestName();
        }
        public PlayerStats(string name)
        {
            playerName = name;
        }

        public string PlayerName
        {
            get
            {
                return playerName;
            }
            set
            {
                playerName = value;
            }
        }

        public Weapon FavoriteWeapon
        {
            get
            {
                if ((data.totalRock == data.totalPaper) && (data.totalPaper == data.totalScissors))
                {
                    return Weapon.None;
                }
                int[] weaponTotals = { data.totalRock, data.totalPaper, data.totalScissors };
                var max = weaponTotals.Select((n, i) => (Number: n, Index: i)).Max();
                return (Weapon)Enum.ToObject(typeof(Weapon), max);
            }
        }

        public int Wins
        {
            get
            {
                return data.wins;
            }
        }

        public int Losses
        {
            get
            {
                return data.totalGames - data.wins;
            }
        }


        private string RandomGuestName()
        {
            var v = Enum.GetValues(typeof(GuestName));
            var name = (GuestName)v.GetValue(new Random().Next(v.Length));
            return name.ToString();
        }
    }
}
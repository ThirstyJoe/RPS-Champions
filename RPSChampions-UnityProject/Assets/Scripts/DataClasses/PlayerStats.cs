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

    [Serializable]
    public class WinLoseDrawStats
    {
        public int Wins = 0;
        public int Losses = 0;
        public int Draws = 0;

        public int Total { get { return Wins + Losses + Draws; } }

        public string GetReadout(bool addKey = true)
        {
            const string seperation = " - ";
            string toRet = Wins + seperation + Losses + seperation + Draws;
            if (addKey)
                toRet += "    Wins Losses Draws";
            return toRet;
        }

        public int addWin() { return ++Wins; }
        public int addLoss() { return ++Losses; }
        public int addDraw() { return ++Draws; }
    }

    public class PlayerStatsData
    {
        public WinLoseDrawStats TotalWLD = new WinLoseDrawStats();
        public WinLoseDrawStats RockWLD = new WinLoseDrawStats();
        public WinLoseDrawStats PaperWLD = new WinLoseDrawStats();
        public WinLoseDrawStats ScissorsWLD = new WinLoseDrawStats();
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
                if ((data.RockWLD.Total == data.PaperWLD.Total) && (data.PaperWLD.Total == data.ScissorsWLD.Total))
                {
                    return Weapon.None;
                }
                int[] weaponTotals = { data.RockWLD.Total, data.PaperWLD.Total, data.ScissorsWLD.Total };
                var max = weaponTotals.Select((n, i) => (Number: n, Index: i)).Max();
                return (Weapon)Enum.ToObject(typeof(Weapon), max);
            }
        }

        public int Wins
        {
            get
            {
                return data.TotalWLD.Wins;
            }
        }

        public int Losses
        {
            get
            {
                return data.TotalWLD.Losses;
            }
        }

        public int Draws
        {
            get
            {
                return data.TotalWLD.Draws;
            }
        }

        public int TotalGames
        {
            get
            {
                return data.TotalWLD.Total;
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
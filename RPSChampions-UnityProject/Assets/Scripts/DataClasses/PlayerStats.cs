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
        public int Rating;
        public WinLoseDrawStats TotalWLD = new WinLoseDrawStats();
        public WinLoseDrawStats RockWLD = new WinLoseDrawStats();
        public WinLoseDrawStats PaperWLD = new WinLoseDrawStats();
        public WinLoseDrawStats ScissorsWLD = new WinLoseDrawStats();
    }

    public class PlayerStats
    {
        public PlayerStatsData data = new PlayerStatsData();
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
                return (Weapon)Enum.ToObject(typeof(Weapon), max.Index);
            }
        }


        public int Rating
        {
            get
            {
                return data.Rating;
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

        public float WinRate
        {
            get
            {
                if (TotalGames == 0)
                    return 0F;
                return (float)Wins / (float)TotalGames;
            }
        }


        public float RockWinRate
        {
            get
            {
                if (data.RockWLD.Total - data.RockWLD.Draws == 0)
                    return 0F;
                return (float)data.RockWLD.Wins / (float)(data.RockWLD.Total - data.RockWLD.Draws);
            }
        }

        public float PaperWinRate
        {
            get
            {
                if (data.PaperWLD.Total - data.PaperWLD.Draws == 0)
                    return 0F;
                return (float)data.PaperWLD.Wins / (float)(data.PaperWLD.Total - data.PaperWLD.Draws);
            }
        }

        public float ScissorsWinRate
        {
            get
            {
                if (data.ScissorsWLD.Total - data.ScissorsWLD.Draws == 0)
                    return 0F;
                return (float)data.ScissorsWLD.Wins / (float)(data.ScissorsWLD.Total - data.ScissorsWLD.Draws);
            }
        }

        public string GetReadout()
        {
            return
                "Rating\t\t" + PlayerManager.PlayerStats.Rating + "\n\n" +
                "Favors\t\t" + FavoriteWeapon + "\n\n" +

                "Wins\t\t" + PlayerManager.PlayerStats.Wins + "\n" +
                "Losses\t\t" + PlayerManager.PlayerStats.Losses + "\n" +
                "Draws\t\t" + PlayerManager.PlayerStats.Draws + "\n" +
                "Games\t\t" + PlayerManager.PlayerStats.TotalGames + "\n\n" +

                "Win Rates\n" +
                "Rock\t\t" + String.Format("{0:p0}", PlayerManager.PlayerStats.RockWinRate) + "\n" +
                "Paper\t\t" + String.Format("{0:p0}", PlayerManager.PlayerStats.PaperWinRate) + "\n" +
                "Scissors\t" + String.Format("{0:p0}", PlayerManager.PlayerStats.ScissorsWinRate);
        }

        private string RandomGuestName()
        {
            var v = Enum.GetValues(typeof(GuestName));
            var name = (GuestName)v.GetValue(new Random().Next(v.Length));
            return name.ToString();
        }
    }

    public class PlayerStatsBrief
    {
        public string PlayerName;
        public string FavoriteWeapon;
        public int Wins;
        public int Losses;


        public PlayerStatsBrief(string playerName, string favors, int wins, int losses)
        {
            PlayerName = playerName;
            FavoriteWeapon = favors;
            Wins = wins;
            Losses = losses;
        }
    }
}
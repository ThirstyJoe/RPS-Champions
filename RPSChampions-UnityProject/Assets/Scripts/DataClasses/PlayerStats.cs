namespace ThirstyJoe.RPSChampions
{
    using System.Linq;
    using System;
    using UnityEngine;


    [Serializable]
    public enum WLD
    {
        Win,
        Lose,
        Draw,
        None
    }

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

        public WinLoseDrawStats() { }

        public WinLoseDrawStats(int wins, int losses, int draws)
        {
            Wins = wins;
            Losses = losses;
            Draws = draws;
        }

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

    public class PlayerStatsFromServer
    {
        public int ScissorsWins;
        public int ScissorsDraws;
        public int ScissorsLosses;
        public int RockWins;
        public int RockDraws;
        public int RockLosses;
        public int PaperWins;
        public int PaperDraws;
        public int PaperLosses;
        public int Wins;
        public int Draws;
        public int Losses;
        public int Rating;

        public string ToJSON()
        {
            return JsonUtility.ToJson(this);
        }
        public static PlayerStatsFromServer CreateFromJSON(string jsonString)
        {
            return JsonUtility.FromJson<PlayerStatsFromServer>(jsonString);
        }
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

        public PlayerStats(PlayerStatsFromServer serverData, string name)
        {
            playerName = name;
            data.Rating = serverData.Rating;
            data.TotalWLD = new WinLoseDrawStats(serverData.Wins, serverData.Losses, serverData.Draws);
            data.RockWLD = new WinLoseDrawStats(serverData.RockWins, serverData.RockLosses, serverData.RockDraws);
            data.PaperWLD = new WinLoseDrawStats(serverData.PaperWins, serverData.PaperLosses, serverData.PaperDraws);
            data.ScissorsWLD = new WinLoseDrawStats(serverData.ScissorsWins, serverData.ScissorsLosses, serverData.ScissorsDraws);
        }
        public PlayerStats(PlayerStatsData _data)
        {
            data = _data;
        }
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
                "Rating\t\t" + Rating + "\n\n" +
                "Favors\t\t" + FavoriteWeapon + "\n\n" +

                "Wins\t\t" + Wins + "\n" +
                "Losses\t\t" + Losses + "\n" +
                "Draws\t\t" + Draws + "\n" +
                "Games\t\t" + TotalGames + "\n\n" +

                "Win Rates\n" +
                "Rock\t\t" + String.Format("{0:p0}", RockWinRate) + "\n" +
                "Paper\t\t" + String.Format("{0:p0}", PaperWinRate) + "\n" +
                "Scissors\t" + String.Format("{0:p0}", ScissorsWinRate);
        }

        private string RandomGuestName()
        {
            var v = Enum.GetValues(typeof(GuestName));
            var name = (GuestName)v.GetValue(new System.Random().Next(v.Length));
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
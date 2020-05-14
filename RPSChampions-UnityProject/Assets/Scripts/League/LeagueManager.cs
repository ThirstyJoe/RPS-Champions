namespace ThirstyJoe.RPSChampions
{
    using UnityEngine;
    using System.Collections.Generic;


    public enum LeagueType
    {
        Custom,
        Rated
    }

    public class LeagueSettings
    {
        public string LeagueType;
        public LeagueSettings(LeagueType type)
        {
            LeagueType = type.ToString();
        }
    }

    public class LeaguePlayer
    {
        public string PlayerName;
        public int Wins;
        public int Losses;
        public int Draws;
        public int Rating;
        public ScheduledMatch[] Schedule;
    }

    public class ScheduledMatch
    {
        public int DateTime;
        public string OpponentID;
        public string OpponentName;
    }

    public class League
    {
        public LeagueSettings LeagueSettings;
        public string LeagueName = "Unnamed";
        public LeaguePlayer[] LeaguePlayerList;

        public League(string name, LeagueSettings settings)
        {
            LeagueName = name;
            LeagueSettings = settings;
        }
    }

    public class LeagueManager : Singleton<PlayerManager>
    {
        public static LeagueSettings leagueSettings;

        public static void NewCustomLeague()
        {
            NewLeague(LeagueType.Custom);
        }
        public static void NewRatedLeague()
        {
            NewLeague(LeagueType.Rated);
        }
        public static void NewLeague(LeagueType leagueType)
        {
            leagueSettings = new LeagueSettings(leagueType);
        }

        public static TitleDescriptionPair[] GetCurrentLeagues()
        {
            return FakePlayerList();
            //return new TitleDescriptionPair[0];
        }

        public static TitleDescriptionPair[] GetLeagueHistory()
        {
            return new TitleDescriptionPair[0];
        }

        public static TitleDescriptionPair[] GetOpenLeagues()
        {
            return new TitleDescriptionPair[0];
        }

        private static TitleDescriptionPair[] FakePlayerList()
        {
            return new TitleDescriptionPair[]{
                new TitleDescriptionPair("League1",  "0-0-0"),
                new TitleDescriptionPair("League2",  "0-0-0"),
                new TitleDescriptionPair("League3",  "0-0-0"),
                new TitleDescriptionPair("League4",  "0-0-0"),
                new TitleDescriptionPair("League5",  "0-0-0"),
                new TitleDescriptionPair("League6",  "0-0-0"),
                new TitleDescriptionPair("League7",  "0-0-0"),
                new TitleDescriptionPair("League8",  "0-0-0"),
                new TitleDescriptionPair("League9",  "0-0-0"),
                new TitleDescriptionPair("League10", "0-0-0"),
                new TitleDescriptionPair("League11", "0-0-0"),
                new TitleDescriptionPair("League12", "0-0-0"),
                new TitleDescriptionPair("League13", "0-0-0"),
                new TitleDescriptionPair("League14", "0-0-0"),
                new TitleDescriptionPair("League15", "0-0-0"),
                new TitleDescriptionPair("League16",  "0-0-0"),
                new TitleDescriptionPair("League17",  "0-0-0"),
                new TitleDescriptionPair("League3",  "0-0-0"),
                new TitleDescriptionPair("League4",  "0-0-0"),
                new TitleDescriptionPair("League5",  "0-0-0"),
                new TitleDescriptionPair("League6",  "0-0-0"),
                new TitleDescriptionPair("League7",  "0-0-0"),
                new TitleDescriptionPair("League8",  "0-0-0"),
                new TitleDescriptionPair("League9",  "0-0-0"),
                new TitleDescriptionPair("League10", "0-0-0"),
                new TitleDescriptionPair("League11", "0-0-0"),
                new TitleDescriptionPair("League12", "0-0-0"),
                new TitleDescriptionPair("League13", "0-0-0"),
                new TitleDescriptionPair("League14", "0-0-0"),
                new TitleDescriptionPair("League15", "0-0-0"),
            };
        }
    }
}

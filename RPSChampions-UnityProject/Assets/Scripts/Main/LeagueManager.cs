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
        public LeagueType Type;
        public LeagueSettings(LeagueType type)
        {
            Type = type;
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
            return new TitleDescriptionPair[0];
        }

        public static TitleDescriptionPair[] GetLeagueHistory()
        {
            return new TitleDescriptionPair[0];
        }

        public static TitleDescriptionPair[] GetOpenLeagues()
        {
            return new TitleDescriptionPair[0];
        }
    }
}

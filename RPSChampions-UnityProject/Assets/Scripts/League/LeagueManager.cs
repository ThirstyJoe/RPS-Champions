namespace ThirstyJoe.RPSChampions
{
    using UnityEngine;
    using System.Collections.Generic;
    using PlayFab;
    using PlayFab.ClientModels;
    using System.Linq;

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

        public string ToJSON()
        {
            return JsonUtility.ToJson(this);
        }
        public static LeagueSettings CreateFromJSON(string jsonString)
        {
            return JsonUtility.FromJson<LeagueSettings>(jsonString);
        }
    }

    // server saves this data in TitleData so it can be easily looked up when populating league lists
    public class LeagueInfo
    {
        public string Status;
        public string LeagueSettingsJSON;
        public string LeagueName;
        public string HostName;
        public string HostId;

        public string ToJSON()
        {
            return JsonUtility.ToJson(this);
        }
        public static LeagueInfo CreateFromJSON(string jsonString)
        {
            return JsonUtility.FromJson<LeagueInfo>(jsonString);
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
        public string LeagueHost = "NoHost";
        public LeaguePlayer[] LeaguePlayerList;
        public string Key = "";

        public League(string name, string host, LeagueSettings settings)
        {
            LeagueName = name;
            LeagueSettings = settings;
            LeagueHost = host;
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

        public delegate void GetLeaguesCallBack(List<TitleDescriptionPair> leagues);
        public static void GetCurrentLeagues(GetLeaguesCallBack callback)
        {
            List<string> userDataKeys = new List<string>() { "CurrentLeagues" };
            List<string> leagueKeys = new List<string>();
            List<TitleDescriptionPair> toRet = new List<TitleDescriptionPair>();

            // get list of league keys from PlayerData
            PlayFabClientAPI.GetUserData(new GetUserDataRequest()
            {
                Keys = userDataKeys
            },
            result =>
            {
                // validate this key before getting keys from JSON
                if (result.Data.ContainsKey("CurrentLeagues"))
                {
                    // parse LeagueIds from JSON into a list of string 
                    var leagueListJSON = result.Data["CurrentLeagues"].Value;
                    var leagueKeysArray = leagueListJSON.Split('"').Where((item, index) => index % 2 != 0);
                    leagueKeys = new List<string>(leagueKeysArray);
                }

                // nested server calls, i know this is ugly!
                PlayFabClientAPI.GetTitleData(new GetTitleDataRequest()
                {
                    Keys = leagueKeys
                },
                titleResult =>
                {
                    foreach (var key in leagueKeys)
                    {
                        if (titleResult.Data.ContainsKey(key))
                        {
                            LeagueInfo leagueInfo = LeagueInfo.CreateFromJSON(titleResult.Data[key]);
                            if (leagueInfo.Status != "Complete")
                            {
                                toRet.Add(new TitleDescriptionPair(leagueInfo.LeagueName, "Host: " + leagueInfo.HostName));
                            }
                        }
                    }
                    callback(toRet);
                },
                RPSCommon.OnPlayFabError
                );
            },
            RPSCommon.OnPlayFabError
            );
        }

        public static void GetLeagueHistory(GetLeaguesCallBack callback)
        {
            List<string> userDataKeys = new List<string>() { "FinishedLeagues" };
            List<string> leagueKeys = new List<string>();
            List<TitleDescriptionPair> toRet = new List<TitleDescriptionPair>();

            // get list of league keys from PlayerData
            PlayFabClientAPI.GetUserData(new GetUserDataRequest()
            {
                Keys = userDataKeys
            },
            result =>
            {
                // validate this key before getting keys from JSON
                if (result.Data.ContainsKey("FinishedLeagues"))
                {
                    // parse LeagueIds from JSON into a list of string 
                    var leagueListJSON = result.Data["FinishedLeagues"].Value;
                    var leagueKeysArray = leagueListJSON.Split('"').Where((item, index) => index % 2 != 0);
                    leagueKeys = new List<string>(leagueKeysArray);
                }

                // nested PlayFab call to get tTitleData using keys returned from UserData
                PlayFabClientAPI.GetTitleData(new GetTitleDataRequest()
                {
                    Keys = leagueKeys
                },
                titleResult =>
                {
                    foreach (var key in leagueKeys)
                    {
                        if (titleResult.Data.ContainsKey(key))
                        {
                            LeagueInfo leagueInfo = LeagueInfo.CreateFromJSON(titleResult.Data[key]);
                            if (leagueInfo.Status == "Complete")
                            {
                                toRet.Add(new TitleDescriptionPair(leagueInfo.LeagueName, "Host: " + leagueInfo.HostName));
                            }
                        }
                    }
                    callback(toRet);
                },
                RPSCommon.OnPlayFabError
                );
            },
            RPSCommon.OnPlayFabError
            );
        }

        public static void GetOpenLeagues(GetLeaguesCallBack callback)
        {
            List<string> leagueKeys = new List<string>();
            List<TitleDescriptionPair> toRet = new List<TitleDescriptionPair>();

            // nested PlayFab call to get tTitleData using keys returned from UserData
            PlayFabClientAPI.GetTitleData(new GetTitleDataRequest()
            { }, // don't provide keys in order to get all league entries
            titleResult =>
            {
                foreach (var entry in titleResult.Data.Values)
                {
                    // TODO: if we add other kinds of data to title data, we need to check for prefix "League"
                    LeagueInfo leagueInfo = LeagueInfo.CreateFromJSON(entry);
                    if (leagueInfo.Status == "Open")
                    {
                        toRet.Add(new TitleDescriptionPair(leagueInfo.LeagueName, "Host: " + leagueInfo.HostName));
                    }
                }
                callback(toRet);
            },
            RPSCommon.OnPlayFabError
            );
        }

        private static List<TitleDescriptionPair> FakePlayerList()
        {
            List<TitleDescriptionPair> toRet = new List<TitleDescriptionPair>();
            for (int i = 0; i < 30; i++)
            {
                toRet.Add(new TitleDescriptionPair("League_" + (i + 1).ToString(), "0-0-0"));
            }
            return toRet;
        }
    }
}

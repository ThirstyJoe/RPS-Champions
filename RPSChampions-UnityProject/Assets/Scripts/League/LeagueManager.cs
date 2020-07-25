namespace ThirstyJoe.RPSChampions
{
    using UnityEngine;
    using System.Collections.Generic;
    using PlayFab;
    using PlayFab.ClientModels;
    using System.Linq;
    using PlayFab.Json;
    using System;
    using Photon.Pun;
    using ExitGames.Client.Photon;
    using Photon.Realtime;

    public enum LeagueType
    {
        Custom,
        Rated
    }

    public class LeagueSettings
    {
        public string LeagueType; // rated or not rated
        public int MatchCount; // number of matches per player
        public int RoundDuration; // time in seconds between rounds of play
        public LeagueSettings(LeagueType type)
        {
            LeagueType = type.ToString();
        }
        public LeagueSettings(LeagueType type, int matchCount, int roundDuration)
        {
            LeagueType = type.ToString();
            MatchCount = matchCount;
            RoundDuration = roundDuration;
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
    [System.Serializable]
    public class LeagueInfo
    {
        public string Status;
        public string Settings;
        public string Name;
        public string HostName;
        public LeagueSettings LeagueSettings
        {
            get
            {
                return LeagueSettings.CreateFromJSON(Settings);
            }
        }

        public string ToJSON()
        {
            return JsonUtility.ToJson(this);
        }
        public static LeagueInfo CreateFromJSON(string jsonString)
        {
            return JsonUtility.FromJson<LeagueInfo>(jsonString);
        }
    }


    [System.Serializable]
    public class LeaguePlayerStats
    {
        public string PlayerName;
        public string PlayerId;
        public int Wins;
        public int Losses;
        public int Draws;
        public int Rating;

        public int WLDScore
        {
            get
            {
                return (Wins * 3) + Draws;
            }
        }

        public LeaguePlayerStats(string name, string id)
        {
            PlayerName = name;
            PlayerId = id;
            Rating = PlayerManager.PlayerStats.Rating;
        }

        public string ToJSON()
        {
            return JsonUtility.ToJson(this);
        }
        public static LeaguePlayerStats CreateFromJSON(string jsonString)
        {
            return JsonUtility.FromJson<LeaguePlayerStats>(jsonString);
        }
    }

    [System.Serializable]
    public class MatchTurn
    {
        public int DateTime;
        public int Round;
        public string MatchID;
        public string LeagueID;
        public string OpponentName;
        public string OpponentId;
        public string MyWeapon;


        public string ToJSON()
        {
            return JsonUtility.ToJson(this);
        }
        public static MatchTurn CreateFromJSON(string jsonString)
        {
            return JsonUtility.FromJson<MatchTurn>(jsonString);
        }
    }


    [System.Serializable]
    public class MatchBrief
    {
        public int DateTime;
        public string Opponent;
        public string OpponentId;
        public string PlayerId;
        public Weapon MyWeapon;
        public Weapon OpponentWeapon;
        public WLD Result;


        // constructor using @ seperated string instead of JSON to meet server 1000 byte PlayFab requirement
        public MatchBrief(string specialString)
        {
            var splitString = specialString.Split('@');
            DateTime = Int32.Parse(splitString[0]);
            Opponent = splitString[1];
            OpponentId = splitString[2];
            PlayerId = splitString[3];

            // parsing result like "RSW" (rock beats scissors, win)
            var resultCode = splitString[4];
            MyWeapon = ParseWeapon(resultCode[0]);
            OpponentWeapon = ParseWeapon(resultCode[1]);
            Result = ParseWLD(resultCode[2]);
        }

        private Weapon ParseWeapon(char weaponCode)
        {
            if (weaponCode == 'R')
                return Weapon.Rock;
            if (weaponCode == 'P')
                return Weapon.Paper;
            if (weaponCode == 'S')
                return Weapon.Scissors;
            return Weapon.None;
        }

        private WLD ParseWLD(char wld)
        {
            if (wld == 'W')
                return WLD.Win;
            if (wld == 'L')
                return WLD.Lose;
            if (wld == 'D')
                return WLD.Draw;
            return WLD.None;
        }
    }


    public class League
    {
        public string Status;
        public LeagueSettings Settings;
        public string Name = "Unnamed";
        public string Host = "NoHost";
        public List<LeaguePlayerStats> PlayerList;
        public string Key = "";
        public List<MatchBrief> Schedule = new List<MatchBrief>();

        public League(string status, string name, string host,
                        LeagueSettings settings, string key, List<LeaguePlayerStats> playerList)
        {
            Status = status;
            Name = name;
            Settings = settings;
            Host = host;
            Key = key;
            PlayerList = playerList;
        }
        public delegate void StartSeasonCallback();
        public void StartSeason(StartSeasonCallback callback, StartSeasonCallback errorCallback)
        {
            Status = "In Progress";
            PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
            {
                FunctionName = "StartLeagueSeason",
                FunctionParameter = new
                {
                    status = Status,
                    leagueId = Key,
                },
                GeneratePlayStreamEvent = true,
            },
           result =>
           {
               // get Json object representing the host's schedule out of FunctionResult
               JsonObject jsonResult = (JsonObject)result.FunctionResult;

               // check if data exists
               if (jsonResult == null)
               {
                   Debug.Log("schedule generation failed...");
                   errorCallback();
                   return;
               }

               // data successfully received 
               // interpret data
               string error = RPSCommon.InterpretCloudScriptData(jsonResult, "error");
               if (error != null)
               {
                   Debug.Log(error);
                   errorCallback();
                   return;
               }

               string scheduleJSON = RPSCommon.InterpretCloudScriptData(jsonResult, "schedule");

               var matchDataArray = scheduleJSON.Split('"').Where((item, index) => index % 2 != 0);
               foreach (string matchString in matchDataArray)
                   Schedule.Add(new MatchBrief(matchString));

               // send event that league is started, for anyone logged in
               var data = new object[] { Key };
               PhotonNetwork.RaiseEvent(
                   LeagueView.LEAGUE_UPDATE_EVENT, // .Code
                   data,                           // .CustomData
                   RaiseEventOptions.Default,
                   SendOptions.SendReliable
               );

               callback();
           },
           RPSCommon.OnPlayFabError
           );
        }

        public void EndSeason()
        {
            Status = "Complete";
            UpdateStatusOnServer();
        }

        private void UpdateStatusOnServer()
        {
            PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
            {
                FunctionName = "UpdateLeagueStatus",
                FunctionParameter = new
                {
                    status = Status,
                    leagueId = Key,
                },
                GeneratePlayStreamEvent = true,
            },
            OnSuccess =>
            {
                Debug.Log("League Status updated: " + Status);
            },
            RPSCommon.OnPlayFabError
            );
        }
    }

    public class LeagueManager : Singleton<PlayerManager>
    {
        public static LeagueSettings leagueSettings;
        public static bool redirectLoginToLeague = false;
        private static string leagueViewKey;

        public static League league;

        public static void NewCustomLeague()
        {
            NewLeague(LeagueType.Custom);
        }
        public static void NewRatedLeague()
        {
            NewLeague(LeagueType.Rated);
        }

        public static void SetMatchCount(int matchCount)
        {
            leagueSettings.MatchCount = matchCount;
        }

        public static void SetRoundDuration(int roundDuration)
        {
            leagueSettings.RoundDuration = roundDuration;
        }
        public static void NewLeague(LeagueType leagueType)
        {
            leagueSettings = new LeagueSettings(leagueType);
        }

        public delegate void GetLeaguesCallBack(List<TitleDescriptionButtonData> leagues);
        public static void GetCurrentLeagues(GetLeaguesCallBack callback)
        {
            List<string> userDataKeys = new List<string>() { "CurrentLeagues" };
            List<string> leagueKeys = new List<string>();
            List<TitleDescriptionButtonData> toRet = new List<TitleDescriptionButtonData>();

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
                            toRet.Add(new TitleDescriptionButtonData(
                                key,
                                leagueInfo.Name,
                                leagueInfo.LeagueSettings.LeagueType + "\n" + leagueInfo.Status,
                                leagueInfo.LeagueSettings.LeagueType == "Rated"));
                        }
                    }
                    toRet.Reverse(); // put most recent league at top
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
            List<string> userDataKeys = new List<string>() { "CurrentLeagues" };
            List<string> leagueKeys = new List<string>();
            List<TitleDescriptionButtonData> toRet = new List<TitleDescriptionButtonData>();

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
                                toRet.Add(new TitleDescriptionButtonData(
                                   key,
                                   leagueInfo.Name,
                                   leagueInfo.LeagueSettings.LeagueType + "\n" + leagueInfo.Status,
                                   leagueInfo.LeagueSettings.LeagueType == "Rated"));
                            }
                        }
                    }
                    toRet.Reverse(); // put most recent league at top
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
            List<string> userDataKeys = new List<string>() { "CurrentLeagues" };
            List<string> leagueKeys = new List<string>();
            List<TitleDescriptionButtonData> toRet = new List<TitleDescriptionButtonData>();

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

                // nested PlayFab call to get tTitleData using keys returned from UserData
                PlayFabClientAPI.GetTitleData(new GetTitleDataRequest()
                { }, // get all data from Title
                titleResult =>
                {
                    foreach (var entry in titleResult.Data)
                    {
                        // TODO: if we add other kinds of data to title data, we need to check for prefix "League"
                        LeagueInfo leagueInfo = LeagueInfo.CreateFromJSON(entry.Value);
                        if (leagueInfo.Status == "Open" && !leagueKeys.Contains(entry.Key))
                        {
                            toRet.Add(new TitleDescriptionButtonData(
                                entry.Key,
                                leagueInfo.Name,
                                // TODO: show game settings instead of league status here
                                leagueInfo.LeagueSettings.LeagueType + "\n" + leagueInfo.Status,
                                leagueInfo.LeagueSettings.LeagueType == "Rated"));
                        }
                    }
                    toRet.Reverse(); // put most recent at league at top
                    callback(toRet);
                },
                RPSCommon.OnPlayFabError
                );
            },
            RPSCommon.OnPlayFabError
            );
        }

        private static List<TitleDescriptionButtonData> FakePlayerList()
        {
            List<TitleDescriptionButtonData> toRet = new List<TitleDescriptionButtonData>();
            for (int i = 0; i < 30; i++)
            {
                toRet.Add(new TitleDescriptionButtonData(null, "League_" + (i + 1).ToString(), "TEST LEAGUE"));
            }
            return toRet;
        }

        public static string GetCurrentLeagueViewKey()
        {
            return leagueViewKey;
        }
    }
}

namespace ThirstyJoe.RPSChampions
{
    using UnityEngine;
    using System.Collections.Generic;
    using PlayFab;
    using PlayFab.ClientModels;
    using System.Linq;
    using PlayFab.Json;
    using System;

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
    [System.Serializable]
    public class LeagueInfo
    {
        public string Status;
        public string LeagueSettingsJSON;
        public string Name;
        public string HostName;

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
    public class LeaguePlayer
    {
        public string PlayerName;
        public string PlayFabId;
        public int Wins;
        public int Losses;
        public int Draws;
        public int Rating;

        public LeaguePlayer(string name, string playfabId, int rating)
        {
            PlayerName = name;
            PlayFabId = playfabId;
            Rating = rating;
        }

        public string ToJSON()
        {
            return JsonUtility.ToJson(this);
        }
        public static LeaguePlayer CreateFromJSON(string jsonString)
        {
            return JsonUtility.FromJson<LeaguePlayer>(jsonString);
        }
    }

    [System.Serializable]
    public class ScheduledMatch
    {
        public int DateTime;
        public string OpponentJSON; // JSON for easier reading and writing to and from Server
        public string MatchID;
        public string Result = "";
        public string MyWeapon;
        public string OpponentWeapon;

        public string ToJSON()
        {
            return JsonUtility.ToJson(this);
        }
        public static ScheduledMatch CreateFromJSON(string jsonString)
        {
            return JsonUtility.FromJson<ScheduledMatch>(jsonString);
        }

        public TitleDescriptionButtonData GetButtonData()
        {
            string matchDescription;
            if (Result == "None") // no winner yet
            {
                // description is start time of match
                var dateTime = new System.DateTime(DateTime);
                matchDescription = dateTime.ToShortDateString() + " " + dateTime.ToShortTimeString();
            }
            else
            {
                matchDescription = Result;
            }

            LeaguePlayer Opponent = LeaguePlayer.CreateFromJSON(OpponentJSON);
            return new TitleDescriptionButtonData(
                MatchID, // MatchID = LeagueID_OpponentName_MatchTime
                "vs " + Opponent.PlayerName,
                matchDescription
            );
        }
    }


    [System.Serializable]
    public class MatchBrief
    {
        public int DateTime;
        public string Opponent;
        public string OpponentId;

        // constructor using @ seperated string instead of JSON to meet server 1000 byte requirement
        public MatchBrief(string specialString)
        {
            var splitString = specialString.Split('@');
            DateTime = Int32.Parse(splitString[0]);
            Opponent = splitString[1];
            OpponentId = splitString[2];
        }

        public string ToJSON()
        {
            return JsonUtility.ToJson(this);
        }
        public static MatchBrief CreateFromJSON(string jsonString)
        {
            return JsonUtility.FromJson<MatchBrief>(jsonString);
        }
    }


    public class League
    {
        public string Status;
        public LeagueSettings Settings;
        public string Name = "Unnamed";
        public string Host = "NoHost";
        public List<LeaguePlayer> PlayerList;
        public string Key = "";
        public List<MatchBrief> Schedule = new List<MatchBrief>();

        public League(string status, string name, string host,
                        LeagueSettings settings, string key, List<LeaguePlayer> playerList)
        {
            Status = status;
            Name = name;
            Settings = settings;
            Host = host;
            Key = key;
            PlayerList = playerList;
        }
        public delegate void StartSeasonCallback();
        public void StartSeason(StartSeasonCallback callback)
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
                   return;
               }

               // data successfully received 
               // interpret data
               string scheduleJSON = RPSCommon.InterpretCloudScriptData(jsonResult, "schedule");
               Debug.Log("League schedule: " + scheduleJSON);
               var matchDataArray = scheduleJSON.Split('"').Where((item, index) => index % 2 != 0);
               foreach (string matchString in matchDataArray)
                   Schedule.Add(new MatchBrief(matchString));

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
        private static string leagueViewKey;

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
                            if (leagueInfo.Status != "Complete")
                            {
                                toRet.Add(new TitleDescriptionButtonData(
                                    key,
                                    leagueInfo.Name,
                                    leagueInfo.Status));
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
                                   leagueInfo.Status));
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
                                leagueInfo.Status));
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

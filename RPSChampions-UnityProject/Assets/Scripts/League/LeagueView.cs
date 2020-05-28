namespace ThirstyJoe.RPSChampions
{
    using UnityEngine;
    using TMPro;
    using UnityEngine.SceneManagement;
    using UnityEngine.EventSystems;
    using PlayFab;
    using PlayFab.ClientModels;
    using System.Collections.Generic;
    using System.Linq;
    using System;
    using System.Globalization;
    using PlayFab.Json;

    public class LeagueView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI TitleText;
        [SerializeField] private GameObject StandingsListPanel;
        [SerializeField] private GameObject StandingsListContent;
        [SerializeField] private GameObject PlayerListPanel;
        [SerializeField] private GameObject PlayerListContent;
        [SerializeField] private GameObject MatchListPanel;
        [SerializeField] private GameObject MatchListContent;
        [SerializeField] private GameObject PlayerButtonPrefab;


        // tracking previous selection, for when returning from this menu
        private GameObject prevUISelection;

        // All the data for the league
        private League league;

        private void Start()
        {
            if (TitleDescriptionButtonLinkData.Label == "Open")
                JoinLeague();
            else
                GetLeagueDataFromServer();
        }

        private void JoinLeague()
        {
            string leagueKey = TitleDescriptionButtonLinkData.LinkID;
            LeaguePlayerStats leaguePlayerData = new LeaguePlayerStats(
                PlayerManager.PlayerName,
                PlayerPrefs.GetString("playFabId"));

            PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
            {
                FunctionName = "JoinLeague",
                FunctionParameter = new
                {
                    playerData = leaguePlayerData.ToJSON(),
                    leagueId = leagueKey,
                },
                GeneratePlayStreamEvent = true,
            },
            result =>
            {
                // get Json object representing the Game State out of FunctionResult
                JsonObject jsonResult = (JsonObject)result.FunctionResult;

                // check if data exists
                if (jsonResult == null)
                {
                    Debug.Log("server failed to return data");
                }
                else
                {
                    Debug.Log("Joined League");
                    GetLeagueDataFromServer(); // TODO: skip this by using the data returned from joining
                }
            },
            RPSCommon.OnPlayFabError
            );
        }

        private void GetLeagueDataFromServer()
        {
            // id saved by this butto
            string leagueKey = TitleDescriptionButtonLinkData.LinkID;
            // keys that must exist for valid league
            List<string> validLeagueKeys = new List<string>()
            {
                "Status",
                "Name",
                "HostName",
                "Settings",
            };
            PlayFabClientAPI.GetSharedGroupData(new GetSharedGroupDataRequest()
            {
                SharedGroupId = leagueKey,
            },
            result =>
            {
                // validate
                bool failedKey = false;
                foreach (string key in validLeagueKeys)
                {
                    if (!result.Data.ContainsKey(key))
                    {
                        Debug.Log("ERROR. Missing key from league: " + key);
                        failedKey = true;
                    }
                }

                if (!failedKey)
                {
                    // generate player list
                    // "Player_" Prefix, in a key is brief player data
                    // "PlayerSchedule_" Prefix is the complete list of matches for a player
                    List<LeaguePlayerStats> playerList = new List<LeaguePlayerStats>();
                    foreach (string key in result.Data.Keys)
                    {
                        if (key.StartsWith("Player_"))
                        {
                            string playerDataJSON = result.Data[key].Value;
                            LeaguePlayerStats playerData = LeaguePlayerStats.CreateFromJSON(playerDataJSON);
                            playerList.Add(playerData);
                        }
                    }

                    // create instance of league
                    league = new League(
                        result.Data["Status"].Value,
                        result.Data["Name"].Value,
                        result.Data["HostName"].Value,
                        LeagueSettings.CreateFromJSON(result.Data["Settings"].Value),
                        leagueKey,
                        playerList
                    );

                    // get our schedule
                    string scheduleKey = "PlayerSchedule_" + PlayerPrefs.GetString("playFabId");
                    if (result.Data.ContainsKey(scheduleKey))
                    {
                        string scheduleJSON = result.Data[scheduleKey].Value;
                        var matchDataArray = scheduleJSON.Split('"').Where((item, index) => index % 2 != 0);
                        foreach (string matchString in matchDataArray)
                            league.Schedule.Add(new MatchBrief(matchString));
                    }

                    // determine type of UI we need to set up, OPEN league or CLOSED
                    // Open means it is still recruiting, otherwise it has started or completed
                    if (league.Status == "Open")
                        LeagueViewOpenUI();
                    else
                        LeagueViewClosedUI();
                }
            },
            RPSCommon.OnPlayFabError
            );
        }

        private void LeagueViewClosedUI()
        {
            TitleText.text = league.Name;
            MatchListPanel.SetActive(true);
            PlayerListPanel.SetActive(false);
            StandingsListPanel.SetActive(true);

            // generate player list
            int matchIndex = 0;
            foreach (LeaguePlayerStats player in league.PlayerList)
            {
                GameObject obj = Instantiate(PlayerButtonPrefab, StandingsListContent.transform);
                var tdButton = obj.GetComponent<TitleDescriptionButton>();

                var buttonData = new TitleDescriptionButtonData(
                    player.PlayerName, // TODO: PlayFabId might be better to use here for LinkID
                    player.PlayerName,
                    player.Wins.ToString() + " - " + player.Losses.ToString() + " - " + player.Draws.ToString()
                );
                tdButton.SetupButton(buttonData, "PlayerProfile");
            }

            UpdateMatchList();
        }

        private void LeagueViewOpenUI()
        {
            TitleText.text = league.Name;
            MatchListPanel.SetActive(false);
            PlayerListPanel.SetActive(true);
            StandingsListPanel.SetActive(false);

            // generate player list
            foreach (LeaguePlayerStats player in league.PlayerList)
            {
                GameObject obj = Instantiate(PlayerButtonPrefab, PlayerListContent.transform);
                var tdButton = obj.GetComponent<TitleDescriptionButton>();

                var buttonData = new TitleDescriptionButtonData(
                    player.PlayerName, // TODO: PlayFabId might be better to use here for LinkID
                    player.PlayerName,
                    "Rating: " + player.Rating.ToString()
                );
                tdButton.SetupButton(buttonData, "PlayerProfile");
            }
        }

        public void OnBackButtonPress()
        {
            EventSystem.current.SetSelectedGameObject(prevUISelection);
            SceneManager.UnloadSceneAsync("LeagueView");
        }
        public void OnStartSeasonButtonPress()
        {
            league.StartSeason(UpdateMatchList);
            LeagueViewClosedUI();
        }

        public void UpdateMatchList()
        {
            CultureInfo culture = new CultureInfo("en-US");
            // generate match list
            if (league.Schedule != null)
            {
                int matchIndex = 0;
                foreach (MatchBrief match in league.Schedule)
                {
                    GameObject obj = Instantiate(PlayerButtonPrefab, MatchListContent.transform);
                    var tdButton = obj.GetComponent<TitleDescriptionButton>();

                    string formattedDate =
                        RPSCommon.UnixTimeToDateTime(match.DateTime).ToString("m", culture) +
                        " " +
                        RPSCommon.UnixTimeToDateTime(match.DateTime).ToString("t", culture);

                    var buttonData = new TitleDescriptionButtonData(
                        league.Key,
                        match.Opponent,
                        formattedDate
                    );
                    tdButton.SetupButton(buttonData, "MatchOverview", "", matchIndex++);
                }
            }
        }

    }
}
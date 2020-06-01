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
                    playerName = PlayerManager.PlayerName
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
            var leagueId = TitleDescriptionButtonLinkData.LinkID;

            // keys that must exist for valid league
            List<string> validLeagueKeys = new List<string>()
            {
                "Status",
                "Name",
                "HostName",
                "Settings",
            };
            PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
            {
                FunctionName = "GetLeague",
                FunctionParameter = new
                {
                    leagueKey = leagueId
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
                    Debug.Log("League data received from server");

                    // get list of players
                    string playerListJSON = RPSCommon.InterpretCloudScriptData(jsonResult, "Players");
                    var playerArray = JsonHelper.getJsonArray<LeaguePlayerStats>(playerListJSON);
                    var playerList = new List<LeaguePlayerStats>();
                    foreach (LeaguePlayerStats player in playerArray)
                        playerList.Add(player);

                    // create instance of league
                    league = new League(
                        RPSCommon.InterpretCloudScriptData(jsonResult, "Status"),
                        RPSCommon.InterpretCloudScriptData(jsonResult, "Name"),
                        RPSCommon.InterpretCloudScriptData(jsonResult, "HostName"),
                        LeagueSettings.CreateFromJSON(RPSCommon.InterpretCloudScriptData(jsonResult, "Settings")),
                        leagueId,
                        playerList
                    );

                    // interpret schedule as object and save in league object
                    Debug.Log(league.Status);
                    if (league.Status != "Open")
                    {
                        string scheduleJSON = RPSCommon.InterpretCloudScriptData(jsonResult, "Schedule");
                        Debug.Log(scheduleJSON);
                        if (scheduleJSON != "null")
                        {
                            var matchDataArray = scheduleJSON.Split('"').Where((item, index) => index % 2 != 0);
                            foreach (string matchString in matchDataArray)
                                league.Schedule.Add(new Match(matchString));
                        }
                    }
                }

                // determine type of UI we need to set up, OPEN league or CLOSED
                // Open means it is still recruiting, otherwise it has started or completed
                if (league.Status == "Open")
                    LeagueViewOpenUI();
                else
                    LeagueViewClosedUI();
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
                    player.PlayerName,
                    player.PlayerName,
                    player.Rating.ToString()
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
                foreach (Match match in league.Schedule)
                {
                    GameObject obj = Instantiate(PlayerButtonPrefab, MatchListContent.transform);
                    var tdButton = obj.GetComponent<TitleDescriptionButton>();

                    string description = "";
                    if (match.Result == WLD.None)
                    {
                        description =
                            RPSCommon.UnixTimeToDateTime(match.DateTime).ToString("m", culture) + "\n" +
                            " " +
                            RPSCommon.UnixTimeToDateTime(match.DateTime).ToString("t", culture);
                    }
                    else
                    {
                        if (match.Result == WLD.Draw)
                            description = match.Result.ToString();
                        else
                            description = "You " + match.Result.ToString();
                    }

                    var buttonData = new TitleDescriptionButtonData(
                        league.Key,
                        match.Opponent,
                        description
                    );
                    tdButton.SetupButton(buttonData, "MatchOverview", "", matchIndex++);
                }
            }
        }

    }
}
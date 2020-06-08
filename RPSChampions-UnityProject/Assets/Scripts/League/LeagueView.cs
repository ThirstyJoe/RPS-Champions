namespace ThirstyJoe.RPSChampions
{
    using UnityEngine;
    using Photon.Pun;
    using TMPro;
    using ExitGames.Client.Photon;
    using Photon.Realtime;
    using UnityEngine.SceneManagement;
    using UnityEngine.EventSystems;
    using PlayFab;
    using PlayFab.ClientModels;
    using System.Collections.Generic;
    using System.Linq;
    using System;
    using System.Globalization;
    using PlayFab.Json;

    public class LeagueView : MonoBehaviourPunCallbacks
    {
        #region EVENT DEFS

        public const byte LEAGUE_UPDATE_EVENT = 0;
        public const byte LEAGUE_UPDATE_SELF_EVENT = 1;
        public const byte LEAGUE_CANCELLED_EVENT = 2;

        #endregion

        #region OBJ REFS 
        [SerializeField] private TextMeshProUGUI OpenTitleText;
        [SerializeField] private TextMeshProUGUI ClosedTitleText;
        [SerializeField] private GameObject StandingsListPanel;
        [SerializeField] private GameObject StandingsListContent;
        [SerializeField] private GameObject PlayerListPanel;
        [SerializeField] private GameObject PlayerListContent;
        [SerializeField] private GameObject MatchListPanel;
        [SerializeField] private GameObject MatchListContent;
        [SerializeField] private GameObject PlayerButtonPrefab;
        [SerializeField] private GameObject HostButtonGroup;
        [SerializeField] private GameObject NonHostButtonGroup;
        [SerializeField] private GameObject NonHostSeasonOverButtonGroup;
        [SerializeField] private GameObject HostSeasonOverButtonGroup;
        [SerializeField] private GameObject HostQuitConfirmationPanel;
        [SerializeField] private GameObject NonHostQuitConfirmationPanel;
        [SerializeField] private GameObject LeagueCancelledAlertPanel;

        #endregion

        #region PRIVATE VARS 

        // use these list to track buttons refs for cleanup when updating
        private List<GameObject> playerButtonList = new List<GameObject>();
        private List<GameObject> matchButtonList = new List<GameObject>();


        // tracking previous selection, for when returning from this menu
        private GameObject prevUISelection;

        // All the data for the league
        private League league;

        #endregion

        #region UNITY 

        private void Awake()
        {
            PhotonNetwork.NetworkingClient.EventReceived += ReceiveCustomPUNEvents;
        }

        private void OnDestroy()
        {
            PhotonNetwork.NetworkingClient.EventReceived -= ReceiveCustomPUNEvents;
        }

        private void Start()
        {
            prevUISelection = EventSystem.current.currentSelectedGameObject;
            HostButtonGroup.SetActive(false);
            NonHostButtonGroup.SetActive(false);
            HostSeasonOverButtonGroup.SetActive(false);
            NonHostSeasonOverButtonGroup.SetActive(false);

            if (TitleDescriptionButtonLinkData.Label == "Open")
                JoinLeague();
            else
                GetLeagueDataFromServer();

            ConnectToPhoton();
        }

        #endregion

        #region PUN EVENT FUNCS
        private void ReceiveCustomPUNEvents(EventData obj)
        {
            // get data from event
            object[] data = (object[])obj.CustomData;
            if (data == null) return;

            // make sure event is from the correct league
            string eventLeague = (string)data[0];
            if (eventLeague != league.Key) return;

            // switch to correct function
            switch (obj.Code)
            {
                case LEAGUE_UPDATE_EVENT:
                    UpdateLeagueView();
                    break;
                case LEAGUE_UPDATE_SELF_EVENT:
                    string playerId = (string)data[1];
                    Debug.Log(playerId + " / " + PlayerPrefs.GetString("playFabId"));
                    if (playerId == PlayerPrefs.GetString("playFabId"))
                        UpdateLeagueView();
                    break;
                case LEAGUE_CANCELLED_EVENT:
                    ShowLeagueCancelledAlert();
                    break;
            }
        }

        #endregion

        #region PUN


        public void ConnectToPhoton()
        {
            PhotonNetwork.LocalPlayer.NickName = PlayerManager.PlayerStats.PlayerName;
            PhotonNetwork.ConnectUsingSettings();
        }

        public override void OnPlayerEnteredRoom(Player other)
        {
            Debug.LogFormat("OnPlayerEnteredRoom() {0}", other.NickName);
            if (league.Status == "Open")
                UpdateLeagueView();
        }

        public override void OnPlayerLeftRoom(Player other)
        {
            Debug.LogFormat("OnPlayerLeftRoom() {0}", other.NickName);
        }

        public override void OnConnectedToMaster()
        {
            // connect to room
            Debug.Log("connected to master");
            string leagueKey = TitleDescriptionButtonLinkData.LinkID;
            PhotonNetwork.JoinOrCreateRoom(leagueKey, null, null);
        }

        public override void OnCreatedRoom()
        {
            Debug.Log("Game Created");
        }

        public override void OnJoinedRoom()
        {
            Debug.Log("Join Game Successful");
        }

        public override void OnCreateRoomFailed(short returnCode, string message)
        {
            Debug.Log("Create Game Failed: " + message);
        }

        public override void OnJoinRoomFailed(short returnCode, string message)
        {
            Debug.Log("Join Game Failed: " + message);
        }

        public override void OnJoinRandomFailed(short returnCode, string message)
        {
            Debug.Log("Join Random Game Failed: " + message);
        }

        #endregion

        #region CUSTOM PUBLIC 



        // called when returning from match overview
        public void UpdateLeagueView()
        {
            GetLeagueDataFromServer();
        }

        #endregion

        #region CUSTOM PRIVATE 

        private void ShowLeagueCancelledAlert()
        {
            LeagueCancelledAlertPanel.SetActive(true);
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
                // update player stats
                PlayerManager.UpdatePlayerStats();

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
                    // order them WLD
                    playerList.Sort(
                        delegate (LeaguePlayerStats c1, LeaguePlayerStats c2)
                        {
                            return c1.WLDScore.CompareTo(c2.WLDScore);
                        }
                    );
                    playerList.Reverse();

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
                    if (league.Status != "Open")
                    {
                        string scheduleJSON = RPSCommon.InterpretCloudScriptData(jsonResult, "Schedule");

                        if (scheduleJSON != "null")
                        {
                            scheduleJSON = scheduleJSON.Trim(']');
                            scheduleJSON = scheduleJSON.Trim('[');
                            scheduleJSON = scheduleJSON.Replace("\"", string.Empty);
                            var matchDataArray = scheduleJSON.Split(',');

                            foreach (string matchString in matchDataArray)
                            {
                                if (matchString == "null")
                                    league.Schedule.Add(null);
                                else
                                    league.Schedule.Add(new MatchBrief(matchString));
                            }

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

        #endregion

        #region UI 

        private void LeagueViewClosedUI()
        {
            ClosedTitleText.text = league.Name;
            MatchListPanel.SetActive(true);
            PlayerListPanel.SetActive(false);
            StandingsListPanel.SetActive(true);

            // clear previous buttons
            foreach (var button in playerButtonList)
                GameObject.Destroy(button);
            playerButtonList.Clear();

            // show buttons for end of season navigation
            if (league.Status == "Complete")
            {
                NonHostSeasonOverButtonGroup.SetActive(true);
            }

            // data for end of season rating adjustments
            List<int> ratingList = new List<int>();
            List<int> scoreList = new List<int>();
            int rank = 0;
            if (league.Status == "Complete")
            {
                foreach (LeaguePlayerStats player in league.PlayerList)
                {
                    ratingList.Add(player.Rating);
                    scoreList.Add(player.WLDScore);
                }
            }

            // generate player list
            foreach (LeaguePlayerStats player in league.PlayerList)
            {
                rank++;

                GameObject obj = Instantiate(PlayerButtonPrefab, StandingsListContent.transform);
                playerButtonList.Add(obj);
                var tdButton = obj.GetComponent<TitleDescriptionButton>();

                var buttonData = new TitleDescriptionButtonData(
                    player.PlayerName, // TODO: PlayFabId might be better to use here for LinkID
                    player.PlayerName,
                    player.Wins.ToString() + " - " + player.Losses.ToString() + " - " + player.Draws.ToString()
                );
                tdButton.SetupButton(buttonData, "PlayerProfile");

                if (league.Status == "Complete")
                {
                    int points = CalculateRatingChange(ratingList, scoreList, rank);
                    tdButton.SetPointText(points);
                }
            }

            UpdateMatchList();
        }

        private int CalculateRatingChange(List<int> ratings, List<int> scores, int rank)
        {
            int index = rank - 1;
            int myRating = ratings[index];
            int myScore = scores[index];
            float ratingChange = 0;

            for (int i = 0; i < ratings.Count; i++)
            {
                if (i == index) continue;
                ratingChange += CalculateRatingChangeFactor(myRating, ratings[i], myScore, scores[i]);
            }

            return (int)Math.Round(ratingChange);
        }

        // using: https://www.youtube.com/watch?v=AsYfbmp0To0&feature=emb_title
        private float CalculateRatingChangeFactor(int myRating, int oppRating, int myScore, int oppScore)
        {
            float maxChange = 32F;
            float ELOrange = 400F;
            float expectedScore = 1F / (1F + (float)Math.Pow(10F, (oppRating - myRating) / ELOrange));
            float score = 0.5F;
            if (myScore < oppScore) score = 0F;
            if (myScore > oppScore) score = 1F;
            return maxChange * (score - expectedScore);
        }
        private void LeagueViewOpenUI()
        {
            // clear previous buttons
            foreach (var button in playerButtonList)
                GameObject.Destroy(button);
            playerButtonList.Clear();

            OpenTitleText.text = league.Name;
            MatchListPanel.SetActive(false);
            PlayerListPanel.SetActive(true);
            StandingsListPanel.SetActive(false);

            // generate player list
            foreach (LeaguePlayerStats player in league.PlayerList)
            {
                GameObject obj = Instantiate(PlayerButtonPrefab, PlayerListContent.transform);
                playerButtonList.Add(obj);
                var tdButton = obj.GetComponent<TitleDescriptionButton>();

                var buttonData = new TitleDescriptionButtonData(
                    player.PlayerName,
                    player.PlayerName,
                    player.Rating.ToString()
                );
                tdButton.SetupButton(buttonData, "PlayerProfile");
            }

            // buttons for host or non host
            if (league.Host == PlayerManager.PlayerName)
                HostButtonGroup.SetActive(true);
            else
                NonHostButtonGroup.SetActive(true);
        }

        public void UpdateMatchList()
        {
            // clear previous buttons
            foreach (var button in matchButtonList)
                GameObject.Destroy(button);
            matchButtonList.Clear();

            CultureInfo culture = new CultureInfo("en-US");
            // generate match list
            if (league.Schedule != null)
            {
                int matchIndex = 0;
                foreach (MatchBrief match in league.Schedule)
                {
                    if (match == null)
                    { // skip bye round
                        matchIndex++;
                        continue;
                    }

                    GameObject obj = Instantiate(PlayerButtonPrefab, MatchListContent.transform);
                    matchButtonList.Add(obj);
                    var tdButton = obj.GetComponent<TitleDescriptionButton>();

                    string description = "";
                    if (match.Result == WLD.None)
                    {
                        string weaponText = "No Selection";
                        if (match.MyWeapon != Weapon.None)
                            weaponText = "Playing " + match.MyWeapon.ToString();
                        description =
                            weaponText + "\n" +
                            RPSCommon.UnixTimeToDateTime(match.DateTime).ToString("m", culture) + ", " +
                            " " +
                            RPSCommon.UnixTimeToDateTime(match.DateTime).ToString("t", culture);
                    }
                    else
                    {
                        string weaponText = "No Selection";
                        if (match.MyWeapon != Weapon.None)
                            weaponText = match.MyWeapon.ToString() + " VS " + match.OpponentWeapon.ToString();
                        if (match.Result == WLD.Draw)
                            description = weaponText + "\n" +
                                          match.Result.ToString();
                        else
                            description = weaponText + "\n" +
                                          match.Result.ToString();
                    }

                    var buttonData = new TitleDescriptionButtonData(
                        league.Key,
                        match.Opponent,
                        description
                    );
                    tdButton.SetupButton(buttonData, "MatchOverview", "", matchIndex++);

                    if (match.Result != WLD.None)
                    {
                        int points = 0;
                        if (match.Result == WLD.Draw)
                            points = 1;
                        if (match.Result == WLD.Win)
                            points = 3;
                        tdButton.SetPointText(points);
                    }
                }
            }
        }

        #endregion

        #region PLAYER ACTIONS 

        public void OnBackButtonPress()
        {
            DisconnectFromGame();
        }
        public void OnStartSeasonButtonPress()
        {
            league.StartSeason(UpdateMatchList);
            LeagueViewClosedUI();
        }
        public void OnQuitLeagueButtonPress()
        {
            NonHostQuitConfirmationPanel.SetActive(true);
        }

        public void OnCancelLeagueButtonPress()
        {
            HostQuitConfirmationPanel.SetActive(true);
        }

        public void OnConfirmCancelLeague()
        {
            // on confirmation server will do the following...
            // delete from title data
            // delete shared data
            PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
            {
                FunctionName = "CancelLeague",
                FunctionParameter = new
                {
                    leagueId = league.Key,
                },
                GeneratePlayStreamEvent = true,
            },
            result =>
            {
                CreateAndSendLeagueEvent(LEAGUE_CANCELLED_EVENT);

                Debug.Log("league cancelled");

                DisconnectFromGame();
            },
            RPSCommon.OnPlayFabError
            );
        }

        public void OnConfirmQuitLeague()
        {
            // on confirmation server will ...
            // delete from current league list
            // delete player from shared data
            PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
            {
                FunctionName = "QuitLeague",
                FunctionParameter = new
                {
                    leagueId = league.Key,
                },
                GeneratePlayStreamEvent = true,
            },
            result =>
            {
                CreateAndSendLeagueEvent(LEAGUE_UPDATE_EVENT);

                Debug.Log("quit league");

                DisconnectFromGame();
            },
            RPSCommon.OnPlayFabError
            );
        }

        public void OnExitLeagueButtonPress()
        {
            DisconnectFromGame();
        }

        public void OnCancelQuitLeague()
        {
            NonHostQuitConfirmationPanel.SetActive(false);
        }

        public void OnCancelCancelLeague()
        {
            HostQuitConfirmationPanel.SetActive(false);
        }

        public void OnConfirmCancelledLeagueAlert()
        {
            DisconnectFromGame();
        }

        private void CreateAndSendLeagueEvent(byte eventName)
        {
            // send event to update in league view
            string leagueKey = TitleDescriptionButtonLinkData.LinkID;
            var data = new object[] { league.Key };
            PhotonNetwork.RaiseEvent(
                eventName,         // .Code
                data,                                   // .CustomData
                RaiseEventOptions.Default,
                SendOptions.SendReliable
            );
        }

        private void DisconnectFromGame()
        {
            PhotonNetwork.Disconnect();

            // return to previous menu
            EventSystem.current.SetSelectedGameObject(prevUISelection);
            SceneManager.UnloadSceneAsync("LeagueView");
        }

        #endregion
    }
}
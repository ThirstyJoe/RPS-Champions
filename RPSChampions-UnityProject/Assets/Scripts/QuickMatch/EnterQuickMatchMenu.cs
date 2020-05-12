namespace ThirstyJoe.RPSChampions
{
    using UnityEngine;
    using Photon.Pun;
    using TMPro;
    using ExitGames.Client.Photon;
    using Photon.Realtime;
    using System.Collections.Generic;
    using PlayFab;
    using PlayFab.ClientModels;
    using UnityEngine.SceneManagement;
    using System.Text;
    using System;

    public class EnterQuickMatchMenu : MonoBehaviourPunCallbacks
    {
        #region EVENT DEFS

        private const byte REQUEST_MATCH_EVENT = 0;
        private const byte CANCEL_MATCH_EVENT = 1;
        private const byte CREATED_MATCH_EVENT = 2;

        #endregion

        #region PRIVATE VARS
        [SerializeField]
        private GameObject connectingLabel;

        [SerializeField]
        private GameObject waitingLabel;
        [SerializeField]
        private GameObject requestedLabel;
        [SerializeField]
        private GameObject selectLabel;
        [SerializeField]
        private GameObject challengeLabel;

        [SerializeField]
        private TextMeshProUGUI playerNameText;

        [SerializeField]
        private TextMeshProUGUI playerStatsText;

        [SerializeField]
        private ChallengePlayerButton[] buttonArray;

        private int CreateRoomAttempts = 0;
        private int MaxCreateRoomAttemps = 3;
        private string requestedName = "";
        private Dictionary<string, string> challenges = new Dictionary<string, string>();
        private Dictionary<string, int> userButtonMap = new Dictionary<string, int>();
        private int totalOpponents = 0;
        private bool connected = false;
        private bool startingMatch = false;

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
            ConnectToPhoton();
            UpdatePlayerUI();
            UpdateUserListUI();
        }

        #endregion

        #region PUN


        public override void OnPlayerEnteredRoom(Player other)
        {
            Debug.LogFormat("OnPlayerEnteredRoom() {0}", other.NickName);
            UpdateUserListUI();
        }

        public override void OnPlayerLeftRoom(Player other)
        {
            Debug.LogFormat("OnPlayerLeftRoom() {0}", other.NickName);
            UpdateUserListUI();
        }

        public override void OnConnectedToMaster()
        {
            // connect to room
            SetPlayerProperties();
            Debug.Log("connected to master");
            if (startingMatch)
                PhotonNetwork.JoinOrCreateRoom(PlayerManager.QuickMatchId, null, null);
            else
                AttemptToJoinRoom();
        }

        public override void OnCreatedRoom()
        {
            Debug.Log("Game Created");

            if (startingMatch)
                EnterMatch();
        }

        public override void OnJoinedRoom()
        {
            Debug.Log("Join Game Successful");
            //SceneManager.LoadScene("QuickMatch");

            if (startingMatch)
            {
                EnterMatch();
                return;
            }

            connected = true;
            UpdateUserListUI();

        }

        public override void OnCreateRoomFailed(short returnCode, string message)
        {
            Debug.Log("Create Game Failed: " + message);
            CreateRoom();
        }

        public override void OnJoinRoomFailed(short returnCode, string message)
        {
            Debug.Log("Join Game Failed: " + message);
            CreateRoom();
        }

        public override void OnJoinRandomFailed(short returnCode, string message)
        {
            Debug.Log("Join Random Game Failed: " + message);
            CreateRoom();
        }

        #endregion

        #region CUSTOM PUBLIC

        public void OnMainMenuButtonPress()
        {
            PhotonNetwork.Disconnect();
            SceneManager.LoadScene("MainMenu");
        }

        public void ConnectToPhoton()
        {
            // use nickname to connect to Photon
            if (PlayerPrefs.HasKey("screenName"))
            {
                PhotonNetwork.LocalPlayer.NickName = PlayerPrefs.GetString("screenName");
            }
            else
            {
                PhotonNetwork.LocalPlayer.NickName = PlayerManager.PlayerStats.PlayerName;
            }

            PhotonNetwork.ConnectUsingSettings();
        }

        public void RequestedMatch(string opponentName)
        {

            if (challenges.ContainsKey(opponentName))
            { // has already been challenged... start match
                PlayerManager.OpponentName = opponentName;
                PlayerManager.OpponentId = challenges[opponentName];
                StartMatch(opponentName, true);
                return;
            }
            else
            {  // has not yet been challenged, send event
                Debug.Log("match requested, event sent");
                var data = new object[] {
                    PhotonNetwork.NickName,
                    opponentName,
                    PlayerPrefs.GetString("playFabId")
                };

                // 2nd press on same request means cancel without issueing a new request
                if (requestedName == opponentName)
                {
                    RequestCancelled(requestedName);
                    return;
                }

                // if request exists, cancel first
                if (requestedName != "")
                {
                    RequestCancelled(requestedName);
                }

                requestedName = opponentName;

                PhotonNetwork.RaiseEvent(
                    REQUEST_MATCH_EVENT,        // .Code
                    data,                       // .CustomData
                    RaiseEventOptions.Default,
                    SendOptions.SendReliable);
            }

            UpdateUserListUI();
        }
        public void RequestCancelled(string opponentName)
        {
            Debug.Log("match request cancelled, event sent");
            var button = buttonArray[userButtonMap[opponentName]];
            button.RequestCancelled();

            var data = new object[] {
                PhotonNetwork.NickName,
                opponentName,
            };

            PhotonNetwork.RaiseEvent(
                CANCEL_MATCH_EVENT,         // .Code
                data,                       // .CustomData
                RaiseEventOptions.Default,
                SendOptions.SendReliable);

            requestedName = ""; // reset to null value

            UpdateUserListUI();
        }


        #endregion

        #region CUSTOM PRIVATE

        // custom properties for plaer
        private void SetPlayerProperties()
        {
            Hashtable properties = new Hashtable()
            {
                {"Favors", PlayerManager.PlayerStats.FavoriteWeapon.ToString()},
                {"Wins",   PlayerManager.PlayerStats.Wins},
                {"Losses", PlayerManager.PlayerStats.Losses},
            };

            PhotonNetwork.LocalPlayer.SetCustomProperties(properties);
        }

        // player to send final request in match making is labeled as the "HOST"
        private void StartMatch(string opponentName, bool isHost)
        {
            // generate quick match Id and save it for later
            string[] nameArray = { opponentName, PhotonNetwork.NickName };
            PlayerManager.OpponentName = opponentName;
            Array.Sort(nameArray);
            PlayerManager.QuickMatchId = "quickmatch:" + nameArray[0] + "_" + nameArray[1];

            Debug.Log("attempting to enter quickmatch with id: " + PlayerManager.QuickMatchId);

            PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
            {
                FunctionName = "JoinQuickMatch",
                FunctionParameter = new
                {
                    IsHost = isHost,
                    QuickMatchId = PlayerManager.QuickMatchId,
                },
                GeneratePlayStreamEvent = true,
            },
                success =>
                {
                    if (isHost)
                        InitializeGameStartState();
                    else
                        RequestEnterMatch();
                },
                error =>
                {
                    Debug.Log(error.ErrorMessage + "error attempting to join game");
                }
            );
        }

        private void InitializeGameStartState()
        {
            GameSettings gameSettings = new GameSettings();
            PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
            {
                FunctionName = "InitializeGameStartState",
                FunctionParameter = new
                {
                    sharedGroupId = PlayerManager.QuickMatchId,
                    gameSettings = gameSettings.ToJSON(),
                    opponentId = PlayerManager.OpponentId,
                    opponentName = PlayerManager.OpponentName,
                    hostName = PlayerManager.PlayerStats.PlayerName
                },
                GeneratePlayStreamEvent = true,
            },
            OnSuccess =>
            {
                // send match created event
                var data = new object[] {
                        PhotonNetwork.NickName,
                        PlayerManager.OpponentName,
                        PlayerPrefs.GetString("playFabId")
                    };
                PhotonNetwork.RaiseEvent(
                    CREATED_MATCH_EVENT,        // .Code
                    data,                       // .CustomData
                    RaiseEventOptions.Default,
                    SendOptions.SendReliable
                );

                Debug.Log("new game room created, new game states initialized");
                RequestEnterMatch();
            },
            errorCallback =>
            {
                Debug.Log(errorCallback.ErrorMessage + "error attempting to initialize game state.");
            }
            );
        }

        private void RequestEnterMatch()
        {
            // removes player from finding quickmatch player list
            startingMatch = true;
            PhotonNetwork.LeaveRoom();
        }

        private void EnterMatch()
        {
            Debug.Log("successfully joined quickmatch with id: " + PlayerManager.QuickMatchId);

            // load match in client
            SceneManager.LoadScene("QuickMatch");
        }

        private void ReceiveCustomPUNEvents(EventData obj)
        {
            // get data from event
            object[] data = (object[])obj.CustomData;
            if (data == null) return; // invalid data 
            string receiverName = (string)data[1];
            string senderName = (string)data[0];

            // check if you are the receiver for this event
            if (PhotonNetwork.NickName != receiverName) return;

            // switch to correct function
            switch (obj.Code)
            {
                case REQUEST_MATCH_EVENT:
                    string senderId = (string)data[2];
                    IncomingChallenge(senderName, senderId);
                    break;
                case CANCEL_MATCH_EVENT:
                    ChallengeCancelled(senderName);
                    break;
                case CREATED_MATCH_EVENT:
                    PlayerManager.OpponentId = (string)data[2];
                    StartMatch(senderName, false);
                    break;
            }
        }

        private void IncomingChallenge(string opponentName, string opponentId)
        {
            challenges.Add(opponentName, opponentId);
            var button = buttonArray[userButtonMap[opponentName]];
            button.Challenged();

            // we dont update UI in this case because we expect a CREATED_MATCH_EVENT event to come shortly
            if (requestedName == opponentName)
                return;

            UpdateUserListUI();
        }
        private void ChallengeCancelled(string opponentName)
        {
            challenges.Remove(opponentName);
            var button = buttonArray[userButtonMap[opponentName]];
            button.ChallengeCancelled();

            UpdateUserListUI();
        }

        private void UpdatePlayerUI()
        {
            var stats = PlayerManager.PlayerStats;
            stats.PlayerName = PhotonNetwork.NickName;
            playerNameText.text = stats.PlayerName;
            playerStatsText.text =
                "Wins\t" + stats.Wins.ToString() +
                "\n" +
                "Losses\t" + stats.Losses.ToString() +
                "\n" +
                "Favors\t" + stats.FavoriteWeapon.ToString();
        }
        private void UpdateUserListUI()
        {
            if (connected)
            {
                userButtonMap.Clear();

                int i = 0;
                foreach (var player in PhotonNetwork.PlayerList)
                {
                    // skip self
                    if (player == PhotonNetwork.LocalPlayer)
                        continue;

                    var button = buttonArray[i];

                    PlayerStatsBrief stats = new PlayerStatsBrief(
                        player.NickName,
                        (string)player.CustomProperties["Favors"],
                        (int)player.CustomProperties["Wins"],
                        (int)player.CustomProperties["Losses"]
                    );

                    button.gameObject.SetActive(true);  // Switch button on
                    button.SetButtonText(stats);        // TODO: get real user stats
                    userButtonMap[player.NickName] = i; // map name to index for retrieval later on

                    // iterate until the capacity of userArray is reached
                    if (++i == buttonArray.Length)
                        break;
                }

                totalOpponents = i;

                // disable unused buttons
                for (; i < buttonArray.Length; i++)
                {
                    buttonArray[i].gameObject.SetActive(false);
                }
            }
            else
            {
                // hide buttons 
                foreach (var user in buttonArray)
                    user.gameObject.SetActive(false);
            }

            UpdateUserListLabel();
        }

        private void AttemptToJoinRoom()
        {
            Debug.Log("attempting to join random room");
            PhotonNetwork.JoinRandomRoom();
        }

        private void CreateRoom()
        {
            if (CreateRoomAttempts < MaxCreateRoomAttemps) // only attempt to create a room 3 times
            {
                // Create room
                RoomOptions options = new RoomOptions { MaxPlayers = 100 };
                string roomName = "quickmatch-lobby";
                Debug.Log("attempting to create room named: " + roomName);
                PhotonNetwork.CreateRoom(roomName, options);
                ++CreateRoomAttempts;
            }
            else
            {
                Debug.Log(MaxCreateRoomAttemps.ToString() + " failures to create game, returning to Main Menu");
                SceneManager.LoadScene("MainMenu");
            }
        }


        private void UpdateUserListLabel()
        {
            if (!connected)
            {
                SelectUserListLabel(connectingLabel);
                return;
            }

            if (totalOpponents == 0)
            {
                SelectUserListLabel(waitingLabel);
                return;
            }

            if (requestedName != "")
            {
                SelectUserListLabel(requestedLabel);
                return;
            }

            if (challenges.Count > 0)
            {
                SelectUserListLabel(challengeLabel);
                return;
            }

            SelectUserListLabel(selectLabel);
        }

        private void SelectUserListLabel(GameObject selection)
        {
            connectingLabel.SetActive(false);
            waitingLabel.SetActive(false);
            requestedLabel.SetActive(false);
            challengeLabel.SetActive(false);
            selectLabel.SetActive(false);

            selection.SetActive(true);
        }

        #endregion
    }
}
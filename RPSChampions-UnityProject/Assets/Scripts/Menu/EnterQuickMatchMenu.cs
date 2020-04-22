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
        private HashSet<string> challenges = new HashSet<string>();
        private Dictionary<string, int> userButtonMap = new Dictionary<string, int>();
        private int totalOpponents = 0;
        private bool connected = false;

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
            Debug.Log("connected to master");
            AttemptToJoinRoom();
        }

        public override void OnCreatedRoom()
        {
            Debug.Log("Game Created");
        }

        public override void OnJoinedRoom()
        {
            Debug.Log("Join Game Successful");
            //SceneManager.LoadScene("QuickMatch");

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
            Debug.Log("match requested, event sent");
            var data = new object[] {
                PhotonNetwork.NickName,
                opponentName,
            };

            // if request exists, cancel first
            if (requestedName != "")
                RequestCancelled(requestedName);

            requestedName = opponentName;

            PhotonNetwork.RaiseEvent(
                REQUEST_MATCH_EVENT,        // .Code
                data,                       // .CustomData
                RaiseEventOptions.Default,
                SendOptions.SendReliable);

            if (challenges.Contains(opponentName))
            {
                StartMatch(opponentName);
                return;
            }

            UpdateUserListUI();
        }
        public void RequestCancelled(string opponentName)
        {
            Debug.Log("match request cancelled, event sent");
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

            UpdateUserListUI(); // UI update
        }


        #endregion

        #region CUSTOM PRIVATE

        private void StartMatch(string opponentName)
        {
            string[] nameArray = { opponentName, PhotonNetwork.NickName };
            PlayerManager.OpponentName = opponentName;
            Array.Sort(nameArray);
            PlayerManager.Room = "quickmatch:" + nameArray[0] + "_" + nameArray[1];
            Debug.Log("entering game with room name: " + PlayerManager.Room);
            //PhotonNetwork.Disconnect();
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
                    IncomingChallenge(senderName);
                    break;
                case CANCEL_MATCH_EVENT:
                    ChallengeCancelled(senderName);
                    break;
            }
        }

        private void IncomingChallenge(string opponentName)
        {
            challenges.Add(opponentName);
            var button = buttonArray[userButtonMap[opponentName]];
            button.Challenged();

            if (requestedName == opponentName)
            {
                StartMatch(opponentName);
                return;
            }

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
                    var stats = new PlayerStats(player.NickName);

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
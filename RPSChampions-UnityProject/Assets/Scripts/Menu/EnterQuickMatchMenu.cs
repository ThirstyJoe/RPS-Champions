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

    public class EnterQuickMatchMenu : MonoBehaviourPunCallbacks
    {
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
        private ChallengePlayerButton[] userArray;

        private int CreateRoomAttempts = 0;
        private int MaxCreateRoomAttemps = 3;
        private int requestedIndex = -1;
        private int challengedIndex = -1;
        private int totalOpponents = 0;
        private bool connected = false;
        #endregion

        #region UNITY

        private void Start()
        {
            UpdatePlayerUI();
            UpdateUserListUI();
            ConnectToPhoton();
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
                PhotonNetwork.LocalPlayer.NickName = "Newbie"; // TODO: select from random name list
            }

            PhotonNetwork.ConnectUsingSettings();
        }

        #endregion

        #region CUSTOM PRIVATE

        private void UpdatePlayerUI()
        {
            var stats = PlayerManager.PlayerStats;
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
                var playerList = new StringBuilder();

                int i = 0;
                foreach (var player in PhotonNetwork.PlayerList)
                {
                    // skip self
                    // if (player == PhotonNetwork.LocalPlayer)
                    //     continue;

                    userArray[i].gameObject.SetActive(true);
                    var stats = new PlayerStats(player.NickName);
                    userArray[i].SetButtonText(stats); // TODO: get real user stats

                    // iterate until the capacity of userArray is reached
                    if (++i == userArray.Length)
                        break;
                }

                totalOpponents = i;

                // disable unused buttons
                for (; i < userArray.Length; i++)
                {
                    userArray[i].gameObject.SetActive(false);
                }
            }
            else
            {
                // hide buttons 
                foreach (var user in userArray)
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

            if (requestedIndex != -1)
            {
                SelectUserListLabel(requestedLabel);
                return;
            }

            if (challengedIndex != -1)
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
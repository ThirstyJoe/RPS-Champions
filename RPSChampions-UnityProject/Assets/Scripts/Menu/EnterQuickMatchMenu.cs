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

    public class EnterQuickMatchMenu : MonoBehaviourPunCallbacks
    {
        #region PRIVATE VARS
        [SerializeField]
        private TextMeshProUGUI progressText;

        private int CreateRoomAttempts = 0;
        private int MaxCreateRoomAttemps = 3;

        #endregion

        #region UNITY

        private void Start()
        {
            ConnectToPhoton();
        }

        #endregion

        #region PUN

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
            SceneManager.LoadScene("QuickMatch");
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

        public void ConnectToPhoton()
        {
            // use nickname to connect to Photon
            PhotonNetwork.LocalPlayer.NickName = PlayerPrefs.GetString("screenName");
            PhotonNetwork.ConnectUsingSettings();
        }

        #endregion

        #region CUSTOM PRIVATE

        private void AttemptToJoinRoom()
        {
            Debug.Log("attempting to join random room");
            PhotonNetwork.JoinRandomRoom(); //(null, 2);
        }

        private void CreateRoom()
        {
            if (CreateRoomAttempts < MaxCreateRoomAttemps) // only attempt to create a room 3 times
            {
                // Create room
                RoomOptions options = new RoomOptions { MaxPlayers = 2 };
                string roomName = "quickmatch:" + PlayerPrefs.GetString("screenName");
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

        #endregion
    }
}
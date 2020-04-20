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
            JoinRoom();
        }

        public override void OnJoinedRoom()
        {
            SceneManager.LoadScene("QuickMatch");
        }

        public override void OnJoinRoomFailed(short returnCode, string message)
        {
            SceneManager.LoadScene("MainMenu");
            // TODO: alert player to reason they are returned to Main Menu
            Debug.Log("Join Game Failed: " + message);
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

        private void JoinRoom()
        {
            // Create room
            RoomOptions options = new RoomOptions { MaxPlayers = 2 };
            PhotonNetwork.JoinOrCreateRoom("Test Game", options, null);
        }

        #endregion
    }
}
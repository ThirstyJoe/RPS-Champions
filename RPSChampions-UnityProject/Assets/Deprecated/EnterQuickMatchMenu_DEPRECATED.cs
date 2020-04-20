namespace ThirstyJoe.RPSChampions
{
    using UnityEngine;
    using Photon.Pun;
    using ExitGames.Client.Photon;
    using Photon.Realtime;
    using System.Collections.Generic;
    using UnityEngine.UI;
    using PlayFab;
    using PlayFab.ClientModels;
    using UnityEngine.SceneManagement;

    public class EnterQuickMatchMenu_DEPRECATED : MonoBehaviourPunCallbacks
    {
        #region PRIVATE VARS

        [Tooltip("The UI Panel to let the user enter name, connect and play")]
        [SerializeField]
        private GameObject loginPanel;

        [Tooltip("The UI Panel to inform the user that the connection is in progress")]
        [SerializeField]
        private GameObject progressPanel;

        [SerializeField]
        private InputField nameInputField;

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
            // UI State
            loginPanel.SetActive(true);
            progressPanel.SetActive(false);

            Debug.Log("Join Game Failed: " + message);
        }

        #endregion

        #region PLAYFAB


        #endregion
        #region CUSTOM PUBLIC

        public void OnEnterGameButtonPressed()
        {
            // retrieve desired nickname from input field
            if (PlayerPrefs.HasKey("displayName"))
            {
                nameInputField.text = PlayerPrefs.GetString("displayName");
            };
            var nickname = nameInputField.text;
            if (nickname == "")
            {
                // some default name to use for easier testing
                nickname = "Guest";
            }

            // filter nickname through a PlayFab displayName request
            PlayFabClientAPI.UpdateUserTitleDisplayName(new UpdateUserTitleDisplayNameRequest
            { DisplayName = nickname },
            OnDisplayName =>
            {
                Debug.Log(OnDisplayName.DisplayName + " is your display name");
                nickname = OnDisplayName.DisplayName;
            },
            errorCallback =>
            {
                Debug.Log(errorCallback.ErrorMessage + " error with display name, possibly in use");
            });

            // use nickname to connect to Photon
            PhotonNetwork.LocalPlayer.NickName = nickname;
            PhotonNetwork.ConnectUsingSettings();

            // UI State change
            loginPanel.SetActive(false);
            progressPanel.SetActive(true);
        }

        #endregion

        #region CUSTOM PRIVATE

        private void JoinRoom()
        {
            // Create room
            RoomOptions options = new RoomOptions { MaxPlayers = 8 };
            PhotonNetwork.JoinOrCreateRoom("Test Game", options, null);
        }

        #endregion
    }
}
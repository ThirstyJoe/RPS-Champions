namespace ThirstyJoe.GalaxyBound
{
    using UnityEngine;
    using Photon.Pun;
    using ExitGames.Client.Photon;
    using Photon.Realtime;
    using System.Collections.Generic;
    using UnityEngine.UI;
    using PlayFab;
    using PlayFab.ClientModels;

    public class LoginController : MonoBehaviourPunCallbacks
    {
        #region PRIVATE VARS


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
            Debug.Log("Joined Game");
            Application.LoadLevel("TestGame");
        }

        public override void OnJoinRoomFailed(short returnCode, string message)
        {
            LoginUI.EnterAttemptingLoginState();
            Debug.Log("Join Game Failed: " + message);
        }

        #endregion

        #region PLAYFAB


        #endregion
        #region CUSTOM PUBLIC

        public void OnEnterGameButtonPressed()
        {
            // retrieve desired nickname from input field
            var nickname = nameInputField.text;
            if (nickname == "")
            {
                // some default name to use for easier testing
                nickname = "Mr. Nothington";
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
            LoginUI.EnterAttemptingLoginState();
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
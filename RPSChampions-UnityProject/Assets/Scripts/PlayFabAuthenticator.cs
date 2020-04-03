namespace ThirstyJoe.GalaxyBound
{
    using PlayFab;
    using PlayFab.ClientModels;
    using UnityEngine;
    using Photon.Pun;
    using ExitGames.Client.Photon;
    using Photon.Realtime;
    using System.Collections.Generic;

    public class PlayFabAuthenticator : MonoBehaviour
    {

        private string _playFabPlayerIdCache;


        //Run the entire thing on awake
        public void Awake()
        {
            AuthenticateWithPlayFab();
            DontDestroyOnLoad(gameObject);
        }

        /*
         * Step 1
         * We authenticate current PlayFab user normally.
         * In this case we use LoginWithCustomID API call for simplicity.
         * You can absolutely use any Login method you want.
         * We use PlayFabSettings.DeviceUniqueIdentifier as our custom ID.
         * We pass RequestPhotonToken as a callback to be our next step, if
         * authentication was successful.
         */
        private void AuthenticateWithPlayFab()
        {
            Debug.Log("PlayFab authenticating using Custom ID...");

#if UNITY_ANDROID
            // android authentication
            PlayFabClientAPI.LoginWithAndroidDeviceID(new LoginWithAndroidDeviceIDRequest()
            {
                CreateAccount = true,
                AndroidDeviceId = PlayFabSettings.DeviceUniqueIdentifier
            }, RequestPhotonToken, OnPlayFabError);
#else
            // development authentication
            PlayFabClientAPI.LoginWithCustomID(new LoginWithCustomIDRequest()
            {
                CreateAccount = true,
                CustomId = PlayFabSettings.DeviceUniqueIdentifier
            }, RequestPhotonToken, OnPlayFabError);
#endif

        }

        /*
        * Step 2
        * We request Photon authentication token from PlayFab.
        * This is a crucial step, because Photon uses different authentication tokens
        * than PlayFab. Thus, you cannot directly use PlayFab SessionTicket and
        * you need to explicitly request a token. This API call requires you to
        * pass Photon App ID. App ID may be hard coded, but, in this example,
        * We are accessing it using convenient static field on PhotonNetwork class
        * We pass in AuthenticateWithPhoton as a callback to be our next step, if
        * we have acquired token successfully
        */
        private void RequestPhotonToken(LoginResult obj)
        {
            Debug.Log("PlayFab authenticated. Requesting photon token...");
            // Save the player PlayFabId. This will come in handy during next step
            _playFabPlayerIdCache = obj.PlayFabId;

            PlayFabClientAPI.GetPhotonAuthenticationToken(new GetPhotonAuthenticationTokenRequest()
            {
                PhotonApplicationId = PhotonNetwork.PhotonServerSettings.AppSettings.AppIdRealtime
            }, AuthenticateWithPhoton, OnPlayFabError);
        }

        /*
         * Step 3
         * This is the final and the simplest step. We create new AuthenticationValues instance.
         * This class describes how to authenticate a player inside Photon environment.
         */
        private void AuthenticateWithPhoton(GetPhotonAuthenticationTokenResult obj)
        {
            Debug.Log("Photon token acquired: " + obj.PhotonCustomAuthenticationToken + "  Authentication complete.");

            //We set AuthType to custom, meaning we bring our own, PlayFab authentication procedure.
            var customAuth = new AuthenticationValues { AuthType = CustomAuthenticationType.Custom };
            //We add "username" parameter. Do not let it confuse you: PlayFab is expecting this parameter to contain player PlayFab ID (!) and not username.
            customAuth.AddAuthParameter("username", _playFabPlayerIdCache);    // expected by PlayFab custom auth service

            //We add "token" parameter. PlayFab expects it to contain Photon Authentication Token issues to your during previous step.
            customAuth.AddAuthParameter("token", obj.PhotonCustomAuthenticationToken);

            //We finally tell Photon to use this authentication parameters throughout the entire application.
            PhotonNetwork.AuthValues = customAuth;
        }

        private void OnPlayFabError(PlayFabError obj)
        {
            Debug.Log(obj.ErrorMessage);
        }

        // Add small button to launch our example code
        // public void OnGUI()
        // {
        //     if (GUILayout.Button("Execute Example")) ExecuteExample();
        // }


        // Example code which raises custom room event, then sets custom room property
        private void ExecuteExample()
        {
            // Raise custom room event
            var data = new Dictionary<string, object>() { { "Hello", "World" } };
            var flags = new WebFlags(WebFlags.HttpForwardConst);
            var result = PhotonNetwork.RaiseEvent(
                15, data,
                new RaiseEventOptions() { Flags = flags },
                new SendOptions()
            );
            Debug.Log("New Room Event Post: " + result);

            // Set custom room property
            var properties = new ExitGames.Client.Photon.Hashtable() { { "CustomProperty", "It's Value" } };
            var expectedProperties = new ExitGames.Client.Photon.Hashtable();
            PhotonNetwork.CurrentRoom.SetCustomProperties(properties, expectedProperties, flags);
            Debug.Log("New Room Properties Set");
        }
    }
}
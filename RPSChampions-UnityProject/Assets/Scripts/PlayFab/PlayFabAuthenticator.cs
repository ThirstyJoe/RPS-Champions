namespace ThirstyJoe.RPSChampions
{
    using PlayFab;
    using PlayFab.ClientModels;
    using UnityEngine;
    using Photon.Pun;
    using Photon.Realtime;
    using UnityEngine.Events;

    public class PlayFabAuthenticator : Singleton<PlayFabAuthenticator>
    {
        private static string playFabId;
        public static string screenName;
        public static bool authenticated = false;


        // Prevent non-singleton constructor use.
        protected PlayFabAuthenticator() { }

        //Run the entire thing on awake
        public void Start()
        {
            AuthenticateWithPlayFab();
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
        public static void AuthenticateWithPlayFab()
        {
            Debug.Log("PlayFab authenticating using Custom ID...");

#if UNITY_ANDROID
            // android authentication
            PlayFabClientAPI.LoginWithAndroidDeviceID(new LoginWithAndroidDeviceIDRequest()
            {
                CreateAccount = true,
                AndroidDeviceId = PlayFabSettings.DeviceUniqueIdentifier
            }, Authenticated, OnPlayFabError);
#else
            // development authentication
            PlayFabClientAPI.LoginWithCustomID(new LoginWithCustomIDRequest()
            {
                CreateAccount = true,
                CustomId = PlayFabSettings.DeviceUniqueIdentifier
            }, Authenticated, OnPlayFabError);
#endif

        }

        public static void LogOut()
        {
            PlayFabClientAPI.ForgetAllCredentials();
            screenName = null;
            playFabId = null;
        }

        public static void Authenticated(LoginResult loginResult)
        {
            Debug.Log("PlayFab authenticated. Requesting photon token...");

            // flag that we have done this
            authenticated = true;

            // Save the player PlayFabId. This will come in handy during next step
            playFabId = loginResult.PlayFabId;

            // move onto Photon Authentication
            RequestPhotonToken();

            // player profile
            PlayFabClientAPI.GetPlayerProfile(new GetPlayerProfileRequest()
            {
                PlayFabId = playFabId,
            }, PlayerProfileReceived, OnPlayFabError);
        }

        private static void PlayerProfileReceived(GetPlayerProfileResult result)
        {
            screenName = result.PlayerProfile.DisplayName;

            Debug.Log("Player profile received... screenName saved: " + screenName);

            // send event
            EventManager.TriggerEvent("PlayerProfileReceived");
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
        private static void RequestPhotonToken()
        {
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
        private static void AuthenticateWithPhoton(GetPhotonAuthenticationTokenResult obj)
        {
            Debug.Log("Photon token acquired: " + obj.PhotonCustomAuthenticationToken + "  Authentication complete.");

            //We set AuthType to custom, meaning we bring our own, PlayFab authentication procedure.
            var customAuth = new AuthenticationValues { AuthType = CustomAuthenticationType.Custom };
            //We add "username" parameter. Do not let it confuse you: PlayFab is expecting this parameter to contain player PlayFab ID (!) and not username.
            customAuth.AddAuthParameter("username", playFabId);    // expected by PlayFab custom auth service

            //We add "token" parameter. PlayFab expects it to contain Photon Authentication Token issues to your during previous step.
            customAuth.AddAuthParameter("token", obj.PhotonCustomAuthenticationToken);

            //We finally tell Photon to use this authentication parameters throughout the entire application.
            PhotonNetwork.AuthValues = customAuth;
        }

        public static void OnPlayFabError(PlayFabError obj)
        {
            Debug.Log(obj.ErrorMessage);
        }
    }
}
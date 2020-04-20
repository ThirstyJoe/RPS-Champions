namespace ThirstyJoe.RPSChampions
{

    using UnityEngine.SceneManagement;
    using UnityEngine;
    using TMPro;
    using PlayFab;
    using PlayFab.ClientModels;

    public class MainMenu : MonoBehaviour
    {
        [SerializeField]
        private GameObject logInButton;
        [SerializeField]
        private GameObject accountButton;


        private void Start()
        {
            if (PlayerPrefs.HasKey("screenName"))
            {
                // player was logged in previously
                TextMeshProUGUI buttonText = accountButton.GetComponentInChildren<TextMeshProUGUI>();
                buttonText.text = PlayerPrefs.GetString("screenName");
                logInButton.SetActive(false);
                accountButton.SetActive(true);

                // log in
                var request = new LoginWithPlayFabRequest
                {
                    Username = PlayerPrefs.GetString("screenName"),
                    Password = PlayerPrefs.GetString("password"),
                    TitleId = PlayFabSettings.TitleId
                };
                PlayFabClientAPI.LoginWithPlayFab(
                    request,
                    OnSuccess =>
                    {
                        Debug.Log("successfully logged into: " + PlayerPrefs.GetString("screenName"));
                    },
                    errorCallback =>
                    {
                        Debug.Log("Logging out due to error: " + errorCallback.ErrorMessage);
                        PlayFabAuthenticator.LogOut();
                        DefaultLogIn();
                    }
                );
            }
            else
            {
                DefaultLogIn();
            }
        }

        public void DefaultLogIn()
        {
            // player is not fully logged in
            logInButton.SetActive(true);
            accountButton.SetActive(false);
            PlayFabAuthenticator.AuthenticateWithPlayFab(); // get a default device login
        }

        public void OnOuickMatchButtonPress()
        {
            SceneManager.LoadScene("EnterQuickMatch");
        }
        public void OnLeaguePlayerButtonPress()
        {
            SceneManager.LoadScene("LeaguePlayDashboard");
        }
        public void OnPracticeButtonPress()
        {
            SceneManager.LoadScene("PracticeGame");
        }
        public void OnLogInButtonPress()
        {
            SceneManager.LoadScene("LogIn");
        }
        public void OnAccountButtonPress()
        {
            SceneManager.LoadScene("Account");
        }

    }
}

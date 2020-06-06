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

        private bool loggedIn = false;


        private void Start()
        {
            if (PlayerPrefs.HasKey("screenName") && PlayerPrefs.HasKey("password"))
            {
                loggedIn = true;

                // player was logged in previously
                TextMeshProUGUI buttonText = accountButton.GetComponentInChildren<TextMeshProUGUI>();
                buttonText.text = PlayerPrefs.GetString("screenName");
                logInButton.SetActive(false);
                accountButton.SetActive(true);

                PlayFabAuthenticator.AuthenticateWithPlayFab(); // get a default device login
            }
            else
            {
                loggedIn = false;

                // player is not fully logged in
                logInButton.SetActive(true);
                accountButton.SetActive(false);
                PlayFabAuthenticator.AuthenticateWithPlayFab(); // get a default device login
            }
        }

        public void OnOuickMatchButtonPress()
        {
            SceneManager.LoadScene("EnterQuickMatch");
        }
        public void OnLeaguePlayButtonPress()
        {
            Debug.Log(loggedIn);
            if (loggedIn)
            {
                SceneManager.LoadScene("LeagueDashboard");
            }
            else
            {
                SceneManager.LoadScene("LogIn");
                LeagueManager.redirectLoginToLeague = true;
            }
        }
        public void OnPracticeButtonPress()
        {
            SceneManager.LoadScene("PracticeGame");
        }
        public void OnLogInButtonPress()
        {
            LeagueManager.redirectLoginToLeague = false;
            SceneManager.LoadScene("LogIn");
        }
        public void OnAccountButtonPress()
        {
            SceneManager.LoadScene("Account");
        }

    }
}

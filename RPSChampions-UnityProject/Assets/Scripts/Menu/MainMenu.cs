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

                PlayFabAuthenticator.AuthenticateWithPlayFab(); // get a default device login
            }
            else
            {
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

namespace ThirstyJoe.RPSChampions
{

    using UnityEngine.SceneManagement;
    using UnityEngine;
    using TMPro;

    public class MainMenu : MonoBehaviour
    {
        [SerializeField]
        private GameObject logInButton;
        [SerializeField]
        private GameObject accountButton;


        void OnEnable()
        {
            EventManager.StartListening("PlayerProfileReceived", UpdateLogInButton);
        }
        void OnDisable()
        {
            EventManager.StopListening("PlayerProfileReceived", UpdateLogInButton);
        }

        private void Start()
        {
            if (PlayFabAuthenticator.authenticated)
            {
                UpdateLogInButton();
            }
            else
            {
                PlayFabAuthenticator.AuthenticateWithPlayFab();
            }
        }

        private void UpdateLogInButton()
        {
            if (PlayFabAuthenticator.screenName == null)
            {   // player is not fully logged in
                logInButton.SetActive(true);
                accountButton.SetActive(false);
            }
            else
            {   // player is logged in
                TextMeshProUGUI buttonText = accountButton.GetComponentInChildren<TextMeshProUGUI>();
                buttonText.text = PlayFabAuthenticator.screenName;
                logInButton.SetActive(false);
                accountButton.SetActive(true);
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

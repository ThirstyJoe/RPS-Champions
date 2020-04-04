namespace ThirstyJoe.RPSChampions
{

    using UnityEngine.SceneManagement;
    using UnityEngine;

    public class WelcomeMenuLogic : MonoBehaviour
    {
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
    }
}

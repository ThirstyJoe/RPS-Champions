
namespace ThirtyJoe.RPSChampions
{
    using UnityEngine.SceneManagement;
    using UnityEngine;
    using TMPro;

    public class LogInMenu : MonoBehaviour
    {
        [SerializeField]
        private GameObject errorPanel;
        [SerializeField]
        private TextMeshProUGUI errorTitleText;
        [SerializeField]
        private TextMeshProUGUI errorMessageText;

        public void OnConfirmErrorButtonPress()
        {
            errorPanel.SetActive(false);
        }
        public void OnCreateAccountButtonPress()
        {
            SceneManager.LoadScene("CreateAccount");
        }
        public void OnLogInButtonPress()
        {
            // attempt to log in and return to main menu or error notification
            SceneManager.LoadScene("MainMenu");

            // on sign up error...
            // errorPanel.SetActive(true);
            // errorTitleText.text = "Password change error";
            // errorMessageText.text = "OMG! you got the password change done all wrong.";
        }
        public void OnForgetPassButtonPress()
        {
            // pop up confirmation:
            // "Are you sure you want to reset your password? 
            // A password reset link will be emailed to you."
            // [NO] [YES]
            SceneManager.LoadScene("PasswordResetConfirmation", LoadSceneMode.Additive);
        }

        public void OnMainMenuButtonPress()
        {
            SceneManager.LoadScene("MainMenu");
        }
    }
}

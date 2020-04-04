
namespace ThirtyJoe.RPSChampions
{
    using UnityEngine.SceneManagement;
    using UnityEngine;

    public class LogInMenuLogic : MonoBehaviour
    {
        public void OnCreateAccountButtonPress()
        {
            SceneManager.LoadScene("CreateAccount");
        }
        public void OnLogInButtonPress()
        {
            // attempt to log in and return to main menu or error notification
            SceneManager.LoadScene("MainMenu");
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


namespace ThirtyJoe.RPSChampions
{
    using UnityEngine.SceneManagement;
    using UnityEngine;
    using TMPro;

    public class ChangePasswordMenu : MonoBehaviour
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
        public void OnChangePasswordButtonPress()
        {
            // check if fields are valid

            // if invalid
            errorPanel.SetActive(true);
            errorTitleText.text = "Password change error";
            errorMessageText.text = "OMG! you got the password change done all wrong.";

            // if valid...
            // SceneManager.LoadScene("Account");
            // SceneManager.LoadScene("PasswordChangeConfirmation", LoadSceneMode.Single);
        }
        public void OnCancelButtonPress()
        {
            SceneManager.LoadScene("Account");
        }
    }
}

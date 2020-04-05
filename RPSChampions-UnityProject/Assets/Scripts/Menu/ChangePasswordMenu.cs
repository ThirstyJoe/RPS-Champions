
namespace ThirtyJoe.RPSChampions
{
    using UnityEngine.SceneManagement;
    using UnityEngine;
    using TMPro;
    public class ChangePasswordMenu : MonoBehaviour
    {
        [SerializeField]
        private GameObject SuccessPanel;
        [SerializeField]
        private TextMeshProUGUI ErrorTitleText;
        [SerializeField]
        private TextMeshProUGUI ErrorMessageText;

        public void OnConfirmErrorButtonPress()
        {
            SuccessPanel.SetActive(false);
        }
        public void OnChangePasswordButtonPress()
        {
            // check if fields are valid

            // if invalid
            SuccessPanel.SetActive(true);
            ErrorTitleText.text = "Password change error";
            ErrorMessageText.text = "OMG! you got the password change done all wrong.";

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

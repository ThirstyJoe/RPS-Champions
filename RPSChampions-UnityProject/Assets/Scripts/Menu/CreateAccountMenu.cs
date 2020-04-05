
namespace ThirtyJoe.RPSChampions
{
    using UnityEngine.SceneManagement;
    using UnityEngine;
    using TMPro;

    public class CreateAccountMenu : MonoBehaviour
    {
        [SerializeField]
        private GameObject errorPanel;
        [SerializeField]
        private TextMeshProUGUI errorTitleText;
        [SerializeField]
        private TextMeshProUGUI errorMessageText;

        public void OnCancelButtonPress()
        {
            SceneManager.LoadScene("MainMenu");
        }
        public void OnSignUpButtonPress()
        {
            SceneManager.LoadScene("MainMenu");

            // on sign up error...
            // errorPanel.SetActive(true);
            // errorTitleText.text = "Password change error";
            // errorMessageText.text = "OMG! you got the password change done all wrong.";
        }
        public void OnConfirmErrorButtonPress()
        {
            errorPanel.SetActive(false);
        }
    }
}

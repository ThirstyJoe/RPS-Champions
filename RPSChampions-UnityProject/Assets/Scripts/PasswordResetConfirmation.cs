
namespace ThirtyJoe.RPSChampions
{
    using UnityEngine.SceneManagement;
    using UnityEngine;


    public class PasswordResetConfirmation : MonoBehaviour
    {
        [SerializeField]
        private GameObject PasswordResetSuccessPanel;
        [SerializeField]
        private GameObject PasswordResetConfirmationPanel;

        public void OnCancelButtonPress()
        {
            SceneManager.UnloadSceneAsync("PasswordResetConfirmation");
        }

        public void OnConfirmButtonPress()
        {
            // send password recovery email

            PasswordResetConfirmationPanel.SetActive(false);
            PasswordResetSuccessPanel.SetActive(true);
        }

        public void OnSuccessButtonPress()
        {
            SceneManager.UnloadSceneAsync("PasswordResetConfirmation");
        }
    }
}

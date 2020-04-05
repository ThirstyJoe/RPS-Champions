
namespace ThirtyJoe.RPSChampions
{
    using UnityEngine.SceneManagement;
    using UnityEngine;


    public class DeleteAccountConfirmation : MonoBehaviour
    {
        [SerializeField]
        private GameObject SuccessPanel;
        [SerializeField]
        private GameObject ConfirmationPanel;

        public void OnCancelButtonPress()
        {
            SceneManager.UnloadSceneAsync("DeleteAccountConfirmation");
        }

        public void OnConfirmButtonPress()
        {
            // account is deleted

            ConfirmationPanel.SetActive(false);
            SuccessPanel.SetActive(true);
        }

        public void OnSuccessButtonPress()
        {
            SceneManager.UnloadSceneAsync("DeleteAccountConfirmation");
        }
    }
}


namespace ThirtyJoe.RPSChampions
{
    using UnityEngine.SceneManagement;
    using UnityEngine;
    public class AccountMenuLogic : MonoBehaviour
    {
        public void OnLogOutButtonPress()
        {
            SceneManager.LoadScene("MainMenu");
        }
        public void OnChangePasswordButtonPress()
        {
            SceneManager.LoadScene("PasswordChange");
        }
        public void OnDeleteAcountButtonPress()
        {
            SceneManager.LoadScene("DeleteAccountConfirmation", LoadSceneMode.Single);
        }
        public void OnMainMenuButtonPress()
        {
            SceneManager.LoadScene("MainMenu");
        }
    }
}

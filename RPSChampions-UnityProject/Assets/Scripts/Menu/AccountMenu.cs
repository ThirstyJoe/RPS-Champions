
namespace ThirstyJoe.RPSChampions
{
    using UnityEngine.SceneManagement;
    using UnityEngine;
    using TMPro;
    using PlayFab;

    public class AccountMenu : MonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI screenName;


        private void Start()
        {
            screenName.text = PlayFabAuthenticator.screenName;
        }

        public void OnLogOutButtonPress()
        {
            PlayFabAuthenticator.LogOut();
            SceneManager.LoadScene("MainMenu");
        }
        public void OnChangePasswordButtonPress()
        {
            SceneManager.LoadScene("PasswordChange");
        }
        public void OnDeleteAcountButtonPress()
        {
            SceneManager.LoadSceneAsync("DeleteAccountConfirmation", LoadSceneMode.Additive);
        }
        public void OnMainMenuButtonPress()
        {
            SceneManager.LoadScene("MainMenu");
        }
    }
}


namespace ThirstyJoe.RPSChampions
{
    using UnityEngine.SceneManagement;
    using UnityEngine;
    using PlayFab;
    using PlayFab.ClientModels;

    public class PasswordResetConfirmation : MonoBehaviour
    {
        [SerializeField]
        private GameObject PasswordResetSuccessPanel;
        [SerializeField]

        public void OnCancelButtonPress()
        {
            SceneManager.UnloadSceneAsync("PasswordResetConfirmation");
        }

        public void OnConfirmButtonPress()
        {
            var screenName = PlayerPrefs.GetString("screenNameInputField");
            if (screenName.Contains("@")) // username is actually an email
            {
                SendRecoveryEmail(screenName);
            }
            else
            {
                AccountInfoRequest(screenName);
            }
        }

        public void OnSuccessButtonPress()
        {
            SceneManager.UnloadSceneAsync("PasswordResetConfirmation");
        }

        void AccountInfoRequest(string screenName)
        {
            var request = new GetAccountInfoRequest
            {
                Username = screenName
            };

            PlayFabClientAPI.GetAccountInfo(request, res =>
            {
                SendRecoveryEmail(res.AccountInfo.PrivateInfo.Email);
            }, FailureCallback);
        }

        void SendRecoveryEmail(string email)
        {
            Debug.Log("attempting password recovery at: " + email);
            PasswordResetSuccessPanel.SetActive(true);
            var request = new SendAccountRecoveryEmailRequest
            {
                Email = email,
                EmailTemplateId = "FCCD62394DB8AEAC",
                TitleId = PlayFabSettings.TitleId
            };

            PlayFabClientAPI.SendAccountRecoveryEmail(request, res =>
            {
                Debug.Log("An account recovery email has been sent to the player's email address.");
            }, FailureCallback);
        }

        void FailureCallback(PlayFabError error)
        {
            Debug.LogWarning("Something went wrong with your API call. Here's some debug information:");
            Debug.LogError(error.GenerateErrorReport());
        }
    }
}

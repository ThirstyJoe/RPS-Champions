
namespace ThirstyJoe.RPSChampions
{
    using UnityEngine.SceneManagement;
    using UnityEngine;
    using PlayFab;
    using PlayFab.ClientModels;
    using PlayFab.Json;
    using System;

    public class PasswordResetConfirmation : MonoBehaviour
    {
        [SerializeField]
        private GameObject PasswordResetSuccessPanel;
        [SerializeField]
        private GameObject PasswordResetFailurePanel;

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
                SendRecoveryEmailFromScreenName(screenName);
            }
        }

        public void GetPlayFabIdFromScreenNameToSendRecoveryEmail(string screenName)
        {
            var request = new GetAccountInfoRequest
            {
                Username = screenName
            };

            PlayFabClientAPI.GetAccountInfo(request, res =>
            {
                string playFabId = res.AccountInfo.PlayFabId;
                Debug.Log("play fab id retireved: " + playFabId);
                SendRecoveryEmailFromScreenName(playFabId);
            }, FailureCallback);
        }

        public void SendRecoveryEmailFromScreenName(string username)
        {
            Debug.Log("attempting to send email for: " + username);
            PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
            {
                FunctionName = "UsernameResetPassword",
                FunctionParameter = new
                {
                    Username = username
                },
                GeneratePlayStreamEvent = true,
            }, SendRecoveryEmailResult, FailureCallback);
        }

        public void SendRecoveryEmailResult(ExecuteCloudScriptResult result)
        {
            Debug.Log("SendRecoveryEmailResult result received");
            JsonObject jsonResult = (JsonObject)result.FunctionResult;
            Debug.Log(jsonResult);
            object messageOut;
            jsonResult.TryGetValue("message", out messageOut);
            Debug.Log(messageOut);
            string message = (string)messageOut;

            if (message == "") // no error
            {
                PasswordResetSuccessPanel.SetActive(true);
                Debug.Log("An account recovery email has been sent to the player's email address.");
            }
            else
            {
                Debug.Log("Username not found, recovery email not sent.");
                PasswordResetFailurePanel.SetActive(true);
            }

        }

        public void OnSuccessButtonPress()
        {
            SceneManager.UnloadSceneAsync("PasswordResetConfirmation");
        }

        public void OnFailureButtonPress()
        {
            SceneManager.UnloadSceneAsync("PasswordResetConfirmation");
        }

        void SendRecoveryEmail(string email)
        {
            Debug.Log("attempting password recovery at: " + email);
            var request = new SendAccountRecoveryEmailRequest
            {
                Email = email,
                EmailTemplateId = "FCCD62394DB8AEAC",
                TitleId = PlayFabSettings.TitleId
            };

            PlayFabClientAPI.SendAccountRecoveryEmail(request, res =>
            {
                PasswordResetSuccessPanel.SetActive(true);
                Debug.Log("An account recovery email has been sent to the player's email address.");
            }, FailureCallback);
        }

        void FailureCallback(PlayFabError error)
        {
            PasswordResetFailurePanel.SetActive(true);
            Debug.LogWarning("Something went wrong with your API call. Here's some debug information:");
            Debug.LogError(error.GenerateErrorReport());
        }
    }
}

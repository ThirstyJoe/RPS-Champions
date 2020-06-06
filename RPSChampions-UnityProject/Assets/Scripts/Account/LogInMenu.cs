
namespace ThirstyJoe.RPSChampions
{
    using UnityEngine.SceneManagement;
    using UnityEngine;
    using TMPro;
    using PlayFab;
    using PlayFab.ClientModels;

    public class LogInMenu : MonoBehaviour
    {
        // Error message obj refs
        [SerializeField]
        private GameObject errorPanel;
        [SerializeField]
        private TextMeshProUGUI errorTitleText;
        [SerializeField]
        private TextMeshProUGUI errorMessageText;

        // Input field obj refs
        [SerializeField]
        private TMP_InputField screenNameInputField;
        [SerializeField]
        private TMP_InputField passwordInputField;



        public void OnConfirmErrorButtonPress()
        {
            errorPanel.SetActive(false);
        }

        public void OnCreateAccountButtonPress()
        {
            SceneManager.LoadScene("CreateAccount");
        }

        public void OnMainMenuButtonPress()
        {
            SceneManager.LoadScene("MainMenu");
        }

        public void OnLogInButtonPress()
        {
            var username = screenNameInputField.text;
            var password = passwordInputField.text;

            // save login in prefs
            PlayerPrefs.SetString("password", password);

            // lets server know we want account info returned so we can get screenName/email
            var requestParams = new GetPlayerCombinedInfoRequestParams()
            {
                GetUserAccountInfo = true
            };

            if (username.Contains("@")) // username is actually an email
            {
                var request = new LoginWithEmailAddressRequest
                {
                    InfoRequestParameters = requestParams,
                    Email = username,
                    Password = password,
                    TitleId = PlayFabSettings.TitleId
                };

                PlayFabClientAPI.LoginWithEmailAddress(
                    request,
                    SuccessfulLogin,
                    FailureCallback
                );
            }
            else
            {
                // save login in prefs
                PlayerPrefs.SetString("screenName", username);

                var request = new LoginWithPlayFabRequest
                {
                    InfoRequestParameters = requestParams,
                    Username = username,
                    Password = password,
                    TitleId = PlayFabSettings.TitleId
                };


                PlayFabClientAPI.LoginWithPlayFab(
                    request,
                    SuccessfulLogin,
                    FailureCallback
                );
            }
        }

        public void SuccessfulLogin(LoginResult result)
        {
            // save login in prefs
            PlayerPrefs.SetString("screenName", result.InfoResultPayload.AccountInfo.Username);
            PlayerPrefs.SetString("email", result.InfoResultPayload.AccountInfo.PrivateInfo.Email);


            if (LeagueManager.redirectLoginToLeague)
                SceneManager.LoadScene("LeagueDashboard");
            else
                SceneManager.LoadScene("MainMenu");
            PlayFabAuthenticator.Authenticated(result);
        }

        public void OnForgetPassButtonPress()
        {
            PlayerPrefs.SetString("screenNameInputField", screenNameInputField.text);
            SceneManager.LoadScene("PasswordResetConfirmation", LoadSceneMode.Additive);
        }

        private void FailureCallback(PlayFabError error)
        {
            PlayerPrefs.DeleteAll(); // password save since login failed

            Debug.LogError(error.GenerateErrorReport());

            // Recognize and handle the error
            switch (error.Error)
            {
                case PlayFabErrorCode.EmailAddressNotAvailable:
                    CreateErrorPopUp(
                    "Email in use",
                    "This email is already registered to an account."
                    );
                    break;
                case PlayFabErrorCode.InvalidEmailAddress:
                    CreateErrorPopUp(
                     "Invalid email",
                     "Please provide a valid email address."
                    );
                    break;
                case PlayFabErrorCode.InvalidPassword:
                    CreateErrorPopUp(
                     "Invalid password",
                     "Password must be at least 6 characters long."
                    );
                    break;
                case PlayFabErrorCode.InvalidUsername:
                    CreateErrorPopUp(
                     "Invalid screen name",
                     "That screen name is invalid. Try using numbers and letters only."
                    );
                    break;
                case PlayFabErrorCode.NameNotAvailable:
                    CreateErrorPopUp(
                     "Screen name unavailable",
                     "Screen names must be unique. That one is taken already."
                    );
                    break;
                case PlayFabErrorCode.ProfaneDisplayName:
                    CreateErrorPopUp(
                     "Profane screen name",
                     "That screen name is not allowed."
                    );
                    break;
                case PlayFabErrorCode.UsernameNotAvailable:
                    CreateErrorPopUp(
                     "Screen name unavailable",
                     "Screen names must be unique. That one is taken already."
                    );
                    break;
                case PlayFabErrorCode.InvalidUsernameOrPassword:
                    CreateErrorPopUp(
                     "Invalid Screen Name or Password",
                     "Screen name or Password is invalid."
                    );
                    break;
                case PlayFabErrorCode.InvalidParams:
                    CreateErrorPopUp(
                     "Invalid screen name",
                     "screen name must be between 3-20 characters long"
                    );
                    break;
                case PlayFabErrorCode.AccountAlreadyLinked:
                    CreateErrorPopUp(
                     "Account already registered",
                     "That's odd. Try again!"
                    );
                    break;
                default:
                    // TODO: is there a way to get these forwarded to me in an email?
                    CreateErrorPopUp(
                     "Unexpected Error",
                     "Please try again or contact the developer at joecgo@gmail.com."
                    );
                    break;
            }
        }

        private void CreateErrorPopUp(string errorTitle, string errorMsg)
        {
            errorPanel.SetActive(true);
            errorTitleText.text = errorTitle;
            errorMessageText.text = errorMsg;
        }

    }
}

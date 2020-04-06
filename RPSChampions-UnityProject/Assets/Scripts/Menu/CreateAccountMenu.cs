
namespace ThirstyJoe.RPSChampions
{
    using UnityEngine.SceneManagement;
    using UnityEngine;
    using TMPro;
    using PlayFab;
    using PlayFab.ClientModels;

    public class CreateAccountMenu : MonoBehaviour
    {
        // error msg obj refs
        [SerializeField]
        private GameObject errorPanel;
        [SerializeField]
        private TextMeshProUGUI errorTitleText;
        [SerializeField]
        private TextMeshProUGUI errorMessageText;

        // input field obj refs
        [SerializeField]
        private TMP_InputField screenNameInputField;
        [SerializeField]
        private TMP_InputField emailInputField;
        [SerializeField]
        private TMP_InputField passwordInputField;
        [SerializeField]
        private TMP_InputField confirmPasswordInputField;

        public void OnCancelButtonPress()
        {
            SceneManager.LoadScene("MainMenu");
        }
        public void OnSignUpButtonPress()
        {
            if (confirmPasswordInputField.text == passwordInputField.text)
            {
                RegisterUser();
            }
            else
            {
                CreateErrorPopUp("Password Mismatch", "Please make sure 'Password' and Confirm password' are exactly the same.");
            }
        }

        public void OnConfirmErrorButtonPress()
        {
            errorPanel.SetActive(false);
        }

        private void RegisterUser()
        {
            var username = screenNameInputField.text;
            var password = passwordInputField.text;
            var email = emailInputField.text;

            var request = new RegisterPlayFabUserRequest
            {
                RequireBothUsernameAndEmail = true,
                Username = username,
                Email = email,
                Password = password,
                TitleId = PlayFabSettings.TitleId
            };


            PlayFabClientAPI.RegisterPlayFabUser(request, success =>
                {
                    Debug.Log("Successfully registered " + username);
                    SuccessfullyCreatedAccount(username, password);
                }, FailureCallback);
        }

        private void SuccessfullyCreatedAccount(string username, string password)
        {
            // return to main menu after successful account creation

            var request = new LoginWithPlayFabRequest
            {
                Username = username,
                Password = password,
                TitleId = PlayFabSettings.TitleId
            };

            PlayFabClientAPI.LoginWithPlayFab(
                request,
                PlayFabAuthenticator.Authenticated,
                PlayFabAuthenticator.OnPlayFabError
            );

            SceneManager.LoadScene("MainMenu");
        }

        private void FailureCallback(PlayFabError error)
        {
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

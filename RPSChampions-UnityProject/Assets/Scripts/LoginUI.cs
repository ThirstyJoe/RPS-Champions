
namespace ThirstyJoe.RPSChampions
{
    using UnityEngine;

    public class LoginUI : MonoBehaviour
    {
        #region PRIVATE VARS

        [Tooltip("The Ui Panel to let the user enter name, connect and play")]
        [SerializeField]
        private GameObject loginPanel;

        [Tooltip("The UI Panel to inform the user that the connection is in progress")]
        [SerializeField]
        private GameObject progressPanel;

        private enum State
        {
            WaitForLoginAttempt,
            AttemptingLogin,
            Success
        }
        private State state;

        private static LoginUI singleton;

        #endregion


        #region UNITY
        private void Awake()
        {
            if (singleton == null)
            {
                singleton = this;
            }
            else
            {
                Destroy(this);
            }
        }

        #endregion


        #region PUBLIC ENTER "STATES"

        public static void EnterWaitForLoginAttemptState()
        {
            singleton.loginPanel.SetActive(true);
            singleton.progressPanel.SetActive(false);
            singleton.state = LoginUI.State.WaitForLoginAttempt;
        }

        public static void EnterAttemptingLoginState()
        {
            singleton.loginPanel.SetActive(false);
            singleton.progressPanel.SetActive(true);
            singleton.state = LoginUI.State.AttemptingLogin;
        }

        public static void EnterSuccessState()
        {
            singleton.loginPanel.SetActive(false);
            singleton.progressPanel.SetActive(false);
            singleton.state = LoginUI.State.Success;
        }

        #endregion
    }
}
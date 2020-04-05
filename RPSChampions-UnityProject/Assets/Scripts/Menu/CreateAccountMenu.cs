
namespace ThirtyJoe.RPSChampions
{
    using UnityEngine.SceneManagement;
    using UnityEngine;
    public class CreateAccountMenu : MonoBehaviour
    {
        public void OnCancelButtonPress()
        {
            SceneManager.LoadScene("MainMenu");
        }
        public void OnSignUpButtonPress()
        {
            SceneManager.LoadScene("MainMenu");
        }
    }
}

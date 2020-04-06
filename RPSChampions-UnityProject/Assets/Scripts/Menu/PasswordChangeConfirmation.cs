
namespace ThirstyJoe.RPSChampions
{
    using UnityEngine.SceneManagement;
    using UnityEngine;
    public class PasswordChangeConfirmation : MonoBehaviour
    {
        public void OnSuccessButtonPress()
        {
            SceneManager.UnloadSceneAsync("PasswordChangeConfirmation");
        }

    }
}

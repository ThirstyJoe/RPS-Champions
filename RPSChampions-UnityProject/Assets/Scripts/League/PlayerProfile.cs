namespace ThirstyJoe.RPSChampions
{
    using UnityEngine;
    using TMPro;
    using UnityEngine.SceneManagement;

    public class PlayerProfile : MonoBehaviour
    {
        public void OnBackButtonPress()
        {
            SceneManager.UnloadSceneAsync("PlayerProfile");
        }
    }
}
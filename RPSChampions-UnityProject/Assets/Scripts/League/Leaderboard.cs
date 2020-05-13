namespace ThirstyJoe.RPSChampions
{
    using UnityEngine;
    using TMPro;
    using UnityEngine.SceneManagement;

    public class Leaderboard : MonoBehaviour
    {
        public void OnBackButtonPress()
        {
            SceneManager.UnloadSceneAsync("Leaderboard");
        }
    }
}
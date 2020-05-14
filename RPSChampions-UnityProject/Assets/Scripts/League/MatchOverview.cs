namespace ThirstyJoe.RPSChampions
{
    using UnityEngine;
    using TMPro;
    using UnityEngine.SceneManagement;


    public class MatchOverview : MonoBehaviour
    {
        public void OnBackButtonPress()
        {
            SceneManager.UnloadSceneAsync("MatchOverview");
        }
    }
}
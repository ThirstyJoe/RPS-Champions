namespace ThirstyJoe.RPSChampions
{
    using UnityEngine;
    using TMPro;
    using UnityEngine.SceneManagement;

    public class LeagueView : MonoBehaviour
    {
        public void OnBackButtonPress()
        {
            SceneManager.UnloadSceneAsync("LeagueView");
        }
        public void OnStartSeasonButtonPress()
        {
            // TODO: send message to server that season is starting NOW NOW NOW, remove from OPEN list
        }
    }
}
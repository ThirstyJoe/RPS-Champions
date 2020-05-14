namespace ThirstyJoe.RPSChampions
{
    using UnityEngine;
    using TMPro;
    using UnityEngine.SceneManagement;

    public class LeagueView : MonoBehaviour
    {
        [SerializeField]
        private GameObject StandingsListPanel;
        [SerializeField]
        private GameObject StandingsListContent;
        [SerializeField]
        private GameObject PlayerListPanel;
        [SerializeField]
        private GameObject PlayerListContent;
        [SerializeField]
        private GameObject MatchListPanel;
        [SerializeField]
        private GameObject MatchListContent;
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
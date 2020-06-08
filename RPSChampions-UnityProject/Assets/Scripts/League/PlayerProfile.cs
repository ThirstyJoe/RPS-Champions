namespace ThirstyJoe.RPSChampions
{
    using UnityEngine;
    using TMPro;
    using UnityEngine.SceneManagement;

    public class PlayerProfile : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI statsText;
        [SerializeField] private TextMeshProUGUI leagueStatsText;

        private void Start()
        {
            RequestPlayerInfoFromServer();
        }

        private void RequestPlayerInfoFromServer()
        {

        }

        public void OnBackButtonPress()
        {
            SceneManager.UnloadSceneAsync("PlayerProfile");
        }
    }
}
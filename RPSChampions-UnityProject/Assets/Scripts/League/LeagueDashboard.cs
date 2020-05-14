namespace ThirstyJoe.RPSChampions
{
    using UnityEngine;
    using TMPro;
    using System.Collections.Generic;
    using UnityEngine.SceneManagement;
    using UnityEngine.UIElements;
    using UnityEngine.UI;

    public class LeagueDashboard : MonoBehaviour
    {
        #region UNITY OBJ REFS 

        [SerializeField]
        private GameObject StartToggle;
        [SerializeField]
        private GameObject NoLeaguesPanel;
        [SerializeField]
        private GameObject NoLeaguesOpenPanel;
        [SerializeField]
        private GameObject NoLeagueHistoryPanel;
        [SerializeField]
        private GameObject LeagueListPanel;
        [SerializeField]
        private GameObject LeagueListContent;
        [SerializeField]
        private GameObject PlayerButtonPrefab;
        [SerializeField]
        private float PlayerButtonSpacing = 55.0F;


        #endregion

        #region UNITY 
        private void Start()
        {
            ShowCurrentLeagues();
            StartToggle.SendMessage("Select");
        }


        #endregion
        #region UI EVENTS 

        public void OnLeaguesToggleOn()
        {
            ShowCurrentLeagues();
        }
        public void OnJoinToggleOn()
        {
            ShowOpenLeagues();
        }
        public void OnHistoryToggleOn()
        {
            ShowHistory();
        }
        public void OnMainMenuButtonPress()
        {
            SceneManager.LoadScene("MainMenu");
        }
        public void OnLeaderboardButtonPress()
        {
            SceneManager.LoadScene("Leaderboard", LoadSceneMode.Additive);
        }
        public void OnCreateRatedButtonPress()
        {
            LeagueManager.NewRatedLeague();
            SceneManager.LoadScene("NewLeague", LoadSceneMode.Additive);
        }
        public void OnCreateCustomButtonPress()
        {
            LeagueManager.NewCustomLeague();
            SceneManager.LoadScene("NewLeague", LoadSceneMode.Additive);
        }

        #endregion

        #region UI 

        private void ShowCurrentLeagues()
        {
            TitleDescriptionPair[] leagueList = LeagueManager.GetCurrentLeagues();
            if (leagueList.Length == 0)
                HideAllPanelsExcept(NoLeaguesPanel);
            else
                UpdateLeagueList(leagueList);
        }
        private void ShowOpenLeagues()
        {
            TitleDescriptionPair[] leagueList = LeagueManager.GetOpenLeagues();
            if (leagueList.Length == 0)
                HideAllPanelsExcept(NoLeaguesOpenPanel);
            else
                UpdateLeagueList(leagueList);
        }
        private void ShowHistory()
        {
            TitleDescriptionPair[] leagueList = LeagueManager.GetLeagueHistory();
            if (leagueList.Length == 0)
                HideAllPanelsExcept(NoLeagueHistoryPanel);
            else
                UpdateLeagueList(leagueList);
        }
        private void UpdateLeagueList(TitleDescriptionPair[] leagueList)
        {
            HideAllPanelsExcept(LeagueListPanel);


            // clear previous list
            foreach (var item in LeagueListContent.transform.GetComponentsInChildren<TitleDescriptionButton>())
            {
                Destroy(item.gameObject);
            }

            // generate new list
            foreach (TitleDescriptionPair player in leagueList)
            {
                GameObject obj = Instantiate(PlayerButtonPrefab, LeagueListContent.transform);
                var tdButton = obj.GetComponent<TitleDescriptionButton>();
                tdButton.SetText(player);
                tdButton.SetLoadSceneName("LeagueView");
            }
        }

        private void HideAllPanelsExcept(GameObject panel)
        {
            NoLeaguesPanel.SetActive(false);
            NoLeaguesOpenPanel.SetActive(false);
            NoLeagueHistoryPanel.SetActive(false);
            LeagueListPanel.SetActive(false);
            panel.SetActive(true);
        }


        #endregion
    }
}
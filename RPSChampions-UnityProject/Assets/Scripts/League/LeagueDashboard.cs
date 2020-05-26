namespace ThirstyJoe.RPSChampions
{
    using UnityEngine;
    using TMPro;
    using System.Collections.Generic;
    using UnityEngine.SceneManagement;
    using UnityEngine.UI;

    public class LeagueDashboard : MonoBehaviour
    {
        #region UNITY OBJ REFS 

        [SerializeField] private GameObject StartToggle;
        [SerializeField] private GameObject NoLeaguesPanel;
        [SerializeField] private GameObject NoLeaguesOpenPanel;
        [SerializeField] private GameObject NoLeagueHistoryPanel;
        [SerializeField] private GameObject LeagueToggle;
        [SerializeField] private GameObject JoinToggle;
        [SerializeField] private GameObject HistoryToggle;
        [SerializeField] private GameObject LeagueListPanel;
        [SerializeField] private GameObject LeagueListContent;
        [SerializeField] private GameObject PlayerButtonPrefab;


        #endregion

        #region UNITY 
        private void Start()
        {
            LeagueManager.GetCurrentLeagues(new LeagueManager.GetLeaguesCallBack(ShowCurrentLeagues));
            StartToggle.SendMessage("Select");
        }


        #endregion
        #region UI EVENTS 

        public void OnLeaguesToggleOn()
        {
            if (LeagueToggle.GetComponent<Toggle>().isOn)
                LeagueManager.GetCurrentLeagues(new LeagueManager.GetLeaguesCallBack(ShowCurrentLeagues));
        }
        public void OnJoinToggleOn()
        {
            if (JoinToggle.GetComponent<Toggle>().isOn)
                LeagueManager.GetOpenLeagues(new LeagueManager.GetLeaguesCallBack(ShowOpenLeagues));
        }
        public void OnHistoryToggleOn()
        {
            if (HistoryToggle.GetComponent<Toggle>().isOn)
                LeagueManager.GetLeagueHistory(new LeagueManager.GetLeaguesCallBack(ShowLeagueHistory));
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

        private void ShowCurrentLeagues(List<TitleDescriptionButtonData> leagueList)
        {
            if (leagueList.Count == 0)
                HideAllPanelsExcept(NoLeaguesPanel);
            else
                UpdateLeagueList(leagueList, "Current");
        }
        private void ShowOpenLeagues(List<TitleDescriptionButtonData> leagueList)
        {
            if (leagueList.Count == 0)
                HideAllPanelsExcept(NoLeaguesOpenPanel);
            else
                UpdateLeagueList(leagueList, "Open");
        }
        private void ShowLeagueHistory(List<TitleDescriptionButtonData> leagueList)
        {
            if (leagueList.Count == 0)
                HideAllPanelsExcept(NoLeagueHistoryPanel);
            else
                UpdateLeagueList(leagueList, "History");
        }
        private void UpdateLeagueList(List<TitleDescriptionButtonData> leagueList, string label)
        {
            HideAllPanelsExcept(LeagueListPanel);

            // clear previous list
            foreach (var item in LeagueListContent.transform.GetComponentsInChildren<TitleDescriptionButton>())
            {
                Destroy(item.gameObject);
            }

            // generate new list
            foreach (TitleDescriptionButtonData league in leagueList)
            {
                GameObject obj = Instantiate(PlayerButtonPrefab, LeagueListContent.transform);
                var tdButton = obj.GetComponent<TitleDescriptionButton>();
                tdButton.SetupButton(league, "LeagueView", label);
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
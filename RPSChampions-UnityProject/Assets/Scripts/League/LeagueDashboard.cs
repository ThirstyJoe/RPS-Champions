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

        [SerializeField]
        private GameObject StartToggle;
        [SerializeField]
        private GameObject NoLeaguesPanel;
        [SerializeField]
        private GameObject NoLeaguesOpenPanel;
        [SerializeField]
        private GameObject NoLeagueHistoryPanel;
        [SerializeField]
        private GameObject LeagueToggle;
        [SerializeField]
        private GameObject JoinToggle;
        [SerializeField]
        private GameObject HistoryToggle;
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

        private void ShowCurrentLeagues(List<TitleDescriptionPair> leagueList)
        {
            if (leagueList.Count == 0)
                HideAllPanelsExcept(NoLeaguesPanel);
            else
                UpdateLeagueList(leagueList);
        }
        private void ShowOpenLeagues(List<TitleDescriptionPair> leagueList)
        {
            if (leagueList.Count == 0)
                HideAllPanelsExcept(NoLeaguesOpenPanel);
            else
                UpdateLeagueList(leagueList);
        }
        private void ShowLeagueHistory(List<TitleDescriptionPair> leagueList)
        {
            if (leagueList.Count == 0)
                HideAllPanelsExcept(NoLeagueHistoryPanel);
            else
                UpdateLeagueList(leagueList);
        }
        private void UpdateLeagueList(List<TitleDescriptionPair> leagueList)
        {
            HideAllPanelsExcept(LeagueListPanel);


            // clear previous list
            foreach (var item in LeagueListContent.transform.GetComponentsInChildren<TitleDescriptionButton>())
            {
                Destroy(item.gameObject);
            }

            // generate new list
            foreach (TitleDescriptionPair league in leagueList)
            {
                GameObject obj = Instantiate(PlayerButtonPrefab, LeagueListContent.transform);
                var tdButton = obj.GetComponent<TitleDescriptionButton>();
                tdButton.SetText(league);
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
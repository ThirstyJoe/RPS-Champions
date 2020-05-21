namespace ThirstyJoe.RPSChampions
{
    using UnityEngine;
    using TMPro;
    using UnityEngine.SceneManagement;
    using UnityEngine.EventSystems;
    using PlayFab;
    using PlayFab.ClientModels;
    using System.Collections.Generic;
    using System.Linq;
    using System;
    using System.Globalization;

    public class LeagueView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI TitleText;
        [SerializeField] private GameObject StandingsListPanel;
        [SerializeField] private GameObject StandingsListContent;
        [SerializeField] private GameObject PlayerListPanel;
        [SerializeField] private GameObject PlayerListContent;
        [SerializeField] private GameObject MatchListPanel;
        [SerializeField] private GameObject MatchListContent;
        [SerializeField] private GameObject PlayerButtonPrefab;


        // tracking previous selection, for when returning from this menu
        private GameObject prevUISelection;

        // All the data for the league
        private League league;

        private void Start()
        {
            GetLeagueDataFromServer();
        }

        private void GetLeagueDataFromServer()
        {
            // id saved by this butto
            string leagueKey = TitleDescriptionButtonLinkID.LastSavedLinkID;
            // keys that must exist for valid league
            List<string> validLeagueKeys = new List<string>()
            {
                "Status",
                "Name",
                "HostName",
                "Settings",
            };
            PlayFabClientAPI.GetSharedGroupData(new GetSharedGroupDataRequest()
            {
                SharedGroupId = leagueKey,
            },
            result =>
            {
                // validate
                bool failedKey = false;
                foreach (string key in validLeagueKeys)
                {
                    if (!result.Data.ContainsKey(key))
                    {
                        Debug.Log("ERROR. Missing key from league: " + key);
                        failedKey = true;
                    }
                }

                if (!failedKey)
                {
                    // generate player list
                    // "Player_" Prefix, in a key is brief player data
                    // "PlayerSchedule_" Prefix is the complete list of matches for a player
                    List<LeaguePlayer> playerList = new List<LeaguePlayer>();
                    foreach (string key in result.Data.Keys)
                    {
                        if (key.StartsWith("Player_"))
                        {
                            string playerDataJSON = result.Data[key].Value;
                            LeaguePlayer playerData = LeaguePlayer.CreateFromJSON(playerDataJSON);
                            playerList.Add(playerData);
                        }
                    }

                    // create instance of league
                    league = new League(
                        result.Data["Status"].Value,
                        result.Data["Name"].Value,
                        result.Data["HostName"].Value,
                        LeagueSettings.CreateFromJSON(result.Data["Settings"].Value),
                        leagueKey,
                        playerList
                    );

                    // get our schedule
                    string scheduleKey = "PlayerSchedule_" + PlayerPrefs.GetString("playFabId");
                    if (result.Data.ContainsKey(scheduleKey))
                    {
                        string scheduleJSON = result.Data[scheduleKey].Value;
                        league.Schedule = JsonHelper.getJsonArray<MatchBrief>(scheduleJSON);
                        Debug.Log(league.Schedule);
                    }

                    // determine type of UI we need to set up, OPEN league or CLOSED
                    // Open means it is still recruiting, otherwise it has started or completed
                    if (league.Status == "Open")
                        LeagueViewOpenUI();
                    else
                        LeagueViewClosedUI();
                }
            },
            RPSCommon.OnPlayFabError
            );
        }

        private void LeagueViewClosedUI()
        {
            TitleText.text = league.Name;
            MatchListPanel.SetActive(true);
            PlayerListPanel.SetActive(false);
            StandingsListPanel.SetActive(true);

            // generate player list
            foreach (LeaguePlayer player in league.PlayerList)
            {
                GameObject obj = Instantiate(PlayerButtonPrefab, StandingsListContent.transform);
                var tdButton = obj.GetComponent<TitleDescriptionButton>();

                var buttonData = new TitleDescriptionButtonData(
                    player.PlayerName, // TODO: PlayFabId might be better to use here for LinkID
                    player.PlayerName,
                    player.Wins.ToString() + " - " + player.Losses.ToString() + " - " + player.Draws.ToString()
                );
                tdButton.SetupButton(buttonData, "PlayerProfile");
            }

            UpdateMatchList();
        }

        private void LeagueViewOpenUI()
        {
            TitleText.text = league.Name;
            MatchListPanel.SetActive(false);
            PlayerListPanel.SetActive(true);
            StandingsListPanel.SetActive(false);

            // generate player list
            foreach (LeaguePlayer player in league.PlayerList)
            {
                GameObject obj = Instantiate(PlayerButtonPrefab, PlayerListContent.transform);
                var tdButton = obj.GetComponent<TitleDescriptionButton>();

                var buttonData = new TitleDescriptionButtonData(
                    player.PlayerName, // TODO: PlayFabId might be better to use here for LinkID
                    player.PlayerName,
                    "Rating: " + player.Rating.ToString()
                );
                tdButton.SetupButton(buttonData, "PlayerProfile");
            }
        }

        public void OnBackButtonPress()
        {
            EventSystem.current.SetSelectedGameObject(prevUISelection);
            SceneManager.UnloadSceneAsync("LeagueView");
        }
        public void OnStartSeasonButtonPress()
        {
            league.StartSeason(UpdateMatchList);
            LeagueViewClosedUI();
        }

        public void UpdateMatchList()
        {
            CultureInfo culture = new CultureInfo("en-US");
            // generate match list
            if (league.Schedule != null)
            {
                foreach (MatchBrief match in league.Schedule)
                {
                    GameObject obj = Instantiate(PlayerButtonPrefab, MatchListContent.transform);
                    var tdButton = obj.GetComponent<TitleDescriptionButton>();
                    Debug.Log(match.Opponent);
                    string formattedDate =
                        RPSCommon.UnixTimeToDateTime(match.DateTime).ToString("m", culture) +
                        " " +
                        RPSCommon.UnixTimeToDateTime(match.DateTime).ToString("t", culture);
                    var buttonData = new TitleDescriptionButtonData(
                        match.GetLinkID(league),
                        match.Opponent,
                        formattedDate
                    );
                    tdButton.SetupButton(buttonData, "MatchOverview");
                }
            }
        }

    }
}
namespace ThirstyJoe.RPSChampions
{
    using UnityEngine;
    using TMPro;
    using UnityEngine.SceneManagement;
    using PlayFab.ClientModels;
    using PlayFab;
    using PlayFab.Json;


    public class PlayerProfile : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI statsText;

        private void Start()
        {
            RequestPlayerInfoFromServer();
            statsText.text = "";
            titleText.text = "";
        }

        private void RequestPlayerInfoFromServer()
        {
            Debug.Log(TitleDescriptionButtonLinkData.LinkID);
            PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
            {
                FunctionName = "GetPlayerStats",
                FunctionParameter = new
                {
                    playerId = TitleDescriptionButtonLinkData.LinkID
                },
                GeneratePlayStreamEvent = true,
            },
            result =>
            {
                // get Json object representing the Game State out of FunctionResult
                JsonObject jsonResult = (JsonObject)result.FunctionResult;

                // check if data exists
                if (jsonResult == null)
                {
                    Debug.Log("server failed to return data");
                }
                else
                {
                    string statsJSON = RPSCommon.InterpretCloudScriptData(jsonResult, "Stats");
                    PlayerStatsFromServer playerStatsFromServer = PlayerStatsFromServer.CreateFromJSON(statsJSON);
                    PlayerStats playerStats = new PlayerStats(playerStatsFromServer, TitleDescriptionButtonLinkData.Label);
                    UpdatePlayerUI(playerStats);
                }
            },
            error => Debug.LogError(error.GenerateErrorReport())
            );
        }

        public void UpdatePlayerUI(PlayerStats playerStats)
        {
            titleText.text = playerStats.PlayerName;
            if (LeagueManager.league == null || LeagueManager.league.Status == "Open")
            {
                statsText.text = playerStats.GetReadout();
            }
            else
            {
                LeaguePlayerStats leagueStats = new LeaguePlayerStats(null, null);
                string currentPlayerId = TitleDescriptionButtonLinkData.LinkID;
                foreach (var player in LeagueManager.league.PlayerList)
                {
                    if (player.PlayerId == currentPlayerId)
                    {
                        leagueStats = player;
                        break;
                    }
                }

                string leagueStatsText = "League Record\n" + LeagueManager.league.Name + "\n" +
                            "Wins\t\t" + leagueStats.Wins.ToString() + "\n" +
                            "Losses\t\t" + leagueStats.Losses.ToString() + "\n" +
                            "Draws\t\t" + leagueStats.Draws.ToString() + "\n" +
                            "Points\t\t" + leagueStats.WLDScore.ToString();
                statsText.text = playerStats.GetReadout() + "\n\n" + leagueStatsText;
            }

        }

        public void OnBackButtonPress()
        {
            SceneManager.UnloadSceneAsync("PlayerProfile");
        }
    }
}
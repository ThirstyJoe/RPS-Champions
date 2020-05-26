namespace ThirstyJoe.RPSChampions
{
    using UnityEngine;
    using TMPro;
    using UnityEngine.SceneManagement;
    using PlayFab;
    using PlayFab.ClientModels;
    using PlayFab.Json;
    using System.Globalization;

    public class MatchOverview : MonoBehaviour
    {

        [SerializeField] private TextMeshProUGUI TitleText;
        [SerializeField] private TextMeshProUGUI DateText;
        [SerializeField] private TextMeshProUGUI OpponentStatsText;

        private void Start()
        {
            GetMatchFromServer();
        }

        public void OnBackButtonPress()
        {
            SceneManager.UnloadSceneAsync("MatchOverview");
        }

        private void GetMatchFromServer()
        {
            PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
            {
                FunctionName = "GetMatch",
                FunctionParameter = new
                {
                    leagueId = TitleDescriptionButtonLinkData.LinkID,
                    matchIndex = TitleDescriptionButtonLinkData.DataIndex
                },
                GeneratePlayStreamEvent = true,
            },
           result =>
           {
               // get Json object representing the host's schedule out of FunctionResult
               JsonObject jsonResult = (JsonObject)result.FunctionResult;

               // check if data exists
               if (jsonResult == null)
               {
                   Debug.Log("get match failed... missing data");
                   return;
               }

               // data successfully received 
               // interpret data
               string matchJSON = RPSCommon.InterpretCloudScriptData(jsonResult, "match");
               string statsJSON = RPSCommon.InterpretCloudScriptData(jsonResult, "opponentStats");
               var matchData = ScheduledMatch.CreateFromJSON(matchJSON);
               var statsData = LeaguePlayerStats.CreateFromJSON(matchJSON);
               UpdateMatchUI(matchData, statsData);
           },
           RPSCommon.OnPlayFabError
           );
        }

        public void UpdateMatchUI(ScheduledMatch match, LeaguePlayerStats stats)
        {
            TitleText.text = PlayerManager.PlayerName + " VS " + match.OpponentName;
            CultureInfo culture = new CultureInfo("en-US");
            DateText.text =
                RPSCommon.UnixTimeToDateTime(match.DateTime).ToString("m", culture) +
                " " +
                RPSCommon.UnixTimeToDateTime(match.DateTime).ToString("t", culture);

            OpponentStatsText.text =
                match.OpponentName + " League Stats" + "\n" +
                "Wins\t  " + stats.Wins.ToString() + "\n" +
                "Losses\t  " + stats.Losses.ToString() + "\n" +
                "Draws\t  " + stats.Draws.ToString();
        }
    }
}
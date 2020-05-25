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
               UpdateMatchUI(new MatchBrief(matchJSON));
           },
           RPSCommon.OnPlayFabError
           );
        }

        public void UpdateMatchUI(MatchBrief match)
        {
            TitleText.text = "Match VS " + match.Opponent;
            CultureInfo culture = new CultureInfo("en-US");
            DateText.text =
                        RPSCommon.UnixTimeToDateTime(match.DateTime).ToString("m", culture) +
                        " " +
                        RPSCommon.UnixTimeToDateTime(match.DateTime).ToString("t", culture);
            OpponentStatsText.text = "No stats available, coming soon!";
        }
    }
}
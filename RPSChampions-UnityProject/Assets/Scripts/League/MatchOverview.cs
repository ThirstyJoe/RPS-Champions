namespace ThirstyJoe.RPSChampions
{
    #region IMPORTS 

    using UnityEngine;
    using TMPro;
    using UnityEngine.SceneManagement;
    using PlayFab;
    using PlayFab.ClientModels;
    using PlayFab.Json;
    using System.Globalization;
    using System;
    using UnityEngine.UI;
    using UnityEngine.EventSystems;

    #endregion
    public class MatchOverview : MonoBehaviour
    {
        #region UNITY OBJ REFS
        [SerializeField] private TextMeshProUGUI TitleText;
        [SerializeField] private TextMeshProUGUI DateText;
        [SerializeField] private TextMeshProUGUI OpponentStatsText;
        [SerializeField] private TextMeshProUGUI gameStatusText; // "select Rock Paper or Scissors", "Waiting for opponent...",
        [SerializeField] private GameObject winPanel;
        [SerializeField] private GameObject losePanel;
        [SerializeField] private GameObject drawPanel;
        [SerializeField] private GameObject chooseWeaponPanel;
        [SerializeField] private GameObject showWeaponPanel;
        [SerializeField] private GameObject[] opponentWeaponChoice; // Reveal: rock, paper, scissors, no move
        [SerializeField] private GameObject[] myWeaponChoice; // Reveal: rock, paper, scissors, no move
        [SerializeField] private GameObject[] weaponToggles;
        #endregion

        #region PRIVATE VARS 
        // tracking previous selection, for when returning from this menu
        private GameObject prevUISelection;
        private ScheduledMatch Match;
        private LeaguePlayerStats OpponentStats;


        #endregion

        #region UNITY 

        private void Start()
        {
            prevUISelection = EventSystem.current.currentSelectedGameObject;
            GetMatchFromServer();
        }


        #endregion

        #region PLAYFAB

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
               Match = ScheduledMatch.CreateFromJSON(matchJSON);
               OpponentStats = LeaguePlayerStats.CreateFromJSON(statsJSON);
               UpdateMatchUI();
           },
           RPSCommon.OnPlayFabError
           );
        }

        private void SubmitLeagueMatchTurn(Weapon weapon)
        {
            PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
            {
                FunctionName = "SubmitLeagueMatchTurn",
                FunctionParameter = new
                {
                    matchId = Match.MatchID,
                    weapon = weapon.ToString(),
                },
                GeneratePlayStreamEvent = true,
            },
           result =>
           {
               Debug.Log("submitted match turn");
           },
           RPSCommon.OnPlayFabError
           );
        }

        #endregion

        #region UI 
        private void UpdateMatchUI()
        {
            TitleText.text = PlayerManager.PlayerName + " VS " + Match.OpponentName;
            CultureInfo culture = new CultureInfo("en-US");
            DateText.text =
                RPSCommon.UnixTimeToDateTime(Match.DateTime).ToString("m", culture) +
                " " +
                RPSCommon.UnixTimeToDateTime(Match.DateTime).ToString("t", culture);

            OpponentStatsText.text =
                Match.OpponentName + " League Stats" + "\n" +
                "Wins\t  " + OpponentStats.Wins.ToString() + "\n" +
                "Losses\t  " + OpponentStats.Losses.ToString() + "\n" +
                "Draws\t  " + OpponentStats.Draws.ToString();

            SetWeaponToggleUI(RPSCommon.ParseWeapon(Match.MyWeapon));
        }

        private void SetWeaponToggleUI(Weapon weapon)
        {
            int weaponInt = (int)weapon;
            if (weaponInt < weaponToggles.Length)
                weaponToggles[weaponInt].GetComponent<Toggle>().isOn = true;
        }

        #endregion

        #region PLAYER ACTIONS

        public void OnBackButtonPress()
        {
            EventSystem.current.SetSelectedGameObject(prevUISelection);
            SceneManager.UnloadSceneAsync("MatchOverview");
        }
        public void OnSelectRock()
        {
            ChooseWeapon(Weapon.Rock);
        }
        public void OnSelectPaper()
        {
            ChooseWeapon(Weapon.Paper);
        }
        public void OnSelectScissors()
        {
            ChooseWeapon(Weapon.Scissors);
        }

        private void ChooseWeapon(Weapon weapon)
        {
            // check if it is already selected
            if (Match.MyWeapon != weapon.ToString())
            {
                Match.MyWeapon = weapon.ToString();
                SubmitLeagueMatchTurn(weapon);
            }


            // TODO: prevent players from spamming input? 
            // ... Could cause too many server calls
        }

        #endregion
    }
}
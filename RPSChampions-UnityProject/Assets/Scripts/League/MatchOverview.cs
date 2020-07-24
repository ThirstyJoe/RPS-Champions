namespace ThirstyJoe.RPSChampions
{
    #region IMPORTS 

    using UnityEngine;
    using Photon.Pun;
    using TMPro;
    using ExitGames.Client.Photon;
    using Photon.Realtime;
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
        [SerializeField] private TextMeshProUGUI titleTextSelf;
        [SerializeField] private TextMeshProUGUI titleTextOpponent;
        [SerializeField] private TextMeshProUGUI dateText;
        [SerializeField] private TextMeshProUGUI opponentStatsText;
        [SerializeField] private TextMeshProUGUI gameStatusText; // "select Rock Paper or Scissors", "Waiting for opponent...",
        [SerializeField] private GameObject winPanel;
        [SerializeField] private GameObject losePanel;
        [SerializeField] private GameObject drawPanel;
        [SerializeField] private GameObject chooseWeaponPanel;
        [SerializeField] private GameObject showWeaponPanel;
        [SerializeField] private GameObject titlePanel;
        [SerializeField] private GameObject[] opponentWeaponChoice; // Reveal: rock, paper, scissors, no move
        [SerializeField] private GameObject[] myWeaponChoice; // Reveal: rock, paper, scissors, no move
        [SerializeField] private GameObject[] weaponToggles;
        #endregion

        #region PRIVATE VARS 
        // tracking previous selection, for when returning from this menu
        private GameObject prevUISelection;
        private MatchTurn matchTurn;
        private MatchBrief matchBrief;
        private LeaguePlayerStats opponentStats;
        private int opponentRating;

        #endregion

        #region UNITY 

        private void Start()
        {
            titlePanel.SetActive(false);
            showWeaponPanel.SetActive(false);
            chooseWeaponPanel.SetActive(false);

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
               // update player stats
               PlayerManager.UpdatePlayerStats();

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
               string matchJSON = RPSCommon.InterpretCloudScriptData(jsonResult, "matchTurn");
               string statsJSON = RPSCommon.InterpretCloudScriptData(jsonResult, "opponentStats");
               matchBrief = new MatchBrief(RPSCommon.InterpretCloudScriptData(jsonResult, "matchBrief"));
               matchTurn = MatchTurn.CreateFromJSON(matchJSON);
               opponentStats = LeaguePlayerStats.CreateFromJSON(statsJSON);
               opponentRating = Int32.Parse(RPSCommon.InterpretCloudScriptData(jsonResult, "opponentRating"));

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
                    round = matchTurn.Round,
                    matchId = matchTurn.MatchID,
                    weapon = weapon.ToString(),
                    leagueId = matchTurn.LeagueID
                },
                GeneratePlayStreamEvent = true,
            },
           result =>
           {
               Debug.Log("submitted match turn");

               // send event to update in league view
               string leagueKey = TitleDescriptionButtonLinkData.LinkID;
               var data = new object[] { leagueKey, PlayerPrefs.GetString("playFabId") };
               PhotonNetwork.RaiseEvent(
                   LeagueView.LEAGUE_UPDATE_SELF_EVENT,         // .Code
                   data,                                        // .CustomData
                   RaiseEventOptions.Default,
                   SendOptions.SendReliable
               );
           },
           RPSCommon.OnPlayFabError
           );
        }

        #endregion

        #region UI 
        private void UpdateMatchUI()
        {
            // initially set both panel states to false
            showWeaponPanel.SetActive(false);
            chooseWeaponPanel.SetActive(false);

            // set up title panel
            titlePanel.SetActive(true);
            titleTextSelf.text = PlayerManager.PlayerName + " " + PlayerManager.PlayerStats.Rating;
            titleTextOpponent.text = matchTurn.OpponentName + " " + opponentRating.ToString();

            if (matchBrief.Result == WLD.None)
            { // set up choose weapon panel
                chooseWeaponPanel.SetActive(true);
                CultureInfo culture = new CultureInfo("en-US");
                dateText.text =
                    RPSCommon.UnixTimeToDateTime(matchTurn.DateTime).ToString("m", culture) +
                    "\n" +
                    RPSCommon.UnixTimeToDateTime(matchTurn.DateTime).ToString("t", culture);

                opponentStatsText.text =
                    matchTurn.OpponentName + " League Stats" + "\n" +
                    "Points\t  " + opponentStats.WLDScore + "\n" +
                    "Wins\t  " + opponentStats.Wins.ToString() + "\n" +
                    "Losses\t  " + opponentStats.Losses.ToString() + "\n" +
                    "Draws\t  " + opponentStats.Draws.ToString();

                SetWeaponToggleUI(RPSCommon.ParseWeapon(matchTurn.MyWeapon));
            }
            else
            { // set up show results panel
                showWeaponPanel.SetActive(true);
                foreach (GameObject weap in opponentWeaponChoice)
                    weap.SetActive(false);
                opponentWeaponChoice[(int)matchBrief.OpponentWeapon].SetActive(true);
                foreach (GameObject weap in myWeaponChoice)
                    weap.SetActive(false);
                myWeaponChoice[(int)matchBrief.MyWeapon].SetActive(true);

                winPanel.SetActive(false);
                losePanel.SetActive(false);
                drawPanel.SetActive(false);

                if (matchBrief.Result == WLD.Win)
                    winPanel.SetActive(true);
                else if (matchBrief.Result == WLD.Lose)
                    losePanel.SetActive(true);
                else
                    drawPanel.SetActive(true);
            }
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
            FindObjectOfType<LeagueView>().UpdateLeagueView();
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
            if (matchTurn.MyWeapon != weapon.ToString())
            {
                matchTurn.MyWeapon = weapon.ToString();
                SubmitLeagueMatchTurn(weapon);
            }


            // TODO: prevent players from spamming input? 
            // ... Could cause too many server calls
        }

        #endregion
    }
}
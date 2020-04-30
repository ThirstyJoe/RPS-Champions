namespace ThirstyJoe.RPSChampions
{
    using UnityEngine;
    using PlayFab;
    using PlayFab.ClientModels;
    using UnityEngine.SceneManagement;
    using TMPro;
    using System.Collections;
    using PlayFab.Json;
    using System;
    using Photon.Pun;
    using UnityEngine.UIElements;

    #region GAME DATA CLASSES 

    [Serializable]
    public class TurnData
    {
        public string weaponChoice = Weapon.None.ToString();

        public string ToJSON()
        {
            return JsonUtility.ToJson(this);
        }
        public static TurnData CreateFromJSON(string jsonString)
        {
            return JsonUtility.FromJson<TurnData>(jsonString);
        }
    }

    public class PlayerData
    {
        public string hostId;
        public string hostName;
        public string opponentId;
        public string opponentName;

        public string ToJSON()
        {
            return JsonUtility.ToJson(this);
        }
        public static PlayerData CreateFromJSON(string jsonString)
        {
            return JsonUtility.FromJson<PlayerData>(jsonString);
        }
    }


    [Serializable]
    public class GameState
    {
        public string winner;
        public string p1Weapon;
        public string p2Weapon;
        public int turnCount;
        public int turnCompletionTime;

        public string ToJSON()
        {
            return JsonUtility.ToJson(this);
        }

        public static GameState CreateFromJSON(string jsonString)
        {
            return JsonUtility.FromJson<GameState>(jsonString);
        }
    }

    [Serializable]
    public class GameSettings
    {
        public int nextRoundDuration = 5;
        public int turnDuration = 10;
        public int bestOf = 1; // how many matches to choose a victor

        public string ToJSON()
        {
            return JsonUtility.ToJson(this);
        }

        public static GameSettings CreateFromJSON(string jsonString)
        {
            return JsonUtility.FromJson<GameSettings>(jsonString);
        }
    }

    public class WinLoseDrawStats
    {
        public int wins = 0;
        public int losses = 0;
        public int draws = 0;

        public string GetReadout(bool addKey = true)
        {
            const string seperation = " - ";
            string toRet = wins + seperation + losses + seperation + draws;
            if (addKey)
                toRet += "    Wins Losses Draws";
            return toRet;
        }

        public int addWin() { return ++wins; }
        public int addLoss() { return ++losses; }
        public int addDraw() { return ++draws; }
    }

    #endregion



    public class QuickMatch : MonoBehaviour
    {
        #region UNITY OBJ REFS

        [SerializeField]
        private TextMeshProUGUI opponentNameText;

        [SerializeField]
        private TextMeshProUGUI userNameText;
        [SerializeField]
        private TextMeshProUGUI seriesRecordText;
        [SerializeField]
        private TextMeshProUGUI winText;
        [SerializeField]
        private TextMeshProUGUI loseText;
        [SerializeField]
        private TextMeshProUGUI drawText;
        [SerializeField]
        private TextMeshProUGUI countdownText;
        [SerializeField]
        private TextMeshProUGUI gameStatusText; // "select Rock Paper or Scissors", "Waiting for opponent...",
        [SerializeField]
        private TextMeshProUGUI nextRoundCountdownText;
        [SerializeField]
        private GameObject nextRoundPanel;
        [SerializeField]
        private GameObject winPanel;
        [SerializeField]
        private GameObject losePanel;
        [SerializeField]
        private GameObject drawPanel;
        [SerializeField]
        private GameObject chooseWeaponPanel;
        [SerializeField]
        private GameObject showWeaponPanel;
        [SerializeField]
        private GameObject[] opponentWeaponChoice; // Reveal: rock, paper, scissors
        [SerializeField]
        private GameObject[] myWeaponChoice; // Reveal: rock, paper, scissors
        [SerializeField]
        private GameObject[] weaponToggles;


        #endregion

        #region PRIVATE VARS
        private WinLoseDrawStats wldStats = new WinLoseDrawStats();
        private bool waitingForGameStateUpdate = false;
        private GameState localGameState;
        private GameSettings gameSettings;
        private TurnData localTurnData = new TurnData();
        private string groupId;
        private bool isHost = false;

        #endregion

        #region UNITY 

        private void Awake()
        {
        }

        private void Start()
        {
            groupId = PlayerManager.QuickMatchId;
            UpdateGameStateFromServer();
            seriesRecordText.text = wldStats.GetReadout();
        }

        #endregion

        #region UI 

        public void OnSelectRock()
        {
            localTurnData.weaponChoice = Weapon.Rock.ToString();
        }
        public void OnSelectPaper()
        {
            localTurnData.weaponChoice = Weapon.Paper.ToString();
        }
        public void OnSelectScissors()
        {
            localTurnData.weaponChoice = Weapon.Scissors.ToString();
        }

        public void OnSelectOptionsMenu()
        {

            // TODO: async load options menu
        }

        private void UpdateTurnTimerUI(int timeLeft)
        {
            countdownText.text = timeLeft.ToString();
        }

        private void UpdateNextRoundTimerUI(int timeLeft)
        {
            nextRoundCountdownText.text = timeLeft.ToString();
        }

        private void UpdatePlayerUI()
        {
            userNameText.text = PlayerManager.PlayerStats.PlayerName;
            opponentNameText.text = PlayerManager.OpponentName;
        }


        #endregion

        #region SHARED GROUP DATA TEST

        private void ProcessPlayerData(PlayerData playerData)
        {
            isHost = (playerData.hostId == PlayerPrefs.GetString("playFabId"));
            if (isHost)
            {
                PlayerManager.OpponentId = playerData.opponentId;
                PlayerManager.OpponentName = playerData.opponentName;
            }
            else // not host
            {
                PlayerManager.OpponentId = playerData.hostId;
                PlayerManager.OpponentName = playerData.hostName;
            }
        }

        private static void OnErrorShared(PlayFabError error)
        {
            Debug.Log(error.GenerateErrorReport());
        }

        private IEnumerator TurnTimer(int completionTime)
        {
            // get current epoch time in seconds
            TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1);
            int time = (int)ts.TotalSeconds;

            int timeLeft = Math.Max(completionTime - time, 0);
            UpdateTurnTimerUI(timeLeft);

            // track turn time countdown locally
            // game server has its own synchronous countdown
            while (timeLeft > 0)
            {
                yield return new WaitForSeconds(1.0F);
                UpdateTurnTimerUI(--timeLeft);
            }

            // wait for game state update from server to come in
            waitingForGameStateUpdate = true;
            SendLocalTurnDataToServer();

            while (waitingForGameStateUpdate)
            {
                // repeatably attempts to get next game state from server
                // this ensures we still get a response even if our own local counter gets ahead somehow
                yield return new WaitForSeconds(1.0F);
                UpdateGameStateFromServer();
            }
        }

        private IEnumerator NextRoundTimer(int nextRoundTimerDuration)
        {
            // get current epoch time in seconds
            TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1);
            int time = (int)ts.TotalSeconds;
            int completionTime = time + nextRoundTimerDuration;

            nextRoundPanel.SetActive(false);

            int timeLeft = Math.Max(completionTime - time, 0);
            UpdateNextRoundTimerUI(timeLeft);

            while (timeLeft > 0)
            {
                yield return new WaitForSeconds(1.0F);
                UpdateNextRoundTimerUI(--timeLeft);
                if (timeLeft <= 3)
                    nextRoundPanel.SetActive(true);
            }

            StartNextRound();
        }

        private void StartNextRound()
        {
            gameStatusText.text = "Select Rock, Paper, or Scissors";
            foreach (var toggle in weaponToggles)
                toggle.SetActive(true);

            nextRoundPanel.SetActive(false);
            showWeaponPanel.SetActive(false);
            chooseWeaponPanel.SetActive(true);

            // start timer
            StartCoroutine(TurnTimer(localGameState.turnCompletionTime));
        }

        private void UpdateGameStateFromServer()
        {
            PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
            {
                FunctionName = "GetGameState",
                FunctionParameter = new
                {
                    sharedGroupId = groupId,
                    opponentId = PlayerManager.OpponentId
                },
                GeneratePlayStreamEvent = true,
            }, OnGetGameState, OnErrorShared);
        }

        private void OnGetGameState(ExecuteCloudScriptResult result)
        {
            // get Json object representing the Game State out of FunctionResult
            JsonObject jsonResult = (JsonObject)result.FunctionResult;

            // check if data exists
            if (jsonResult == null)
            {
                Debug.Log("Game data is missing, disconneting...");
                DisconnectFromGame();
                return;
            }

            // data successfully received 
            // interpret data in appropriate classes:
            TurnData turnData = TurnData.CreateFromJSON(InterpretCloudScriptData(jsonResult, "turnData"));
            GameState gameState = GameState.CreateFromJSON(InterpretCloudScriptData(jsonResult, "gameState"));
            PlayerData playerData = PlayerData.CreateFromJSON(InterpretCloudScriptData(jsonResult, "playerData"));
            gameSettings = GameSettings.CreateFromJSON(InterpretCloudScriptData(jsonResult, "gameSettings"));

            // just started game
            if (localGameState == null)
            {
                // in case player data is missing... such as a game client rejoining a running game
                if (PlayerManager.OpponentName == null)
                    ProcessPlayerData(playerData);

                // update player names
                UpdatePlayerUI();

                // start timer
                StartCoroutine(TurnTimer(gameState.turnCompletionTime));

                // update local turn data
                localTurnData = turnData;

                // update game state
                localGameState = gameState;
            }
            else if (gameState.turnCount > localGameState.turnCount) // turn finished
            {
                // set flag so client game loop can continue into next turn
                waitingForGameStateUpdate = false;

                // update game state
                localGameState = gameState;

                // assign weapon to correct player
                isHost = (playerData.hostId == PlayerPrefs.GetString("playFabId"));
                Weapon myWeapon, opponentWeapon;
                if (isHost)
                {
                    myWeapon = ParseWeapon(gameState.p1Weapon);
                    opponentWeapon = ParseWeapon(gameState.p2Weapon);
                }
                else
                {
                    myWeapon = ParseWeapon(gameState.p2Weapon);
                    opponentWeapon = ParseWeapon(gameState.p1Weapon);
                }

                if (myWeapon == Weapon.None || opponentWeapon == Weapon.None)
                {
                    Debug.Log("user or opponent failed to submit move, disconneting...");

                    // todo: better handling of this case
                    DisconnectFromGame();
                    return;
                }

                // ui call based on result
                if (localGameState.winner == PlayerPrefs.GetString("playFabId"))
                {   // win
                    wldStats.addWin();
                    SetUpGameOverUI(ShowWinUI, myWeapon, opponentWeapon);
                }
                else if (localGameState.winner == PlayerManager.OpponentId)
                {   // lose
                    wldStats.addLoss();
                    SetUpGameOverUI(ShowLoseUI, myWeapon, opponentWeapon);
                }
                else
                {   // draw 
                    wldStats.addDraw();
                    SetUpGameOverUI(ShowDrawUI, myWeapon, opponentWeapon);
                }

                StartCoroutine(NextRoundTimer(gameSettings.nextRoundDuration));
            }
        }

        private Weapon ParseWeapon(string weaponName)
        {
            if (weaponName == null)
                return Weapon.None;

            return (Weapon)Enum.Parse(typeof(Weapon), weaponName);
        }

        private void SetUpGameOverUI(Action resultUI, Weapon myWeapon, Weapon opponentWeapon)
        {
            HideAllGameOverUI();
            resultUI();
            myWeaponChoice[(int)myWeapon].SetActive(true);
            opponentWeaponChoice[(int)opponentWeapon].SetActive(true);
            seriesRecordText.text = wldStats.GetReadout();
        }

        private void HideAllGameOverUI()
        {
            showWeaponPanel.SetActive(true);
            chooseWeaponPanel.SetActive(false);
            losePanel.SetActive(false);
            winPanel.SetActive(false);
            drawPanel.SetActive(false);
            foreach (var icon in opponentWeaponChoice)
                icon.SetActive(false);
            foreach (var icon in myWeaponChoice)
                icon.SetActive(false);
        }

        private void ShowWinUI()
        {
            winPanel.SetActive(true);
        }

        private void ShowLoseUI()
        {
            losePanel.SetActive(true);
        }

        private void ShowDrawUI()
        {
            drawPanel.SetActive(true);
        }


        private string InterpretCloudScriptData(JsonObject jsonResult, string dataName)
        {
            // interpret playerData
            object objValue;
            jsonResult.TryGetValue(dataName, out objValue);
            return (string)objValue;
        }

        public void SendLocalTurnDataToServer()
        {
            DisableWeaponSelect();

            // send turn data to cloud script
            PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
            {
                FunctionName = "UpdateTurnData",
                FunctionParameter = new
                {
                    sharedGroupId = groupId,
                    turnData = localTurnData.ToJSON(),
                },
                GeneratePlayStreamEvent = true,
            }, OnTurnDataRecieved, OnErrorShared);
        }

        private void DisableWeaponSelect()
        {
            gameStatusText.text = "You selected " + localTurnData.weaponChoice;
            var weaponChoice = ParseWeapon(localTurnData.weaponChoice);
            foreach (var toggle in weaponToggles)
                toggle.SetActive(false);

            if (weaponChoice != Weapon.None)
                weaponToggles[(int)weaponChoice].SetActive(true);
        }

        private void OnTurnDataRecieved(ExecuteCloudScriptResult result)
        {
            Debug.Log("server recieved updated turn data from player");
        }


        #endregion


        #region END GAME TEST 


        public void OnEndGameButtonPressed()
        {
            PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
            {
                FunctionName = "CleanUpGameRoom",
                FunctionParameter = new
                {
                    sharedGroupId = groupId,
                },
                GeneratePlayStreamEvent = true,
            }, OnEndGame, OnErrorShared);
        }

        private void OnEndGame(ExecuteCloudScriptResult result)
        {
            Debug.Log("game ended, game data successfully deleted");
            DisconnectFromGame();
        }

        private void DisconnectFromGame()
        {
            PhotonNetwork.Disconnect();
            SceneManager.LoadScene("MainMenu");
        }

        #endregion
    }
}
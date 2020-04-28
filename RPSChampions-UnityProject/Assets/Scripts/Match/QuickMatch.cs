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

    public class PlayerDataEntry
    {
        public string playerId;
        public string playerName;

        public string ToJSON()
        {
            return JsonUtility.ToJson(this);
        }
        public static PlayerDataEntry CreateFromJSON(string jsonString)
        {
            return JsonUtility.FromJson<PlayerDataEntry>(jsonString);
        }
    }

    public class PlayerData
    {
        public PlayerDataEntry[] players;

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
        public bool opponentReady = false;
        public string winner;
        public string p1Weapon;
        public string p2Weapon;
        public int turnCount;

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

    #endregion

    public enum QuickMatchState
    {
        Setup,
        StartIntro,
        Choosing,
        ChoiceMade,
        WaitingForResult,
        ResultWin,
        ResultLose,
        ResultDraw,
        NextTurnIntro,
        DrawReIntro,
        Exiting,
        GameAbandoned,
    }

    public class QuickMatch : MonoBehaviour
    {
        #region UNITY OBJ REFS

        [SerializeField]
        private TextMeshProUGUI opponentNameText;

        [SerializeField]
        private TextMeshProUGUI userNameText;
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


        #endregion

        #region PRIVATE VARS
        private bool waitingForGameStateUpdate = false;
        private GameState localGameState;
        private TurnData localTurnData = new TurnData();
        private int turnCount = 0;
        private string groupId;
        private QuickMatchState matchState = QuickMatchState.Setup;

        #endregion

        #region UNITY 

        private void Awake()
        {
        }

        private void Start()
        {
            groupId = PlayerManager.QuickMatchId;
            UpdateGameStateFromServer();

        }

        #endregion

        #region UI 

        public void OnSelectRock()
        {
            localTurnData.weaponChoice = Weapon.Rock.ToString();
            SendLocalTurnDataToServer();
        }
        public void OnSelectPaper()
        {
            localTurnData.weaponChoice = Weapon.Paper.ToString();
            SendLocalTurnDataToServer();
        }
        public void OnSelectScissors()
        {
            localTurnData.weaponChoice = Weapon.Scissors.ToString();
            SendLocalTurnDataToServer();
        }

        public void OnSelectOptionsMenu()
        {

            // TODO: async load options menu
        }

        private void UpdateTurnTimerUI(int timeLeft)
        {
            Debug.Log(timeLeft);
            countdownText.text = timeLeft.ToString();
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
            string p1Id = playerData.players[0].playerId;
            bool isP1 = (p1Id == PlayerPrefs.GetString("playFabId"));
            if (isP1)
            {
                PlayerManager.OpponentId = playerData.players[0].playerId;
                PlayerManager.OpponentName = playerData.players[0].playerName;
            }
            else
            {
                PlayerManager.OpponentId = playerData.players[1].playerId;
                PlayerManager.OpponentName = playerData.players[1].playerName;
            }
        }

        private static void OnErrorShared(PlayFabError error)
        {
            Debug.Log(error.GenerateErrorReport());
        }

        private IEnumerator TurnTimer(int duration)
        {
            // get current epoch time in seconds
            TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1);
            int time = (int)ts.TotalSeconds;

            int timeLeft = Math.Max(duration - time, 0);
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
            while (waitingForGameStateUpdate)
            {
                // repeatably attempts to get next game state from server
                // this ensures we still get a response even if our own local counter gets ahead somehow
                yield return new WaitForSeconds(0.5F);
                UpdateGameStateFromServer();
            }
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
            GameSettings gameSettings = GameSettings.CreateFromJSON(InterpretCloudScriptData(jsonResult, "gameSettings"));

            // just started game
            if (localGameState == null)
            {
                // in case player data is missing... such as a game client rejoining a running game
                if (PlayerManager.OpponentName == null)
                {
                    PlayerData playerData = PlayerData.CreateFromJSON(InterpretCloudScriptData(jsonResult, "playerData"));
                    ProcessPlayerData(playerData);
                }

                // update player names
                UpdatePlayerUI();

                // start timer
                StartCoroutine(TurnTimer(gameSettings.turnDuration));

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

                showWeaponPanel.SetActive(true);
                chooseWeaponPanel.SetActive(false);
                losePanel.SetActive(false);
                winPanel.SetActive(false);
                drawPanel.SetActive(false);
                foreach (var icon in opponentWeaponChoice)
                    icon.SetActive(false);
                foreach (var icon in myWeaponChoice)
                    icon.SetActive(false);

                if (localGameState.winner == PlayerPrefs.GetString("playFabId"))
                {
                    winPanel.SetActive(true);
                }
                else if (localGameState.winner == PlayerManager.OpponentId)
                {
                    losePanel.SetActive(true);
                }
                else // draw 
                {
                    drawPanel.SetActive(true);
                    // TODO: repeat match after draw result
                }
            }
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
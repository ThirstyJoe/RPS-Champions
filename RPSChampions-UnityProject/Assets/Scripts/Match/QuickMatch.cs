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
        private string _weaponChoice = Weapon.None.ToString();


        public Weapon weaponChoice
        {
            get
            {
                return (Weapon)Enum.ToObject(typeof(Weapon), _weaponChoice);
            }
            set
            {
                _weaponChoice = value.ToString();
            }
        }

        public string ToJSON()
        {
            return JsonUtility.ToJson(this);
        }
        public static TurnData CreateFromJSON(string jsonString)
        {
            return JsonUtility.FromJson<TurnData>(jsonString);
        }
    }

    [Serializable]
    public class ClientGameState
    {
        public bool opponentReady = false;
        public string winner;
        public int turnCount;

        public string ToJSON()
        {
            return JsonUtility.ToJson(this);
        }

        public static ClientGameState CreateFromJSON(string jsonString)
        {
            return JsonUtility.FromJson<ClientGameState>(jsonString);
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
        private GameSettings gameSettings = new GameSettings();
        private ClientGameState localGameState = new ClientGameState();
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
            InitializeGameStartState();
        }

        #endregion

        #region UI 

        public void OnSelectRock()
        {
            localTurnData.weaponChoice = Weapon.Rock;
            SendLocalTurnDataToServer();
        }
        public void OnSelectPaper()
        {
            localTurnData.weaponChoice = Weapon.Paper;
            SendLocalTurnDataToServer();
        }
        public void OnSelectScissors()
        {
            localTurnData.weaponChoice = Weapon.Scissors;
            SendLocalTurnDataToServer();
        }

        public void OnSelectOptionsMenu()
        {
            // TODO: async load options menu
        }

        private void UpdateTurnTimerUI(int timeLeft)
        {
            countdownText.text = timeLeft.ToString();
        }

        #endregion

        #region SHARED GROUP DATA TEST

        private static void OnErrorShared(PlayFabError error)
        {
            Debug.Log(error.GenerateErrorReport());
        }

        private IEnumerator TurnTimer(int timeLeft)
        {
            timeLeft = Math.Max(timeLeft, 0);
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
                Debug.Log("Game has been deleted, disconneting...");
                DisconnectFromGame();
                return;
            }

            // data successfully received 

            // interpret gameState
            object gameStateValue;
            jsonResult.TryGetValue("gameState", out gameStateValue);
            ClientGameState serverGameState = ClientGameState.CreateFromJSON((string)gameStateValue);

            // interpret turnData
            object turnDataValue;
            jsonResult.TryGetValue("turnData", out turnDataValue);
            TurnData turnData = TurnData.CreateFromJSON((string)turnDataValue);

            // update game state if we dont have a localGameState
            // *OR* server turnCount is ahead of localGameState
            if (localGameState == null || serverGameState.turnCount > localGameState.turnCount)
            {
                // set flag so client game loop can continue into next turn
                waitingForGameStateUpdate = false;

                // update game state
                localGameState = serverGameState;

                // get current epoch time in seconds
                TimeSpan t = DateTime.UtcNow - new DateTime(1970, 1, 1);
                int time = (int)t.TotalSeconds;

                // subtract to get 
                StartCoroutine(TurnTimer(gameSettings.turnDuration - time));
            }
        }

        private void InitializeGameStartState()
        {
            GameSettings gameSettings = new GameSettings();
            gameSettings.turnDuration = 10;

            PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
            {
                FunctionName = "InitializeGameStartState",
                FunctionParameter = new
                {
                    sharedGroupId = groupId,
                    gameSettings = gameSettings.ToJSON()
                },
                GeneratePlayStreamEvent = true,
            },
                OnSuccess =>
                {
                    Debug.Log("new game room created, new game states initialized");
                    UpdateGameStateFromServer();
                },
                errorCallback =>
                {
                    Debug.Log(errorCallback.ErrorMessage + "error attempting to initialize game state.");
                    UpdateGameStateFromServer();
                }
            );
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
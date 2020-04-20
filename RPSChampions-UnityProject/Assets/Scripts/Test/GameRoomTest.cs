namespace ThirstyJoe.RPSChampions
{
    using UnityEngine;
    using PlayFab;
    using PlayFab.ClientModels;
    using UnityEngine.SceneManagement;
    using UnityEngine.UI;
    using Photon.Pun;
    using Photon.Realtime;
    using System.Text;
    using System.Collections;
    using PlayFab.Json;
    using System;

    #region GAME DATA CLASSES 

    [Serializable]
    public class TurnData
    {
        public int addToGameState;

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
    public class GameState
    {
        public int sum = 0;
        public int turnCount = -1;
        public int turnCompletionTime = -1;


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
        public int turnDuration = 60 * 60 * 24;

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

    public class GameRoomTest : MonoBehaviourPunCallbacks
    {
        #region UNITY OBJ REFS

        [SerializeField]
        private Text userListText;
        [SerializeField]
        private InputField addToGameStateInputField;
        [SerializeField]
        private Text gameStateSumText;
        [SerializeField]
        private Text turnTimerText;

        #endregion

        #region PRIVATE VARS
        private bool waitingForGameStateUpdate = false;
        private GameState localGameState = new GameState();
        private TurnData localTurnData = new TurnData();

        #endregion

        #region UNITY 

        private void Awake()
        {
            ResetUI();
        }

        private void Start()
        {
            UpdateUserListUI();
            InitializeRoomData(); // expected to fail if room is already create
        }

        #endregion

        #region UI 

        private void ResetUI()
        {
            userListText.text = ""; // clear list
        }

        private void UpdateUserListUI()
        {
            var playerList = new StringBuilder();

            foreach (var player in PhotonNetwork.PlayerList)
            {
                playerList.Append(player.NickName + "\n");
            }

            userListText.text = playerList.ToString();
        }
        private void UpdateGameStateUI(GameState gs)
        {
            gameStateSumText.text = gs.sum.ToString();
        }

        private void UpdateTurnDataUI(TurnData td)
        {
            addToGameStateInputField.text = td.addToGameState.ToString();
        }

        private void UpdateTurnTimerUI(int timeLeft)
        {
            turnTimerText.text = timeLeft.ToString();
        }

        #endregion

        #region PUN CALLBACKS

        public override void OnPlayerEnteredRoom(Player other)
        {
            Debug.LogFormat("OnPlayerEnteredRoom() {0}", other.NickName);
            UpdateUserListUI();
        }


        public override void OnPlayerLeftRoom(Player other)
        {
            Debug.LogFormat("OnPlayerLeftRoom() {0}", other.NickName);
            UpdateUserListUI();
        }

        #endregion

        #region SHARED GROUP DATA TEST

        private static void OnErrorShared(PlayFabError error)
        {
            Debug.Log(error.GenerateErrorReport());
        }

        private void InitializeRoomData()
        {
            string groupId = "RPS";
            var request = new CreateSharedGroupRequest { SharedGroupId = groupId };
            PlayFabClientAPI.CreateSharedGroup(
                request,
                OnSuccess =>
                {
                    Debug.Log("Shared group created named: " + groupId);
                    InitializeGameStartState();
                },
                errorCallback =>
                {
                    Debug.Log(errorCallback.ErrorMessage + "group data already exists");
                    UpdateGameStateFromServer();
                }
            );
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
                    sharedGroupId = "GB",
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
            GameState serverGameState = GameState.CreateFromJSON((string)gameStateValue);

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
                UpdateGameStateUI(localGameState);

                // get current epoch time in seconds
                TimeSpan t = DateTime.UtcNow - new DateTime(1970, 1, 1);
                int time = (int)t.TotalSeconds;

                // subtract to get 
                StartCoroutine(TurnTimer(localGameState.turnCompletionTime - time));
            }

            // update client according to match what turnData the server has
            UpdateTurnDataUI(turnData);
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
                    sharedGroupId = "GB",
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

        public void SetAddToGameState(string addToGameState)
        {
            localTurnData.addToGameState = int.Parse(addToGameState);
            SendTurnDataToServer();
        }

        public void SendTurnDataToServer()
        {
            // send turn data to cloud script
            PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
            {
                FunctionName = "UpdateTurnData",
                FunctionParameter = new
                {
                    sharedGroupId = "GB",
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
                    sharedGroupId = "GB",
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
namespace ThirstyJoe.RPSChampions
{
    #region IMPORTS 

    using UnityEngine;
    using PlayFab;
    using PlayFab.ClientModels;
    using UnityEngine.SceneManagement;
    using TMPro;
    using System.Collections;
    using PlayFab.Json;
    using System;
    using Photon.Pun;
    using Photon.Realtime;
    using UnityEngine.UIElements;
    using System.Collections.Generic;
    using ExitGames.Client.Photon;

    #endregion

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

    [Serializable]
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
        public int turnDuration = 5;
        public int bestOf = 1; // how many wins to choose a victor

        public string ToJSON()
        {
            return JsonUtility.ToJson(this);
        }

        public static GameSettings CreateFromJSON(string jsonString)
        {
            return JsonUtility.FromJson<GameSettings>(jsonString);
        }
    }

    [Serializable]
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

    public class QuickMatch : MonoBehaviourPunCallbacks
    {
        #region EVENT DEFS
        private const byte REQUEST_REMATCH_EVENT = 0;
        private const byte ACCEPT_REMATCH_EVENT = 1;
        private const byte WEAPON_CHOSEN_EVENT = 2;

        #endregion

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
        private GameObject nextRoundPanel;
        [SerializeField]
        private GameObject winPanel;
        [SerializeField]
        private GameObject losePanel;
        [SerializeField]
        private GameObject drawPanel;
        [SerializeField]
        private GameObject rematchButton;
        [SerializeField]
        private GameObject chooseWeaponPanel;
        [SerializeField]
        private GameObject showWeaponPanel;
        [SerializeField]
        private GameObject OpponentQuitPanel;
        [SerializeField]
        private GameObject[] opponentWeaponChoice; // Reveal: rock, paper, scissors
        [SerializeField]
        private GameObject[] myWeaponChoice; // Reveal: rock, paper, scissors
        [SerializeField]
        private GameObject[] weaponToggles;


        #endregion

        #region PRIVATE VARS

        // flags for when a rematch has been requested
        private bool opponentRequestedRematch = false;
        private bool selfRequestedRematch = false;

        // flags for when each player has made their initial weapon choice
        private bool opponentFirstChoiceMade = false;
        private bool selfFirstChoiceMade = false;

        // client tracking some game stats to display
        private WinLoseDrawStats wldStats = new WinLoseDrawStats();
        private bool waitingForGameStateUpdate = false;

        // local game data
        private GameState localGameState;
        private GameSettings gameSettings;
        private TurnData localTurnData = new TurnData();
        private string groupId;

        // flag used to figure out whose data is whose from the server
        private bool isHost = false;

        #endregion

        #region UNITY 

        private void Awake()
        {
            PhotonNetwork.NetworkingClient.EventReceived += ReceiveCustomPUNEvents;
        }

        private void Start()
        {
            groupId = PlayerManager.QuickMatchId;
            UpdateGameStateFromServer();
            seriesRecordText.text = wldStats.GetReadout();
        }

        private void OnDestroy()
        {
            PhotonNetwork.NetworkingClient.EventReceived -= ReceiveCustomPUNEvents;
        }

        #endregion

        #region PUN
        public override void OnPlayerLeftRoom(Player other)
        {
            Debug.LogFormat("OnPlayerLeftRoom() {0}", other.NickName);

            ShowOpponentQuitUI();
        }

        #endregion

        #region UI 

        private void ShowOpponentQuitUI()
        {
            OpponentQuitPanel.SetActive(true);
        }

        private void UpdateTurnTimerUI(int timeLeft)
        {
            countdownText.text = timeLeft.ToString();
        }

        private void UpdatePlayerUI()
        {
            userNameText.text = PlayerManager.PlayerStats.PlayerName;
            opponentNameText.text = "Opponent: " + PlayerManager.OpponentName;
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

        private void DisableWeaponSelectUI()
        {
            gameStatusText.text = "You selected " + localTurnData.weaponChoice;
            var weaponChoice = ParseWeapon(localTurnData.weaponChoice);
            foreach (var toggle in weaponToggles)
                toggle.SetActive(false);

            if (weaponChoice != Weapon.None)
                weaponToggles[(int)weaponChoice].SetActive(true);
        }

        private void SetupStartGameUI()
        {
            // manage UI
            if (localGameState?.turnCount == 0)
                gameStatusText.text = "First round ending...";
            else
                gameStatusText.text = "Select Rock, Paper, or Scissors";
            foreach (var toggle in weaponToggles)
                toggle.SetActive(true);
            rematchButton.SetActive(true);
            nextRoundPanel.SetActive(false);
            showWeaponPanel.SetActive(false);
            chooseWeaponPanel.SetActive(true);
        }

        #endregion

        #region GAME SETUP

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

        #endregion

        #region GAME LOOP

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

        #endregion

        #region PLAYER ACTIONS
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

        private Weapon ParseWeapon(string weaponName)
        {
            if (weaponName == null)
                return Weapon.None;

            return (Weapon)Enum.Parse(typeof(Weapon), weaponName);
        }

        private void ChooseWeapon(Weapon weapon)
        {
            localTurnData.weaponChoice = weapon.ToString();

            // first time a weapon has been choise (first round)
            if (!selfFirstChoiceMade)
            {
                selfFirstChoiceMade = true;
                PhotonNetwork.RaiseEvent(
                       WEAPON_CHOSEN_EVENT,        // .Code
                       null,                       // .CustomData
                       RaiseEventOptions.Default,
                       SendOptions.SendReliable);

                if (opponentFirstChoiceMade)
                {
                    RequestGameStateForNextRound();
                }
                else
                {
                    gameStatusText.text = "Waiting for opponent...";
                }
            }
        }


        public void OnRematchButtonPressed()
        {
            if (opponentRequestedRematch)
            {
                opponentRequestedRematch = false;

                RequestRematchToServer();
            }
            else
            {
                selfRequestedRematch = true;
                rematchButton.SetActive(false);

                // has not yet been challenged, send event
                Debug.Log("rematch requested, event sent");
                PhotonNetwork.RaiseEvent(
                    REQUEST_REMATCH_EVENT,        // .Code
                    null,                         // .CustomData
                    RaiseEventOptions.Default,
                    SendOptions.SendReliable);
            }
        }

        #endregion

        #region INCOMING EVENTS


        private void ReceiveCustomPUNEvents(EventData obj)
        {
            Debug.Log("event recieved " + obj.Code);

            // switch to correct function
            switch (obj.Code)
            {
                case REQUEST_REMATCH_EVENT:
                    IncomingRematchRequest();
                    break;
                case ACCEPT_REMATCH_EVENT:
                    RequestGameStateForNextRound();
                    break;
                case WEAPON_CHOSEN_EVENT:
                    OpponentSelectedFirstWeapon();
                    break;
            }
        }

        private void IncomingRematchRequest()
        {
            opponentRequestedRematch = true;
            if (selfRequestedRematch)
                RequestRematchToServer();
        }


        private void OpponentSelectedFirstWeapon()
        {
            opponentFirstChoiceMade = true;
            if (selfFirstChoiceMade)
                RequestGameStateForNextRound();
        }

        #endregion

        #region PLAYFAB

        private static void OnErrorShared(PlayFabError error)
        {
            Debug.Log(error.GenerateErrorReport());
        }

        private void RequestGameStateForNextRound()
        {
            // reset these flags
            opponentRequestedRematch = false;
            selfRequestedRematch = false;

            // get game state before moving on
            PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
            {
                FunctionName = "StartRoundOfQuickmatch",
                FunctionParameter = new
                {
                    sharedGroupId = groupId,
                    opponentId = PlayerManager.OpponentId
                },
                GeneratePlayStreamEvent = true,
            },
            StartRematch,
            OnErrorShared);
        }

        private void StartRematch(ExecuteCloudScriptResult result)
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

            // get game state from server
            localGameState = GameState.CreateFromJSON(InterpretCloudScriptData(jsonResult, "gameState"));

            // UI
            SetupStartGameUI();

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


            // turn counting debug
            if (localGameState != null)
                Debug.Log("server count: " + gameState.turnCount + "   local turn count: " + localGameState.turnCount);


            // just started game
            if (localGameState == null)
            {
                // UI
                SetupStartGameUI();

                // in case player data is missing... such as a game client rejoining a running game
                if (PlayerManager.OpponentName == null)
                    ProcessPlayerData(playerData);

                // update player names
                UpdatePlayerUI();

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

                nextRoundPanel.SetActive(true);
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
            DisableWeaponSelectUI();

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


        private void RequestRematchToServer()
        {
            PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
            {
                FunctionName = "StartNextRoundOfQuickmatch",
                FunctionParameter = new
                {
                    sharedGroupId = PlayerManager.QuickMatchId,
                },
                GeneratePlayStreamEvent = true,
            },
            OnSuccess =>
            {
                // has not yet been challenged, send event
                Debug.Log("rematch requested, event sent");
                PhotonNetwork.RaiseEvent(
                    ACCEPT_REMATCH_EVENT,        // .Code
                    null,                        // .CustomData
                    RaiseEventOptions.Default,
                    SendOptions.SendReliable);

                RequestGameStateForNextRound();
                Debug.Log("rematch starting");
            },
            errorCallback =>
            {
                Debug.Log(errorCallback.ErrorMessage + "error attempting to start rematch.");
            }
            );
        }



        #endregion

        #region END GAME 

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
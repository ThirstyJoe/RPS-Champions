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
    using UnityEngine.UI;


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

    #endregion

    public class QuickMatch : MonoBehaviourPunCallbacks
    {
        #region EVENT DEFS
        private const byte REQUEST_REMATCH_EVENT = 0;
        private const byte ACCEPT_REMATCH_EVENT = 1;
        private const byte WEAPON_CHOSEN_EVENT = 2;
        private const byte TURN_RECIEVED_EVENT = 4;

        #endregion

        #region UNITY OBJ REFS

        [SerializeField] private TextMeshProUGUI opponentNameText;
        [SerializeField] private TextMeshProUGUI userNameText;
        [SerializeField] private TextMeshProUGUI seriesRecordText;
        [SerializeField] private TextMeshProUGUI winText;
        [SerializeField] private TextMeshProUGUI loseText;
        [SerializeField] private TextMeshProUGUI drawText;
        [SerializeField] private TextMeshProUGUI countdownText;
        [SerializeField] private TextMeshProUGUI gameStatusText; // "select Rock Paper or Scissors", "Waiting for opponent...",
        [SerializeField] private GameObject nextRoundPanel;
        [SerializeField] private GameObject winPanel;
        [SerializeField] private GameObject losePanel;
        [SerializeField] private GameObject drawPanel;
        [SerializeField] private GameObject rematchButton;
        [SerializeField] private GameObject chooseWeaponPanel;
        [SerializeField] private GameObject showWeaponPanel;
        [SerializeField] private GameObject OpponentQuitPanel;
        [SerializeField] private GameObject DisconnectPanel;
        [SerializeField] private ToggleGroup WeaponToggleGroup;
        [SerializeField] private GameObject[] opponentWeaponChoice; // Reveal: rock, paper, scissors
        [SerializeField] private GameObject[] myWeaponChoice; // Reveal: rock, paper, scissors
        [SerializeField] private GameObject[] weaponToggles;


        #endregion

        #region PRIVATE VARS

        // flags used to help synchronize start and end of matches
        private bool opponentRequestedRematch = false;
        private bool selfRequestedRematch = false;
        private bool selfServerRecievedTurn = false;
        private bool opponentServerRecievedTurn = false;
        private bool turnTimerActive = false;

        // flags for when each player has made their weapon choice
        private bool opponentChoiceMade = false;
        private bool selfChoiceMade = false;

        // client tracking some game stats to display
        private WinLoseDrawStats wldStats = new WinLoseDrawStats();
        private bool waitingForGameStateUpdate = false;

        // local game data
        private GameState localGameState;
        private GameSettings gameSettings;
        private TurnData localTurnData = new TurnData();
        private string groupId;

        // isHost flag is used to figure out whose data is whose from the server
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

        public override void OnDisconnected(DisconnectCause cause)
        {
            Debug.Log("You have been disconnected: " + cause.ToString());

            DisconnectPanel.SetActive(true);
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

            countdownText.text = string.Empty;
            WeaponToggleGroup.allowSwitchOff = true;
            WeaponToggleGroup.SetAllTogglesOff();
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

        private void SetupCountdownUI()
        {
            gameStatusText.text = "Round ending...";
        }

        private void SetupStartGameUI()
        {
            // reset these flags
            selfChoiceMade = false;
            opponentChoiceMade = false;
            opponentRequestedRematch = false;
            selfRequestedRematch = false;
            opponentServerRecievedTurn = false;
            selfServerRecievedTurn = false;
            gameStatusText.text = "Select Rock, Paper, or Scissors";

            // toggles
            foreach (var toggle in weaponToggles)
            {
                toggle.SetActive(true);
                toggle.GetComponentInChildren<UnityEngine.UI.Toggle>().isOn = false;
            }
            WeaponToggleGroup.allowSwitchOff = true;
            WeaponToggleGroup.SetAllTogglesOff();

            countdownText.text = string.Empty;
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

            // show your choice for 2 seconds to allow time for server to gogogo
            yield return new WaitForSeconds(2.0F);

            while (waitingForGameStateUpdate)
            {
                // both players have heard from server 
                if (!(opponentServerRecievedTurn && selfServerRecievedTurn))
                    yield return null;

                // waitingForGameStateUpdate set to false when next turn is started
                UpdateGameStateFromServer();

                if (waitingForGameStateUpdate)
                {
                    // wait another 1.0 seconds if server has not finished turn
                    yield return new WaitForSeconds(1.0F);
                }

                // TODO: after several unsuccessful tries, disconnect from match with error
            }

            turnTimerActive = false;
        }

        #endregion

        #region PLAYER ACTIONS

        public void OnSelectRock()
        {
            // make sure this toggle is on since this event fires when toggled off as well
            if (weaponToggles[(int)Weapon.Rock].GetComponentInChildren<UnityEngine.UI.Toggle>().isOn)
                ChooseWeapon(Weapon.Rock);
        }
        public void OnSelectPaper()
        {
            // make sure this toggle is on since this event fires when toggled off as well
            if (weaponToggles[(int)Weapon.Paper].GetComponentInChildren<UnityEngine.UI.Toggle>().isOn)
                ChooseWeapon(Weapon.Paper);
        }
        public void OnSelectScissors()
        {
            // make sure this toggle is on since this event fires when toggled off as well
            if (weaponToggles[(int)Weapon.Scissors].GetComponentInChildren<UnityEngine.UI.Toggle>().isOn)
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

            // send event that weapon has been chosen
            if (!selfChoiceMade)
            {
                selfChoiceMade = true;
                PhotonNetwork.RaiseEvent(
                       WEAPON_CHOSEN_EVENT,        // .Code
                       null,                       // .CustomData
                       RaiseEventOptions.Default,
                       SendOptions.SendReliable);

                if (opponentChoiceMade)
                {
                    RequestStartCountdown();
                }
                else
                {
                    gameStatusText.text = "Waiting for opponent...";
                }
            }
        }


        public void OnRematchButtonPressed()
        {
            Debug.Log("rematch requested, event sent");

            // send event
            PhotonNetwork.RaiseEvent(
                REQUEST_REMATCH_EVENT,        // .Code
                null,                         // .CustomData
                RaiseEventOptions.Default,
                SendOptions.SendReliable);

            // set local flag
            selfRequestedRematch = true;

            // set UI
            rematchButton.SetActive(false);

            // only host communicate with server
            if (opponentRequestedRematch && isHost)
            {
                RequestRematchToServer();
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
                    SetupStartGameUI();
                    break;
                case WEAPON_CHOSEN_EVENT:
                    OpponentSelectedWeapon();
                    break;
                case TURN_RECIEVED_EVENT:
                    OpponentTurnRecievedByServer();
                    break;
            }
        }

        private void OpponentTurnRecievedByServer()
        {
            opponentServerRecievedTurn = true;
        }

        private void IncomingRematchRequest()
        {
            opponentRequestedRematch = true;
            if (selfRequestedRematch)
                RequestRematchToServer();
        }


        private void OpponentSelectedWeapon()
        {
            opponentChoiceMade = true;
            if (selfChoiceMade)
                RequestStartCountdown();
        }

        #endregion

        #region PLAYFAB

        private static void OnErrorShared(PlayFabError error)
        {
            Debug.Log(error.GenerateErrorReport());
        }

        private void RequestStartCountdown()
        {
            // get game state before moving on
            PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
            {
                FunctionName = "StartRoundOfQuickmatch",
                FunctionParameter = new
                {
                    sharedGroupId = groupId,
                },
                GeneratePlayStreamEvent = true,
            },
            StartCountdown,
            OnErrorShared);
        }

        private void StartCountdown(ExecuteCloudScriptResult result)
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
            localGameState = GameState.CreateFromJSON(RPSCommon.InterpretCloudScriptData(jsonResult, "gameState"));

            // start timer
            if (!turnTimerActive) // prevent double action
            {
                turnTimerActive = true;
                SetupCountdownUI();
                StartCoroutine(TurnTimer(localGameState.turnCompletionTime));
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
            TurnData turnData = TurnData.CreateFromJSON(RPSCommon.InterpretCloudScriptData(jsonResult, "turnData"));
            GameState gameState = GameState.CreateFromJSON(RPSCommon.InterpretCloudScriptData(jsonResult, "gameState"));
            PlayerData playerData = PlayerData.CreateFromJSON(RPSCommon.InterpretCloudScriptData(jsonResult, "playerData"));
            gameSettings = GameSettings.CreateFromJSON(RPSCommon.InterpretCloudScriptData(jsonResult, "gameSettings"));


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
            // flag and send event to other player to help synchronize
            selfServerRecievedTurn = true;
            PhotonNetwork.RaiseEvent(
                  TURN_RECIEVED_EVENT,        // .Code
                  null,                       // .CustomData
                  RaiseEventOptions.Default,
                  SendOptions.SendReliable);
            Debug.Log("server recieved updated turn data from player");
        }


        private void RequestRematchToServer()
        {
            PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
            {
                FunctionName = "StartRoundOfQuickmatch",
                FunctionParameter = new
                {
                    sharedGroupId = PlayerManager.QuickMatchId,
                },
                GeneratePlayStreamEvent = true,
            },
            OnSuccess =>
            {
                Debug.Log("rematch accepted, event sent");
                PhotonNetwork.RaiseEvent(
                    ACCEPT_REMATCH_EVENT,        // .Code
                    null,                        // .CustomData
                    RaiseEventOptions.Default,
                    SendOptions.SendReliable);

                SetupStartGameUI();
                Debug.Log("rematch requested");
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
            PlayerManager.UpdatePlayerStats(); // update stats
            DisconnectFromGame();
        }

        private void DisconnectFromGame()
        {
            PhotonNetwork.Disconnect();
            PlayerManager.UpdatePlayerStats();
            SceneManager.LoadScene("MainMenu");
        }

        #endregion
    }
}
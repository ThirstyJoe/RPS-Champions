namespace ThirstyJoe.RPSChampions
{
    using UnityEngine;
    using PlayFab;
    using PlayFab.ClientModels;
    using TMPro;
    using System.Collections;
    using PlayFab.Json;
    using UnityEngine.SceneManagement;
    using UnityEngine.UI;
    using UnityEngine.EventSystems;
    using System;

    public class NewLeague : MonoBehaviour
    {
        // alert
        [SerializeField] private GameObject alertPanel;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI alertPanelText;
        [SerializeField] private TMP_InputField matchCountInputField;
        [SerializeField] private TMP_InputField roundDurationInputField;
        [SerializeField] private GameObject matchCountGameObject;
        [SerializeField] private GameObject roundDurationGameObject;


        // input fields
        [SerializeField] private TMP_InputField leagueNameInput;


        // tracking previous selection, for when returning from this menu
        GameObject prevUISelection;

        private void Start()
        {
            titleText.text = "New " + LeagueManager.leagueSettings.LeagueType + " League";
            roundDurationInputField.text = "4";
            matchCountInputField.text = "8";
            prevUISelection = EventSystem.current.currentSelectedGameObject;

            // hide match count and round duration input boxes if this is a rated match
            matchCountGameObject.SetActive(false);
            roundDurationGameObject.SetActive(false);
        }

        public void OnAlertConfirmButtonPress()
        {
            alertPanel.SetActive(false);
        }
        public void OnBackButtonPress()
        {
            EventSystem.current.SetSelectedGameObject(prevUISelection);
            SceneManager.UnloadSceneAsync("NewLeague");
        }
        public void OnCreateLeagueButtonPress()
        {
            // Manage UI
            SetAllButtonsInteractable(false);

            // Save input from league settings input fields
            LeagueManager.SetMatchCount(Int32.Parse(matchCountInputField.text));
            // * 3600 is hours to seconds conversion
            LeagueManager.SetRoundDuration(Int32.Parse(roundDurationInputField.text) * 3600);
            LeaguePlayerStats leaguePlayerData = new LeaguePlayerStats(
                PlayerManager.PlayerName,
                PlayerPrefs.GetString("playFabId")
            );

            // call to Playfab cloud script function called "CreateNewLeague"
            PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
            {
                FunctionName = "CreateNewLeague",
                FunctionParameter = new
                {
                    status = "Open",
                    leagueName = leagueNameInput.text,
                    hostName = PlayerManager.PlayerName,
                    playerData = leaguePlayerData.ToJSON(),
                    leagueSettings = LeagueManager.leagueSettings.ToJSON()
                },
                GeneratePlayStreamEvent = true,
            },
            result =>
            {
                // get Json object representing the Game State out of FunctionResult
                JsonObject jsonResult = (JsonObject)result.FunctionResult;

                // check if data exists
                if (jsonResult == null)
                {
                    Debug.Log("server failed to return data");
                }
                else
                {
                    Debug.Log("New League created");

                    // save key for accessing the league from server
                    TitleDescriptionButtonLinkData.LinkID =
                        RPSCommon.InterpretCloudScriptData(jsonResult, "leagueKey");

                    // reselect UI element from league dashboard
                    EventSystem.current.SetSelectedGameObject(prevUISelection);

                    // Load league view for displaying new league
                    SceneManager.UnloadSceneAsync("NewLeague");
                    SceneManager.LoadScene("LeagueView", LoadSceneMode.Additive);
                }
            },
            errorCallback =>
            {
                Debug.Log(errorCallback.ErrorMessage + "error creating new League.");

                // TODO: more specific error messages to help with league creation.
                alertPanelText.text =
                    "Oops! According to the server, The league could not be created.";
                alertPanel.SetActive(true);

                // let player interact again
                SetAllButtonsInteractable(true);
            }
            );
        }

        public void SetAllButtonsInteractable(bool value)
        {
            foreach (GameObject button in GameObject.FindGameObjectsWithTag("MenuButton"))
            {
                // disable buttons
                button.GetComponent<UnityEngine.UI.Button>().interactable = value;
            }
        }
    }

}
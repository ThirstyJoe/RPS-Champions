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

    public class NewLeague : MonoBehaviour
    {
        // alert
        [SerializeField] private GameObject alertPanel;
        [SerializeField] private TextMeshProUGUI alertPanelText;


        // input fields
        [SerializeField] private TMP_InputField leagueNameInput;


        // tracking previous selection, for when returning from this menu
        GameObject prevUISelection;

        private void Start()
        {
            prevUISelection = EventSystem.current.currentSelectedGameObject;
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
            SetAllButtonsInteractable(false);

            // TODO: Save league settings in LeagueManager
            // LeagueSettings leagueSettings = new LeagueSettings();
            // LeagueManager.SetCurrentLeagueSettings(leagueSettings);
            LeaguePlayer leaguePlayerData = new LeaguePlayer(
                PlayerManager.PlayerName,
                PlayerPrefs.GetString("playFabId"),
                1200);
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

                    TitleDescriptionButtonLinkData.LinkID = RPSCommon.InterpretCloudScriptData(jsonResult, "leagueKey");

                    EventSystem.current.SetSelectedGameObject(prevUISelection);

                    SceneManager.UnloadSceneAsync("NewLeague");
                    SceneManager.LoadScene("LeagueView", LoadSceneMode.Additive);
                }
            },
            errorCallback =>
            {
                Debug.Log(errorCallback.ErrorMessage + "error creating new League.");

                // TODO: more specific error messages to help with league creation.
                alertPanelText.text = "Oops! According to the server, The league could not be created.";
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
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




    public class NewLeague : MonoBehaviour
    {
        [SerializeField] private GameObject alertPanel;
        [SerializeField] private TextMeshProUGUI alertPanelText;

        // input fields
        [SerializeField] private TMP_InputField leagueNameInput;

        public void OnAlertConfirmButtonPress()
        {
            alertPanel.SetActive(false);
        }
        public void OnBackButtonPress()
        {
            SceneManager.UnloadSceneAsync("NewLeague");
        }
        public void OnCreateLeagueButtonPress()
        {
            SetAllButtonsInteractable(false);

            // TODO: Save league settings in LeagueManager
            // LeagueSettings leagueSettings = new LeagueSettings();
            // LeagueManager.SetCurrentLeagueSettings(leagueSettings);

            PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
            {
                FunctionName = "CreateNewLeague",
                FunctionParameter = new
                {
                    status = "Open",
                    leagueName = leagueNameInput.text,
                    hostId = PlayerManager.QuickMatchId,
                    hostName = PlayerManager.PlayerName,
                    leagueSettings = LeagueManager.leagueSettings.ToJSON()
                },
                GeneratePlayStreamEvent = true,
            },
            OnSuccess =>
            {
                Debug.Log("New League created");
                SceneManager.UnloadSceneAsync("NewLeague");

                // message returned from cloud script
                JsonObject jsonResult = (JsonObject)OnSuccess.FunctionResult;
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
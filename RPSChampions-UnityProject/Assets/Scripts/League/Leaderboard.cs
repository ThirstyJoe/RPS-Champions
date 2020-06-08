namespace ThirstyJoe.RPSChampions
{
    using UnityEngine;
    using TMPro;
    using UnityEngine.SceneManagement;
    using UnityEngine.EventSystems;
    using PlayFab;
    using PlayFab.ClientModels;

    public class Leaderboard : MonoBehaviour
    {
        [SerializeField] private GameObject PlayerListContent;
        [SerializeField] private GameObject PlayerButtonPrefab;

        // tracking previous selection, for when returning from this menu
        private GameObject prevUISelection;

        private void Start()
        {
            prevUISelection = EventSystem.current.currentSelectedGameObject;

            PopulateLeaderboard();
        }

        private void PopulateLeaderboard()
        {
            // request leaderboard from playfab
            PlayFabClientAPI.GetLeaderboard(new GetLeaderboardRequest()
            {
                //MaxResultsCount = 999,
                //StartPosition = 1,
                StatisticName = "Rating"
            },
            result =>
            {
                int rank = 1;
                foreach (var playerEntry in result.Leaderboard)
                {
                    if (string.IsNullOrEmpty(playerEntry.DisplayName))
                        continue;

                    GameObject obj = Instantiate(PlayerButtonPrefab, PlayerListContent.transform);
                    var tdButton = obj.GetComponent<TitleDescriptionButton>();
                    var buttonData = new TitleDescriptionButtonData(
                        playerEntry.PlayFabId,
                        playerEntry.DisplayName,
                        playerEntry.StatValue.ToString()
                    );
                    tdButton.SetupButton(buttonData, "PlayerProfile");
                    tdButton.SetRankText(rank++);
                }
            },
            RPSCommon.OnPlayFabError
            );
        }

        public void OnBackButtonPress()
        {
            // return to previous menu
            EventSystem.current.SetSelectedGameObject(prevUISelection);
            SceneManager.UnloadSceneAsync("Leaderboard");
        }
    }
}
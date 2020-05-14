namespace ThirstyJoe.RPSChampions
{
    using UnityEngine;
    using TMPro;
    using UnityEngine.SceneManagement;

    public class NewLeague : MonoBehaviour
    {
        public void OnBackButtonPress()
        {
            SceneManager.UnloadSceneAsync("NewLeague");
        }
        public void OnCreateLeagueButtonPress()
        {
            // TODO: Save league settings in LeagueManager
            // LeagueSettings leagueSettings = new LeagueSettings();
            // LeagueManager.SetCurrentLeagueSettings(leagueSettings);
            SceneManager.UnloadSceneAsync("NewLeague");
        }
    }
}
namespace ThirstyJoe.RPSChampions
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using TMPro;
    using UnityEngine.SceneManagement;

    public static class TitleDescriptionButtonLinkID
    {
        public static string LastSavedLinkID;
    }

    public class TitleDescriptionButton : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI Title;
        [SerializeField] private TextMeshProUGUI Description;

        private string LinkID;

        private string LoadSceneName;

        public void OnPressedTitleDescriptionButton()
        {
            TitleDescriptionButtonLinkID.LastSavedLinkID = LinkID;
            SceneManager.LoadScene(LoadSceneName, LoadSceneMode.Additive);
        }

        public void SetText(TitleDescriptionButtonData tdPair)
        {
            Title.SetText(tdPair.Title);
            Description.SetText(tdPair.Description);
        }

        public void SetLoadSceneName(string loadSceneName)
        {
            LoadSceneName = loadSceneName;
        }


        public void SetupButton(TitleDescriptionButtonData tdPair, string sceneName)
        {

            LinkID = tdPair.LinkID;
            SetLoadSceneName(sceneName);
            SetText(tdPair);
        }
    }
}

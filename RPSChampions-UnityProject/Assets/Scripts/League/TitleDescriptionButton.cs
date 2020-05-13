namespace ThirstyJoe.RPSChampions
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using TMPro;
    using UnityEngine.SceneManagement;

    public class TitleDescriptionButton : MonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI Title;
        [SerializeField]
        private TextMeshProUGUI Description;

        private string LoadSceneName;

        public void OnPressedTitleDescriptionButton()
        {
            SceneManager.LoadScene(LoadSceneName, LoadSceneMode.Additive);
        }

        public void SetText(TitleDescriptionPair tdPair)
        {
            Title.SetText(tdPair.Title);
            Description.SetText(tdPair.Description);
        }

        public void SetLoadSceneName(string loadSceneName)
        {
            LoadSceneName = loadSceneName;
        }

        public void SetupButton(TitleDescriptionPair tdPair, string sceneName)
        {
            SetLoadSceneName(sceneName);
            SetText(tdPair);
        }
    }
}

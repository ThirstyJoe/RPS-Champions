﻿namespace ThirstyJoe.RPSChampions
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using TMPro;
    using UnityEngine.SceneManagement;

    public static class TitleDescriptionButtonLinkData
    {
        public static string LinkID;
        public static int DataIndex;
        public static string Label;
    }

    public class TitleDescriptionButton : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI Title;
        [SerializeField] private TextMeshProUGUI Description;

        private string LinkID;
        private int DataIndex; // index used to retrieve data from an Array
        private string Label; // additional string data for labelling what menu the link is accessed from

        private string LoadSceneName;

        public void OnPressedTitleDescriptionButton()
        {
            TitleDescriptionButtonLinkData.LinkID = LinkID;
            TitleDescriptionButtonLinkData.DataIndex = DataIndex;
            TitleDescriptionButtonLinkData.Label = Label;
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

        public void SetupButton(TitleDescriptionButtonData tdPair, string sceneName, string label = "", int dataIndex = 0)
        {
            DataIndex = dataIndex;
            LinkID = tdPair.LinkID;
            Label = label;
            SetLoadSceneName(sceneName);
            SetText(tdPair);
        }
    }
}

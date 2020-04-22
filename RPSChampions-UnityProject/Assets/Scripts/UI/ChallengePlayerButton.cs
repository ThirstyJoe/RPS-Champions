
namespace ThirstyJoe.RPSChampions
{
    using System.Collections;
    using System.Collections.Generic;
    using TMPro;
    using System.Text;
    using UnityEngine.SceneManagement;
    using UnityEngine;
    using UnityEngine.UIElements;
    using UnityEngine.EventSystems;

    public class ChallengePlayerButton : MonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI nameText;
        [SerializeField]
        private TextMeshProUGUI statsText;
        [SerializeField]
        private GameObject challengeHighlight;
        [SerializeField]
        private GameObject requestHighlight;
        [SerializeField]
        private GameObject buttonDefault;
        [SerializeField]
        private GameObject buttonChallenged;
        [SerializeField]
        private EnterQuickMatchMenu menu;

        private bool requested = false;
        private bool challenged = false;
        private string playerName;


        public void SetButtonText(PlayerStats stats)
        {
            playerName = stats.PlayerName;
            nameText.text = stats.PlayerName;
            statsText.text =
                "Wins\t" + stats.Wins.ToString() +
                "\n" +
                "Losses\t" + stats.Losses.ToString() +
                "\n" +
                "Favors\t" + stats.FavoriteWeapon.ToString();
        }


        public void OnButtonPressed()
        {
            if (requested)
                OnRequestCancelled();
            else
                OnRequested();

            EventSystem.current.SetSelectedGameObject(null);
        }

        public void Challenged()
        {
            challenged = true;
            UpdateButton();
        }

        public void OnRequested()
        {
            requested = true;
            UpdateButton();
            menu.RequestedMatch(playerName);
        }

        public void ChallengeCancelled()
        {
            challenged = false;
            UpdateButton();
        }

        public void OnRequestCancelled()
        {
            RequestCancelled();
            menu.RequestedMatch(playerName);
            UpdateButton();
        }

        public void RequestCancelled()
        {
            requested = false;
            UpdateButton();
        }

        public void UpdateButton()
        {
            // set defaults
            challengeHighlight.SetActive(false);
            requestHighlight.SetActive(false);
            buttonDefault.SetActive(true);
            buttonChallenged.SetActive(false);

            // adjust for special cases
            if (challenged)
            {
                buttonDefault.SetActive(false);
                buttonChallenged.SetActive(true);
                challengeHighlight.SetActive(true);
            }
            else if (requested)
            {
                requestHighlight.SetActive(true);
            }
        }
    }
}

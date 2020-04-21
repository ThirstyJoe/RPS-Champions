
namespace ThirstyJoe.RPSChampions
{
    using System.Collections;
    using System.Collections.Generic;
    using TMPro;
    using System.Text;
    using UnityEngine.SceneManagement;
    using UnityEngine;

    public class ChallengePlayerButton : MonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI nameText;
        [SerializeField]
        private TextMeshProUGUI statsText;

        public void SetButtonText(PlayerStats stats)
        {
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
            SceneManager.LoadScene("QuickMatch");
        }
    }
}

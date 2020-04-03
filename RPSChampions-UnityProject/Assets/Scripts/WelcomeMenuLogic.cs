namespace ThirstyJoe.RPSChampions
{

    using UnityEngine.SceneManagement;
    using UnityEngine;

    public class WelcomeMenuLogic : MonoBehaviour
    {

        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }

        public void OnOnlinePlayButtonPress()
        {
            SceneManager.LoadScene("GameLobby");
        }
    }
}

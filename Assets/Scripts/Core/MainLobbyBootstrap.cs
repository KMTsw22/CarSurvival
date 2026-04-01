using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(MainLobbyUI))]
public class MainLobbyBootstrap : MonoBehaviour
{
    private MainLobbyUI lobbyUI;

    private void Awake()
    {
        lobbyUI = GetComponent<MainLobbyUI>();

        if (lobbyUI != null)
        {
            lobbyUI.OnStartGameClicked += () => SceneManager.LoadScene("SampleScene");
        }
    }
}

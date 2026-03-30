using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(MainLobbyUI))]
[RequireComponent(typeof(TabNavigationController))]
[RequireComponent(typeof(PartsTabUI))]
public class MainLobbyBootstrap : MonoBehaviour
{
    private MainLobbyUI lobbyUI;
    private TabNavigationController tabNav;
    private PartsTabUI partsTabUI;

    private void Awake()
    {
        lobbyUI = GetComponent<MainLobbyUI>();
        tabNav = GetComponent<TabNavigationController>();
        partsTabUI = GetComponent<PartsTabUI>();

        if (lobbyUI != null)
        {
            lobbyUI.OnStartGameClicked += () => SceneManager.LoadScene("SampleScene");
        }
    }
}

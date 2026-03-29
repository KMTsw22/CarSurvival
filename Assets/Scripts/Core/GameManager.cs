using UnityEngine;
using UnityEngine.SceneManagement;
using System;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum GameState
    {
        Lobby,
        Garage,
        Playing,
        LevelUp,
        Paused,
        GameOver,
        RunComplete
    }

    [Header("References")]
    public PartsDatabase partsDatabase;

    public GameState CurrentState { get; private set; } = GameState.Playing;

    public event Action<GameState> OnGameStateChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void SetState(GameState newState)
    {
        CurrentState = newState;
        OnGameStateChanged?.Invoke(newState);

        switch (newState)
        {
            case GameState.LevelUp:
                Time.timeScale = 0f;
                break;
            case GameState.Playing:
                Time.timeScale = 1f;
                break;
            case GameState.GameOver:
            case GameState.RunComplete:
                Time.timeScale = 0f;
                break;
        }
    }

    public void OnRunComplete()
    {
        SetState(GameState.RunComplete);
    }

    public void OnPlayerDeath()
    {
        SetState(GameState.GameOver);
    }

    public void RestartRun()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void GoToLobby()
    {
        Time.timeScale = 1f;
        SetState(GameState.Lobby);
        SceneManager.LoadScene("MainLobby");
    }

    public void StartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("SampleScene");
    }
}

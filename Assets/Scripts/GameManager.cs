using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public enum Screen
    {
        MainMenu,
        Game,
        End
    }

    [SerializeField] GameConfig config = new GameConfig();

    [SerializeField] GameUI ui;

    [SerializeField] QuestionDatabase questionDatabase;

    public GameConfig Config => config;

    public GameSession Session { get; private set; }

    public Screen CurrentScreen { get; private set; } = Screen.MainMenu;

    public event Action ScreenChanged;

    void Start()
    {
        if (ui == null) ui = FindFirstObjectByType<GameUI>();
        if (questionDatabase == null) questionDatabase = GetComponent<QuestionDatabase>();
        if (questionDatabase == null) questionDatabase = FindFirstObjectByType<QuestionDatabase>();

        if (ui != null) ui.Bind(this);
        else Debug.LogWarning("No GameUI found. The game logic runs but nothing is displayed.");

        SetScreen(Screen.MainMenu);
    }

    void Update()
    {
        if (Session == null) return;

        if (CurrentScreen == Screen.Game) Session.Tick(Time.deltaTime);
    }

    public void StartGame(Difficulty difficulty)
    {
        if (questionDatabase == null || questionDatabase.questions.Count == 0)
        {
            Debug.LogError("No questions assigned. Add them to the Question Database on the Game object.");
            return;
        }

        Session = new GameSession(config, questionDatabase.questions, questionDatabase.radioIntros);
        Session.QuestionStarted += HandleQuestionStarted;
        Session.GameEnded += HandleGameEnded;

        SetScreen(Screen.Game);
        Session.StartNewGame(difficulty);
    }

    public void StartEasy()
    {
        StartGame(Difficulty.Easy);
    }

    public void StartHard()
    {
        StartGame(Difficulty.Hard);
    }

    public void PlayAgain()
    {
        StartGame(Session != null ? Session.Difficulty : Difficulty.Easy);
    }

    public void ReturnToMainMenu()
    {
        Session = null;
        SetScreen(Screen.MainMenu);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        Debug.Log("QUIT pressed. This closes the application in a build.");
#else
        Application.Quit();
#endif
    }

    public bool SubmitAnswer(string answer)
    {
        return Session != null && Session.SubmitAnswer(answer);
    }

    void SetScreen(Screen screen)
    {
        CurrentScreen = screen;
        ScreenChanged?.Invoke();
    }

    void HandleQuestionStarted()
    {
        if (ui != null) ui.OnQuestionStarted();
    }

    void HandleGameEnded()
    {
        SetScreen(Screen.End);
    }
}

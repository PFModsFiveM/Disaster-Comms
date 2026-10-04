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

    [SerializeField] AudioManager audioManager;

    [SerializeField] GameUI ui;

    [SerializeField] QuestionDatabase questionDatabase;

    public GameConfig Config => config;
    public AudioManager Audio => audioManager;

    public GameSession Session { get; private set; }

    public Screen CurrentScreen { get; private set; } = Screen.MainMenu;

    public event Action ScreenChanged;

    void Start()
    {
        if (audioManager == null) audioManager = GetComponent<AudioManager>();
        if (audioManager == null) audioManager = FindFirstObjectByType<AudioManager>();
        if (ui == null) ui = FindFirstObjectByType<GameUI>();
        if (questionDatabase == null) questionDatabase = GetComponent<QuestionDatabase>();
        if (questionDatabase == null) questionDatabase = FindFirstObjectByType<QuestionDatabase>();

        if (ui != null) ui.Bind(this);
        else Debug.LogWarning("[DisasterComms] No GameUI found. The game logic runs but nothing is displayed.");

        SetScreen(Screen.MainMenu);
    }

    void Update()
    {
        if (Session == null) return;

        if (CurrentScreen == Screen.Game) Session.Tick(Time.deltaTime);

        if (audioManager != null)
        {
            audioManager.SetAlarm(CurrentScreen == Screen.Game && Session.AlarmShouldPlay);
        }
    }

    void OnDisable()
    {
        if (audioManager != null) audioManager.SetAlarm(false);
    }

    public void StartGame(Difficulty difficulty)
    {
        if (audioManager != null) audioManager.SetAlarm(false);

        if (questionDatabase == null || questionDatabase.questions.Count == 0)
        {
            Debug.LogError("[Disaster Comms] No questions assigned. Add them to the Question Database on the Game object.");
            return;
        }

        Session = new GameSession(config, questionDatabase.questions, questionDatabase.radioIntros);
        Session.QuestionStarted += HandleQuestionStarted;
        Session.AnswerEvaluated += HandleAnswerEvaluated;
        Session.MeltdownFlashed += HandleMeltdownFlashed;
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
        if (audioManager != null) audioManager.SetAlarm(false);
        Session = null;
        SetScreen(Screen.MainMenu);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        Debug.Log("[DisasterComms] QUIT pressed. This closes the application in a build.");
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
        if (audioManager != null)
        {
            audioManager.PlayRadioStatic();
            audioManager.PlayNewCommunication();
        }

        if (ui != null) ui.OnQuestionStarted();
    }

    void HandleAnswerEvaluated(AnswerResult result)
    {
        if (audioManager == null) return;

        if (result == AnswerResult.Correct) audioManager.PlayCorrect();
        else audioManager.PlayIncorrect();
    }

    void HandleMeltdownFlashed()
    {
        if (audioManager != null) audioManager.PlayExplosion();
    }

    void HandleGameEnded()
    {
        if (audioManager != null) audioManager.SetAlarm(false);
        SetScreen(Screen.End);
    }
}

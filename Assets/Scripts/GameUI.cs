using UnityEngine;
using UnityEngine.UI;

public class GameUI : MonoBehaviour
{
    [Header("Screens")]
    public GameObject mainMenuScreen;
    public GameObject gameScreen;
    public GameObject endScreen;

    [Header("Main menu")]
    public Button easyButton;
    public Button hardButton;
    public Button quitButton;

    [Header("Game - header")]
    public Text difficultyLabel;
    public Text questionCounterLabel;

    [Header("Game - reactor")]
    public Image combustionFill;
    public Text combustionLabel;
    public Text statusLabel;
    public Text warningLabel;

    [Header("Game - radio")]
    public Text signalLabel;
    public Text radioIntroLabel;
    public Text radioMessageLabel;

    [Header("Game - timer")]
    public Image timerFill;
    public Text timerLabel;

    [Header("Game - answers (Easy)")]
    public GameObject choicesPanel;
    public Button[] choiceButtons;

    [Header("Game - answers (Hard)")]
    public GameObject textInputPanel;
    public InputField answerInput;
    public Button submitButton;

    [Header("Game - feedback and meltdown")]
    public Text feedbackLabel;
    public Text meltdownLabel;
    public GameObject flashPanel;

    [Header("Wording")]
    public string winTitle = "REACTOR STABILIZED";
    public string winSubtitle = "COMMUNICATION RESTORED";
    public string loseTitle = "REACTOR DESTROYED";
    public string loseSubtitle = "CONTAINMENT FAILURE : TOTAL LOSS";
    public string playAgainText = "PLAY AGAIN";
    public string tryAgainText = "TRY AGAIN";
    public string warningText = "** REACTOR WARNING : CRITICAL **";
    public string signalActiveText = "SIGNAL: ACTIVE";
    public string signalLostText = "SIGNAL: INTERRUPTED";

    [Header("Label formats")]
    public string difficultyFormat = "DIFFICULTY: {0}";
    public string questionCounterFormat = "QUESTION {0} / {1}";
    public string combustionFormat = "REACTOR COMBUSTION: {0}%";
    public string statusFormat = "STATUS: {0}";
    public string timerFormat = "{0} SECONDS";
    public string radioIntroFormat = "> {0}";
    public string radioMessageFormat = "> \"{0}\"";

    [TextArea(5, 8)]
    public string statsFormat =
        "DIFFICULTY: {0}\nCORRECT ANSWERS: {1}\nWRONG ANSWERS: {2}\nTIMED OUT: {3}\nAVERAGE RESPONSE TIME: {4}s";

    [Header("Reactor bar colours")]
    public Color stableColor = new Color(0.25f, 0.75f, 0.35f);
    public Color warningColor = new Color(0.9f, 0.7f, 0.15f);
    public Color criticalColor = new Color(0.85f, 0.2f, 0.15f);

    [Header("End screen")]
    public Text endTitleLabel;
    public Text endSubtitleLabel;
    public Text endStatsLabel;
    public Button playAgainButton;
    public Button mainMenuButton;

    GameManager manager;

    public void Bind(GameManager gameManager)
    {
        manager = gameManager;
        manager.ScreenChanged += RefreshScreens;
        RefreshScreens();
    }

    void Update()
    {
        if (manager == null) return;

        GameSession session = manager.Session;
        if (session == null) return;

        if (manager.CurrentScreen == GameManager.Screen.Game) RefreshGameScreen(session);
    }

    public void OnQuestionStarted()
    {
        GameSession session = manager != null ? manager.Session : null;
        if (session == null) return;

        bool multipleChoice = session.Settings.InputType == AnswerInputType.MultipleChoice;

        if (choicesPanel != null) choicesPanel.SetActive(multipleChoice);
        if (textInputPanel != null) textInputPanel.SetActive(!multipleChoice);

        if (choiceButtons != null)
        {
            for (int i = 0; i < choiceButtons.Length; i++)
            {
                if (choiceButtons[i] == null) continue;

                bool used = multipleChoice && i < session.CurrentChoices.Count;
                choiceButtons[i].gameObject.SetActive(used);
                if (used) SetButtonLabel(choiceButtons[i], session.CurrentChoices[i].ToUpperInvariant());
            }
        }

        if (!multipleChoice && answerInput != null)
        {
            answerInput.text = "";
            if (answerInput.isActiveAndEnabled) answerInput.ActivateInputField();
        }

        if (flashPanel != null) flashPanel.SetActive(false);
    }

    public void SubmitChoice(int index)
    {
        GameSession session = manager != null ? manager.Session : null;
        if (session == null || !session.IsQuestionActive) return;
        if (index < 0 || index >= session.CurrentChoices.Count) return;

        manager.SubmitAnswer(session.CurrentChoices[index]);
    }

    public void SubmitTypedAnswer()
    {
        GameSession session = manager != null ? manager.Session : null;
        if (session == null || !session.IsQuestionActive || answerInput == null) return;

        manager.SubmitAnswer(answerInput.text);
    }

    void RefreshScreens()
    {
        if (manager == null) return;

        GameManager.Screen screen = manager.CurrentScreen;
        if (mainMenuScreen != null) mainMenuScreen.SetActive(screen == GameManager.Screen.MainMenu);
        if (gameScreen != null) gameScreen.SetActive(screen == GameManager.Screen.Game);
        if (endScreen != null) endScreen.SetActive(screen == GameManager.Screen.End);

        if (screen != GameManager.Screen.Game && flashPanel != null) flashPanel.SetActive(false);
        if (screen == GameManager.Screen.End && manager.Session != null) RefreshEndScreen(manager.Session);
    }

    void RefreshGameScreen(GameSession session)
    {
        SetText(difficultyLabel, Format(difficultyFormat, session.Difficulty.ToString().ToUpperInvariant()));
        SetText(questionCounterLabel,
            Format(questionCounterFormat, Mathf.Min(session.QuestionNumber, session.TotalQuestions), session.TotalQuestions));

        SetBar(combustionFill, session.CombustionNormalized);
        if (combustionFill != null) combustionFill.color = StatusColor(session.Status);
        SetText(combustionLabel, Format(combustionFormat, Mathf.RoundToInt(session.Combustion)));
        SetText(statusLabel, Format(statusFormat, session.Status.ToString().ToUpperInvariant()));

        if (warningLabel != null)
        {
            bool critical = session.Status == ReactorStatus.Critical || session.Status == ReactorStatus.Meltdown;
            bool visible = critical && Mathf.Repeat(Time.unscaledTime, 0.6f) < 0.35f;
            SetText(warningLabel, warningText);
            warningLabel.enabled = visible;
        }

        SetText(signalLabel, session.IsQuestionActive ? signalActiveText : signalLostText);
        SetText(radioIntroLabel, Format(radioIntroFormat, session.RadioIntro));
        SetText(radioMessageLabel, Format(radioMessageFormat, session.RadioMessage));

        bool clockRunning = session.State != SessionState.Meltdown;
        SetBar(timerFill, clockRunning ? session.TimeNormalized : 0f);
        SetText(timerLabel, Format(timerFormat, clockRunning ? Mathf.CeilToInt(session.TimeRemaining) : 0));

        SetText(feedbackLabel, session.FeedbackText);
        SetText(meltdownLabel, session.MeltdownText);
        if (flashPanel != null) flashPanel.SetActive(session.IsFlashing);

        bool answersVisible = session.State == SessionState.Asking || session.State == SessionState.Feedback;
        bool multipleChoice = session.Settings.InputType == AnswerInputType.MultipleChoice;
        if (choicesPanel != null) choicesPanel.SetActive(answersVisible && multipleChoice);
        if (textInputPanel != null) textInputPanel.SetActive(answersVisible && !multipleChoice);

        bool canAnswer = session.IsQuestionActive;
        if (choiceButtons != null)
        {
            for (int i = 0; i < choiceButtons.Length; i++)
            {
                if (choiceButtons[i] != null) choiceButtons[i].interactable = canAnswer;
            }
        }
        if (submitButton != null) submitButton.interactable = canAnswer;
        if (answerInput != null) answerInput.interactable = canAnswer;
    }

    void RefreshEndScreen(GameSession session)
    {
        bool won = session.PlayerWon;

        SetText(endTitleLabel, won ? winTitle : loseTitle);
        SetText(endSubtitleLabel, won ? winSubtitle : loseSubtitle);

        SetText(endStatsLabel, Format(statsFormat,
            session.Difficulty.ToString().ToUpperInvariant(),
            session.CorrectCount,
            session.WrongCount,
            session.TimedOutCount,
            session.AverageResponseTime.ToString("0.0")));

        if (playAgainButton != null) SetButtonLabel(playAgainButton, won ? playAgainText : tryAgainText);
    }

    static void SetText(Text label, string value)
    {
        if (label != null) label.text = value;
    }

    static void SetButtonLabel(Button button, string value)
    {
        if (button == null) return;
        Text label = button.GetComponentInChildren<Text>();
        if (label != null) label.text = value;
    }

    static void SetBar(Image fill, float normalized)
    {
        if (fill == null) return;
        normalized = Mathf.Clamp01(normalized);

        if (fill.type == Image.Type.Filled)
        {
            fill.fillAmount = normalized;
            return;
        }

        RectTransform rect = fill.rectTransform;
        rect.anchorMin = new Vector2(0f, rect.anchorMin.y);
        rect.anchorMax = new Vector2(normalized, rect.anchorMax.y);
        rect.offsetMin = new Vector2(0f, rect.offsetMin.y);
        rect.offsetMax = new Vector2(0f, rect.offsetMax.y);
    }

    Color StatusColor(ReactorStatus status)
    {
        switch (status)
        {
            case ReactorStatus.Stable: return stableColor;
            case ReactorStatus.Warning: return warningColor;
            default: return criticalColor;
        }
    }

    static string Format(string format, params object[] values)
    {
        if (string.IsNullOrEmpty(format)) return "";
        try
        {
            return string.Format(format, values);
        }
        catch (System.FormatException)
        {
            return format;
        }
    }
}

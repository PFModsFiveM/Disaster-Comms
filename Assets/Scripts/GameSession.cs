using System;
using System.Collections.Generic;

public enum AnswerResult
{
    Correct,
    Wrong,
    TimedOut
}

public enum SessionState
{
    Idle,
    Asking,
    Feedback,
    Meltdown,
    Victory,
    Defeat
}

public class GameSession
{
    struct MeltdownStage
    {
        public string Text;
        public float Duration;
        public bool Flash;
    }

    readonly GameConfig config;
    readonly Random random;
    readonly List<QuestionData> pool = new List<QuestionData>();
    readonly List<string> intros = new List<string>();
    readonly List<QuestionData> playlist = new List<QuestionData>();
    readonly List<string> choices = new List<string>();
    readonly List<MeltdownStage> meltdownStages = new List<MeltdownStage>();

    float stateTimer;
    int meltdownStageIndex;
    float totalResponseTime;
    int answeredCount;

    public GameSession(GameConfig config, IList<QuestionData> questions, IList<string> radioIntros)
        : this(config, questions, radioIntros, Environment.TickCount)
    {
    }

    public GameSession(GameConfig config, IList<QuestionData> questions, IList<string> radioIntros, int seed)
    {
        this.config = config ?? new GameConfig();
        random = new Random(seed);

        if (questions != null)
        {
            foreach (QuestionData question in questions)
            {
                if (question != null && !string.IsNullOrWhiteSpace(question.correctAnswer)) pool.Add(question);
            }
        }

        if (radioIntros != null)
        {
            foreach (string intro in radioIntros)
            {
                if (!string.IsNullOrWhiteSpace(intro)) intros.Add(intro);
            }
        }
    }

    public GameConfig Config => config;
    public Difficulty Difficulty { get; private set; }
    public DifficultySettings Settings { get; private set; }
    public SessionState State { get; private set; } = SessionState.Idle;

    public int QuestionNumber { get; private set; }
    public int TotalQuestions => playlist.Count;
    public QuestionData CurrentQuestion { get; private set; }

    public IReadOnlyList<string> CurrentChoices => choices;

    public string RadioIntro { get; private set; } = "";
    public string RadioMessage => CurrentQuestion != null ? CurrentQuestion.sentenceWithBlank : "";

    public float TimeRemaining { get; private set; }
    public float TimeNormalized => Settings.TimePerQuestion > 0f
        ? Clamp01(TimeRemaining / Settings.TimePerQuestion)
        : 0f;

    public float Combustion { get; private set; }
    public float CombustionNormalized => config.maxCombustion > 0f
        ? Clamp01(Combustion / config.maxCombustion)
        : 0f;
    public ReactorStatus Status => config.GetStatus(Combustion);

    public int CorrectCount { get; private set; }
    public int WrongCount { get; private set; }
    public int TimedOutCount { get; private set; }

    public float AverageResponseTime => answeredCount > 0 ? totalResponseTime / answeredCount : 0f;

    public string FeedbackText { get; private set; } = "";
    public string MeltdownText { get; private set; } = "";
    public bool IsFlashing { get; private set; }

    public bool IsQuestionActive => State == SessionState.Asking;
    public bool IsGameOver => State == SessionState.Victory || State == SessionState.Defeat;
    public bool PlayerWon => State == SessionState.Victory;

    public bool AlarmShouldPlay => State != SessionState.Idle && !IsGameOver
                                   && Combustion >= config.criticalThreshold;

    public event Action QuestionStarted;
    public event Action<AnswerResult> AnswerEvaluated;
    public event Action MeltdownFlashed;
    public event Action GameEnded;

    public void StartNewGame(Difficulty difficulty)
    {
        Difficulty = difficulty;
        Settings = config.GetSettings(difficulty);

        Combustion = 0f;
        CorrectCount = 0;
        WrongCount = 0;
        TimedOutCount = 0;
        totalResponseTime = 0f;
        answeredCount = 0;

        FeedbackText = "";
        MeltdownText = "";
        IsFlashing = false;
        stateTimer = 0f;
        meltdownStageIndex = 0;
        meltdownStages.Clear();
        choices.Clear();
        QuestionNumber = 0;
        CurrentQuestion = null;

        BuildPlaylist();
        StartNextQuestion();
    }

    public void Tick(float deltaTime)
    {
        if (deltaTime <= 0f) return;

        switch (State)
        {
            case SessionState.Asking:
                TimeRemaining -= deltaTime;
                if (TimeRemaining <= 0f)
                {
                    TimeRemaining = 0f;
                    Evaluate(AnswerResult.TimedOut);
                }
                break;

            case SessionState.Feedback:
                stateTimer -= deltaTime;
                if (stateTimer <= 0f) ContinueAfterFeedback();
                break;

            case SessionState.Meltdown:
                stateTimer -= deltaTime;
                if (stateTimer <= 0f) AdvanceMeltdown();
                break;
        }
    }

    public bool SubmitAnswer(string answer)
    {
        if (State != SessionState.Asking) return false;
        if (string.IsNullOrWhiteSpace(answer)) return false;

        totalResponseTime += Settings.TimePerQuestion - TimeRemaining;
        answeredCount++;

        bool correct = Matches(answer, CurrentQuestion.correctAnswer);
        Evaluate(correct ? AnswerResult.Correct : AnswerResult.Wrong);
        return true;
    }

    public static string Normalize(string value)
    {
        return value == null ? "" : value.Trim().ToLowerInvariant();
    }

    public static bool Matches(string a, string b)
    {
        return Normalize(a) == Normalize(b);
    }

    public void DebugAddCombustion(float amount)
    {
        if (IsGameOver || State == SessionState.Idle || State == SessionState.Meltdown) return;

        AddCombustion(amount);
        if (Combustion >= config.maxCombustion)
        {
            FeedbackText = "";
            EnterMeltdown();
        }
    }

    public void DebugForceTimeout()
    {
        if (State == SessionState.Asking) Evaluate(AnswerResult.TimedOut);
    }

    public void DebugSkipQuestion()
    {
        if (State != SessionState.Asking && State != SessionState.Feedback) return;

        FeedbackText = "";
        if (QuestionNumber >= TotalQuestions) EndGame(true);
        else StartNextQuestion();
    }

    public void DebugForceMeltdown()
    {
        if (IsGameOver || State == SessionState.Idle || State == SessionState.Meltdown) return;

        Combustion = config.maxCombustion;
        FeedbackText = "";
        EnterMeltdown();
    }

    public void DebugForceVictory()
    {
        if (IsGameOver || State == SessionState.Idle) return;

        FeedbackText = "";
        MeltdownText = "";
        IsFlashing = false;
        EndGame(true);
    }

    void BuildPlaylist()
    {
        playlist.Clear();
        playlist.AddRange(pool);
        Shuffle(playlist);

        int wanted = Math.Min(Math.Max(config.questionsPerGame, 1), playlist.Count);
        playlist.RemoveRange(wanted, playlist.Count - wanted);
    }

    void StartNextQuestion()
    {
        QuestionNumber++;
        CurrentQuestion = playlist[QuestionNumber - 1];
        RadioIntro = intros.Count > 0 ? intros[random.Next(intros.Count)] : "";

        BuildChoices();
        TimeRemaining = Settings.TimePerQuestion;
        FeedbackText = "";
        State = SessionState.Asking;

        QuestionStarted?.Invoke();
    }

    void BuildChoices()
    {
        choices.Clear();
        if (Settings.InputType != AnswerInputType.MultipleChoice) return;

        choices.Add(CurrentQuestion.correctAnswer);

        string[] wrong = CurrentQuestion.incorrectAnswers;
        for (int i = 0; i < wrong.Length && choices.Count < Settings.ChoiceCount; i++)
        {
            choices.Add(wrong[i]);
        }

        Shuffle(choices);
    }

    void Evaluate(AnswerResult result)
    {
        switch (result)
        {
            case AnswerResult.Correct:
                CorrectCount++;
                AddCombustion(config.combustionPerCorrectAnswer);
                FeedbackText = config.feedbackCorrect;
                break;

            case AnswerResult.Wrong:
                WrongCount++;
                AddCombustion(config.combustionPerWrongAnswer);
                FeedbackText = config.feedbackWrong;
                break;

            case AnswerResult.TimedOut:
                TimedOutCount++;
                AddCombustion(config.combustionPerTimeout);
                FeedbackText = config.feedbackTimeout;
                break;
        }

        State = SessionState.Feedback;
        stateTimer = config.feedbackDelay;
        AnswerEvaluated?.Invoke(result);
    }

    void ContinueAfterFeedback()
    {
        FeedbackText = "";

        if (Combustion >= config.maxCombustion)
        {
            EnterMeltdown();
            return;
        }

        if (QuestionNumber >= TotalQuestions)
        {
            EndGame(true);
            return;
        }

        StartNextQuestion();
    }

    void EnterMeltdown()
    {
        Combustion = config.maxCombustion;

        meltdownStages.Clear();
        meltdownStages.Add(new MeltdownStage { Text = config.meltdownFailureLine, Duration = config.meltdownMessageDuration });
        meltdownStages.Add(new MeltdownStage { Text = config.meltdownContainmentLine, Duration = config.meltdownMessageDuration });
        for (int n = Math.Max(config.meltdownCountdown, 0); n >= 1; n--)
        {
            meltdownStages.Add(new MeltdownStage { Text = n.ToString(), Duration = config.meltdownCountdownStep });
        }
        meltdownStages.Add(new MeltdownStage { Text = "", Duration = config.meltdownFlashDuration, Flash = true });

        meltdownStageIndex = -1;
        State = SessionState.Meltdown;
        stateTimer = 0f;
        AdvanceMeltdown();
    }

    void AdvanceMeltdown()
    {
        meltdownStageIndex++;

        if (meltdownStageIndex >= meltdownStages.Count)
        {
            IsFlashing = false;
            MeltdownText = "";
            EndGame(false);
            return;
        }

        MeltdownStage stage = meltdownStages[meltdownStageIndex];
        MeltdownText = stage.Text;
        IsFlashing = stage.Flash;
        stateTimer = Math.Max(stage.Duration, 0.01f);

        if (stage.Flash) MeltdownFlashed?.Invoke();
    }

    void EndGame(bool won)
    {
        State = won ? SessionState.Victory : SessionState.Defeat;
        FeedbackText = "";
        GameEnded?.Invoke();
    }

    void AddCombustion(float amount)
    {
        Combustion = Math.Max(0f, Math.Min(config.maxCombustion, Combustion + amount));
    }

    void Shuffle<T>(IList<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            T temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }
    }

    static float Clamp01(float value)
    {
        if (value < 0f) return 0f;
        return value > 1f ? 1f : value;
    }
}

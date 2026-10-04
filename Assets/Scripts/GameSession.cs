using System;
using System.Collections.Generic;
using UnityEngine;

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
    public Difficulty Difficulty;
    public SessionState State = SessionState.Idle;

    public int QuestionNumber;
    public QuestionData CurrentQuestion;
    public List<string> CurrentChoices = new List<string>();
    public string RadioIntro = "";

    public float TimePerQuestion;
    public float TimeRemaining;
    public bool MultipleChoice;

    public float Combustion;
    public int CorrectCount;
    public int WrongCount;
    public int TimedOutCount;

    public string FeedbackText = "";
    public string MeltdownText = "";
    public bool IsFlashing;

    public event Action QuestionStarted;
    public event Action GameEnded;

    GameConfig config;
    List<QuestionData> allQuestions = new List<QuestionData>();
    List<string> allIntros = new List<string>();
    List<QuestionData> playlist = new List<QuestionData>();

    float stateTimer;
    int meltdownStep;
    float totalAnswerTime;
    int answeredCount;

    public GameSession(GameConfig config, List<QuestionData> questions, List<string> radioIntros)
    {
        this.config = config;

        foreach (QuestionData question in questions)
        {
            if (question != null && !string.IsNullOrWhiteSpace(question.correctAnswer)) allQuestions.Add(question);
        }

        foreach (string intro in radioIntros)
        {
            if (!string.IsNullOrWhiteSpace(intro)) allIntros.Add(intro);
        }
    }

    public int TotalQuestions
    {
        get { return playlist.Count; }
    }

    public string RadioMessage
    {
        get { return CurrentQuestion == null ? "" : CurrentQuestion.sentenceWithBlank; }
    }

    public float TimeNormalized
    {
        get { return TimePerQuestion <= 0f ? 0f : Mathf.Clamp01(TimeRemaining / TimePerQuestion); }
    }

    public float CombustionNormalized
    {
        get { return Mathf.Clamp01(Combustion / config.maxCombustion); }
    }

    public ReactorStatus Status
    {
        get { return config.GetStatus(Combustion); }
    }

    public bool IsQuestionActive
    {
        get { return State == SessionState.Asking; }
    }

    public bool IsGameOver
    {
        get { return State == SessionState.Victory || State == SessionState.Defeat; }
    }

    public bool PlayerWon
    {
        get { return State == SessionState.Victory; }
    }

    public float AverageResponseTime
    {
        get { return answeredCount == 0 ? 0f : totalAnswerTime / answeredCount; }
    }

    public void StartNewGame(Difficulty difficulty)
    {
        Difficulty = difficulty;
        TimePerQuestion = config.GetQuestionTime(difficulty);
        MultipleChoice = config.UsesMultipleChoice(difficulty);

        Combustion = 0f;
        CorrectCount = 0;
        WrongCount = 0;
        TimedOutCount = 0;
        totalAnswerTime = 0f;
        answeredCount = 0;

        FeedbackText = "";
        MeltdownText = "";
        IsFlashing = false;
        stateTimer = 0f;
        meltdownStep = 0;
        QuestionNumber = 0;
        CurrentQuestion = null;
        CurrentChoices.Clear();

        BuildPlaylist();
        StartNextQuestion();
    }

    public void Tick(float deltaTime)
    {
        if (deltaTime <= 0f) return;

        if (State == SessionState.Asking)
        {
            TimeRemaining -= deltaTime;
            if (TimeRemaining <= 0f)
            {
                TimeRemaining = 0f;
                Evaluate(AnswerResult.TimedOut);
            }
            return;
        }

        if (State == SessionState.Feedback)
        {
            stateTimer -= deltaTime;
            if (stateTimer <= 0f) ContinueAfterFeedback();
            return;
        }

        if (State == SessionState.Meltdown)
        {
            stateTimer -= deltaTime;
            if (stateTimer <= 0f) AdvanceMeltdown();
        }
    }

    public bool SubmitAnswer(string answer)
    {
        if (State != SessionState.Asking) return false;
        if (string.IsNullOrWhiteSpace(answer)) return false;

        totalAnswerTime += TimePerQuestion - TimeRemaining;
        answeredCount++;

        if (Matches(answer, CurrentQuestion.correctAnswer)) Evaluate(AnswerResult.Correct);
        else Evaluate(AnswerResult.Wrong);

        return true;
    }

    public static string Normalize(string value)
    {
        if (value == null) return "";
        return value.Trim().ToLowerInvariant();
    }

    public static bool Matches(string a, string b)
    {
        return Normalize(a) == Normalize(b);
    }

    public void DebugAddCombustion(float amount)
    {
        if (State != SessionState.Asking && State != SessionState.Feedback) return;

        AddCombustion(amount);
        if (Combustion >= config.maxCombustion)
        {
            FeedbackText = "";
            StartMeltdown();
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
        if (State != SessionState.Asking && State != SessionState.Feedback) return;

        Combustion = config.maxCombustion;
        FeedbackText = "";
        StartMeltdown();
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
        playlist.AddRange(allQuestions);

        for (int i = playlist.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            QuestionData temp = playlist[i];
            playlist[i] = playlist[j];
            playlist[j] = temp;
        }

        int wanted = Mathf.Clamp(config.questionsPerGame, 1, playlist.Count);
        playlist.RemoveRange(wanted, playlist.Count - wanted);
    }

    void StartNextQuestion()
    {
        QuestionNumber++;
        CurrentQuestion = playlist[QuestionNumber - 1];

        if (allIntros.Count > 0) RadioIntro = allIntros[UnityEngine.Random.Range(0, allIntros.Count)];
        else RadioIntro = "";

        BuildChoices();

        TimeRemaining = TimePerQuestion;
        FeedbackText = "";
        State = SessionState.Asking;

        if (QuestionStarted != null) QuestionStarted();
    }

    void BuildChoices()
    {
        CurrentChoices.Clear();
        if (!MultipleChoice) return;

        CurrentChoices.Add(CurrentQuestion.correctAnswer);

        for (int i = 0; i < CurrentQuestion.incorrectAnswers.Length; i++)
        {
            if (CurrentChoices.Count >= config.easyChoiceCount) break;
            CurrentChoices.Add(CurrentQuestion.incorrectAnswers[i]);
        }

        for (int i = CurrentChoices.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            string temp = CurrentChoices[i];
            CurrentChoices[i] = CurrentChoices[j];
            CurrentChoices[j] = temp;
        }
    }

    void Evaluate(AnswerResult result)
    {
        if (result == AnswerResult.Correct)
        {
            CorrectCount++;
            AddCombustion(config.combustionPerCorrectAnswer);
            FeedbackText = config.feedbackCorrect;
        }
        else if (result == AnswerResult.Wrong)
        {
            WrongCount++;
            AddCombustion(config.combustionPerWrongAnswer);
            FeedbackText = config.feedbackWrong;
        }
        else
        {
            TimedOutCount++;
            AddCombustion(config.combustionPerTimeout);
            FeedbackText = config.feedbackTimeout;
        }

        State = SessionState.Feedback;
        stateTimer = config.feedbackDelay;
    }

    void ContinueAfterFeedback()
    {
        FeedbackText = "";

        if (Combustion >= config.maxCombustion)
        {
            StartMeltdown();
            return;
        }

        if (QuestionNumber >= TotalQuestions)
        {
            EndGame(true);
            return;
        }

        StartNextQuestion();
    }

    void StartMeltdown()
    {
        Combustion = config.maxCombustion;
        State = SessionState.Meltdown;
        meltdownStep = 0;
        stateTimer = 0f;
        AdvanceMeltdown();
    }

    void AdvanceMeltdown()
    {
        int flashStep = 2 + config.meltdownCountdown;

        if (meltdownStep == 0)
        {
            MeltdownText = config.meltdownFailureLine;
            stateTimer = config.meltdownMessageDuration;
        }
        else if (meltdownStep == 1)
        {
            MeltdownText = config.meltdownContainmentLine;
            stateTimer = config.meltdownMessageDuration;
        }
        else if (meltdownStep < flashStep)
        {
            int secondsLeft = config.meltdownCountdown - (meltdownStep - 2);
            MeltdownText = secondsLeft.ToString();
            stateTimer = config.meltdownCountdownStep;
        }
        else if (meltdownStep == flashStep)
        {
            MeltdownText = "";
            IsFlashing = true;
            stateTimer = config.meltdownFlashDuration;
        }
        else
        {
            IsFlashing = false;
            MeltdownText = "";
            EndGame(false);
            return;
        }

        meltdownStep++;
    }

    void EndGame(bool won)
    {
        if (won) State = SessionState.Victory;
        else State = SessionState.Defeat;

        FeedbackText = "";
        if (GameEnded != null) GameEnded();
    }

    void AddCombustion(float amount)
    {
        Combustion = Mathf.Clamp(Combustion + amount, 0f, config.maxCombustion);
    }
}

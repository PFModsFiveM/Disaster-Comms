using System;
using UnityEngine;

public enum Difficulty
{
    Easy,
    Hard
}

public enum ReactorStatus
{
    Stable,
    Warning,
    Critical,
    Meltdown
}

[Serializable]
public class GameConfig
{
    [Header("Question timing (seconds)")]
    public float easyQuestionTime = 20f;
    public float hardQuestionTime = 10f;
    public float feedbackDelay = 0.75f;

    [Header("Questions")]
    public int questionsPerGame = 10;
    public int easyChoiceCount = 5;

    [Header("Reactor combustion (percent)")]
    public float combustionPerCorrectAnswer = 0f;
    public float combustionPerWrongAnswer = 20f;
    public float combustionPerTimeout = 20f;
    public float maxCombustion = 100f;

    [Header("Reactor status thresholds (percent)")]
    public float warningThreshold = 40f;
    public float criticalThreshold = 70f;

    [Header("Meltdown sequence")]
    public int meltdownCountdown = 3;
    public float meltdownMessageDuration = 0.9f;
    public float meltdownCountdownStep = 1f;
    public float meltdownFlashDuration = 0.6f;

    [Header("Wording")]
    public string feedbackCorrect = "MESSAGE UNDERSTOOD";
    public string feedbackWrong = "COMMUNICATION ERROR";
    public string feedbackTimeout = "TRANSMISSION MISSED";
    public string meltdownFailureLine = "CRITICAL REACTOR FAILURE";
    public string meltdownContainmentLine = "CONTAINMENT FAILURE";

    public float GetQuestionTime(Difficulty difficulty)
    {
        if (difficulty == Difficulty.Hard) return hardQuestionTime;
        return easyQuestionTime;
    }

    public bool UsesMultipleChoice(Difficulty difficulty)
    {
        return difficulty == Difficulty.Easy;
    }

    public ReactorStatus GetStatus(float combustion)
    {
        if (combustion >= maxCombustion) return ReactorStatus.Meltdown;
        if (combustion >= criticalThreshold) return ReactorStatus.Critical;
        if (combustion >= warningThreshold) return ReactorStatus.Warning;
        return ReactorStatus.Stable;
    }
}

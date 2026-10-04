using System;

[Serializable]
public class QuestionData
{
    public string sentenceWithBlank;
    public string correctAnswer;
    public string[] incorrectAnswers = new string[0];
}

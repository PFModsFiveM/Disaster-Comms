using System;

[Serializable]
public class QuestionData
{
    public string sentenceWithBlank;
    public string correctAnswer;
    public string[] incorrectAnswers = new string[0];

    public QuestionData()
    {
    }

    public QuestionData(string sentenceWithBlank, string correctAnswer, params string[] incorrectAnswers)
    {
        this.sentenceWithBlank = sentenceWithBlank;
        this.correctAnswer = correctAnswer;
        this.incorrectAnswers = incorrectAnswers ?? new string[0];
    }
}

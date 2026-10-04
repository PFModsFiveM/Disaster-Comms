using System.Collections.Generic;
using UnityEngine;

public class QuestionDatabase : MonoBehaviour
{
    [Header("Transmissions")]
    public List<QuestionData> questions = new List<QuestionData>();

    [Header("Opening lines")]
    public List<string> radioIntros = new List<string>();
}

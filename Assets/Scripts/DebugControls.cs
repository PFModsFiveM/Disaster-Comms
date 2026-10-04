using UnityEngine;
using UnityEngine.InputSystem;

public class DebugControls : MonoBehaviour
{
    [SerializeField] GameManager manager;

    void Awake()
    {
        if (manager == null) manager = GetComponent<GameManager>();
        if (manager == null) manager = FindFirstObjectByType<GameManager>();
    }

    void Update()
    {
        if (manager == null) return;

        GameSession session = manager.Session;
        if (session == null) return;

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.f1Key.wasPressedThisFrame) session.DebugAddCombustion(manager.Config.combustionPerWrongAnswer);
        if (keyboard.f2Key.wasPressedThisFrame) session.DebugForceTimeout();
        if (keyboard.f3Key.wasPressedThisFrame) session.DebugSkipQuestion();
        if (keyboard.f4Key.wasPressedThisFrame) session.DebugForceMeltdown();
        if (keyboard.f5Key.wasPressedThisFrame) session.DebugForceVictory();
        if (keyboard.f6Key.wasPressedThisFrame) manager.PlayAgain();
    }
}

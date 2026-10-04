using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

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

        if (Pressed(DevKey.AddCombustion)) session.DebugAddCombustion(manager.Config.combustionPerWrongAnswer);
        if (Pressed(DevKey.Timeout)) session.DebugForceTimeout();
        if (Pressed(DevKey.NextQuestion)) session.DebugSkipQuestion();
        if (Pressed(DevKey.Meltdown)) session.DebugForceMeltdown();
        if (Pressed(DevKey.Victory)) session.DebugForceVictory();
        if (Pressed(DevKey.Restart)) manager.PlayAgain();
    }

    enum DevKey
    {
        AddCombustion,
        Timeout,
        NextQuestion,
        Meltdown,
        Victory,
        Restart
    }

    static bool Pressed(DevKey key)
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return false;

        switch (key)
        {
            case DevKey.AddCombustion: return keyboard.f1Key.wasPressedThisFrame;
            case DevKey.Timeout: return keyboard.f2Key.wasPressedThisFrame;
            case DevKey.NextQuestion: return keyboard.f3Key.wasPressedThisFrame;
            case DevKey.Meltdown: return keyboard.f4Key.wasPressedThisFrame;
            case DevKey.Victory: return keyboard.f5Key.wasPressedThisFrame;
            case DevKey.Restart: return keyboard.f6Key.wasPressedThisFrame;
            default: return false;
        }
#else
        switch (key)
        {
            case DevKey.AddCombustion: return Input.GetKeyDown(KeyCode.F1);
            case DevKey.Timeout: return Input.GetKeyDown(KeyCode.F2);
            case DevKey.NextQuestion: return Input.GetKeyDown(KeyCode.F3);
            case DevKey.Meltdown: return Input.GetKeyDown(KeyCode.F4);
            case DevKey.Victory: return Input.GetKeyDown(KeyCode.F5);
            case DevKey.Restart: return Input.GetKeyDown(KeyCode.F6);
            default: return false;
        }
#endif
    }
}

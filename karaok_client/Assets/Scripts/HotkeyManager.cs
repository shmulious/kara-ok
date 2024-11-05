using UnityEngine;
using UnityEngine.Events;
using System;
using System.Collections.Generic;

public class HotkeyManager : MonoBehaviour
{
    // Singleton instance
    public static HotkeyManager Instance { get; private set; }

    // Dictionary to hold hotkey mappings
    private Dictionary<KeyCode, (string description, UnityAction action)> hotkeyMappings;

    private void Awake()
    {
        // Enforce singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject); // Keep this instance across scenes if needed

        // Initialize hotkey mappings with descriptions, initially without actions
        hotkeyMappings = new Dictionary<KeyCode, (string, UnityAction)>
        {
            { KeyCode.A, ("Hotkey Command/Ctrl + Shift + A detected", null) },
            { KeyCode.D, ("Hotkey Command/Ctrl + Shift + D detected", null) },
            { KeyCode.E, ("Hotkey Command/Ctrl + Shift + E detected", null) }
        };
    }

    void Update()
    {
        // Check if Command/Control and Shift are held down
        bool isCommandOrControl = Input.GetKey(KeyCode.LeftCommand) || Input.GetKey(KeyCode.RightCommand) ||
                                  Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
        bool isShiftHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

        if (isCommandOrControl && isShiftHeld)
        {
            // Check if any of the defined hotkeys are pressed
            foreach (var hotkey in hotkeyMappings.Keys)
            {
                if (Input.GetKeyDown(hotkey))
                {
                    OnHotkeyPressed(hotkey);
                    break;
                }
            }
        }
    }

    private void OnHotkeyPressed(KeyCode key)
    {
        if (hotkeyMappings.TryGetValue(key, out var hotkeyAction) && hotkeyAction.action != null)
        {
            KaraokLogger.Log(hotkeyAction.description);
            hotkeyAction.action.Invoke();
        }
        else
        {
            KaraokLogger.Log("Unknown or unassigned hotkey detected");
        }
    }

    // Register an action to a hotkey, overriding any existing action
    public void RegisterHotkeyAction(KeyCode key, UnityAction action)
    {
        if (hotkeyMappings.ContainsKey(key))
        {
            // Override the existing action for the hotkey
            hotkeyMappings[key] = (hotkeyMappings[key].description, action);
        }
        else
        {
            // Optionally log or handle attempts to register actions to undefined hotkeys
            KaraokLogger.Log($"Attempted to register an action for an undefined hotkey: {key}");
        }
    }
}
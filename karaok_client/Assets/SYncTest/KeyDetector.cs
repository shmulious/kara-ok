using System;
using System.Collections.Generic;
using UnityEngine;

public class KeyDetector : MonoBehaviour
{
    private readonly Dictionary<KeyCode, Action> keyActions = new Dictionary<KeyCode, Action>();

    /// <summary>
    /// Subscribes an action to be invoked when the specified key is pressed.
    /// </summary>
    /// <param name="key">The key to listen for.</param>
    /// <param name="action">The action to invoke when the key is pressed.</param>
    public void Subscribe(KeyCode key, Action action)
    {
        if (!keyActions.ContainsKey(key))
        {
            keyActions[key] = null;
        }

        keyActions[key] += action;
    }

    /// <summary>
    /// Unsubscribes an action from the specified key.
    /// </summary>
    /// <param name="key">The key to stop listening for.</param>
    /// <param name="action">The action to remove.</param>
    public void Unsubscribe(KeyCode key, Action action)
    {
        if (keyActions.ContainsKey(key))
        {
            keyActions[key] -= action;

            // Remove the key if no actions are left
            if (keyActions[key] == null)
            {
                keyActions.Remove(key);
            }
        }
    }

    private void Update()
    {
        try
        {
            foreach (var keyAction in keyActions)
            {
                if (Input.GetKeyDown(keyAction.Key))
                {
                    keyAction.Value?.Invoke();
                }
            }
        }
        catch (Exception e)
        {
            
        }
    }

    public void Kill()
    {
        Destroy(gameObject);
    }
}
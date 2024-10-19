using System;
using UnityEngine;

public static class KaraokLogger
{
    public static void Log(string message, params object[] args)
    {
        Debug.Log(string.Format(message, args));
    }

    public static void LogError(string message, params object[] args)
    {
        Debug.LogError(string.Format(message, args));
    }

    public static void LogWarning(string message, params object[] args)
    {
        Debug.LogWarning(string.Format(message, args));
    }
}
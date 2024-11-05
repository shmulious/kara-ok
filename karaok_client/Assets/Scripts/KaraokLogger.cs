using System;
using UnityEngine;

public static class KaraokLogger
{
    public static void Log(string message)
    {
        Debug.Log(message);
    }

    public static void LogError(string message)
    {
        Debug.LogError(message);
    }

    public static void LogWarning(string message)
    {
        Debug.LogWarning(message);
    }
}
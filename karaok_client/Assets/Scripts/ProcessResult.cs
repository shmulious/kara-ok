using System;
using System.Collections.Generic;
using UnityEngine;

public class ProcessResult
{
    public string Output { get; set; }
    public string Error { get; set; }
    public int ExitCode { get; set; }
    public object Value { get; set; }

    public bool TryGetValues<T>(out List<T> possibleVals)
    {
            var vals = Output.ToLower().Split('\n');
            possibleVals = new List<T>();
            foreach (object s in vals)
            {
                try
                {
                    //(T)Convert.ChangeType(input, typeof(T));
                    var val = (T)Convert.ChangeType(s, typeof(T));
                    Debug.Log($"[ProcessResult] output: {s} is successfully casted into {typeof(T)}");
                    possibleVals.Add(val);
                }

                catch (Exception e)
                {
                    Debug.LogError($"[ProcessResult] - Can't cast value {s} to {typeof(T)}");
                }
            }

            return possibleVals.Count>0;
    }

    public bool Success => ExitCode == 0 && string.IsNullOrEmpty(Error);

    public ProcessResult()
    {
        
    }
    // Constructor to initialize the result object
    public ProcessResult(string output, string error, int exitCode)
    {
        Output = output;
        Error = error;
        ExitCode = exitCode;
    }
}
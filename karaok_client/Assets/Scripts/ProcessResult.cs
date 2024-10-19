using System;
using System.Collections.Generic;
using UnityEngine;

public class ProcessResult<T>
{
    public string Output { get; set; }
    public string Error { get; set; }
    public int ExitCode { get; set; }
    public T Value { get; set; }

    public bool Success => ExitCode == 0;

    public string StringVal { get; set; }

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
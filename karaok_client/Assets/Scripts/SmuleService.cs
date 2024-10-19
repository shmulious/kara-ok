using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

public enum ProcessType
{
    Convert, // We'll implement this for now, but you can add others like Install, Demo, etc.
}

public static class SmuleService
{
    private static PythonRunner _pythonRunner = new PythonRunner(); // Assuming PythonRunner is already defined elsewhere
    
    /// <summary>
    /// Process a song by running the specified process on smule.py.
    /// After a successful conversion, the current thumbnail is saved as a JPG file.
    /// </summary>
    /// <param name="metadata">The YouTube URL item to be processed.</param>
    /// <param name="outputFolderPath">The output folder path where the processed song will be saved.</param>
    /// <param name="modelNumber">The model number (between 1 and 4) to be used in the conversion process.</param>
    /// <returns>A Task representing the async operation.</returns>
    public static async Task<ProcessResult<string>> ProcessSong(SongMetadata metadata, string outputFolderPath, int modelNumber)
    {
        try
        {
            Debug.Log($"[SmuleService] - Processing song for URL: {metadata.URL}");

            // Ensure the output folder path exists
            if (!Directory.Exists(outputFolderPath))
            {
                Directory.CreateDirectory(outputFolderPath);
            }

            // Call the Python script with the convert action
            var res = await _pythonRunner.RunProcess<string>(
                "main/smule.py",
                string.Format("--convert \"{0}\" \"{1}\" {2}", metadata.Path, outputFolderPath, modelNumber)
            );

            Debug.Log($"[SmuleService] - Finished processing {metadata.URL}");
            Debug.Log($"[SmuleService] - Result: {res.Output ?? "No output returned."}");

            if (res.Success)
            {
                var artifacts_path = res.StringVal;
                Debug.Log($"[SmuleService] - Conversion successful.");
                Debug.Log($"[SmuleService] - outputs are placed under: {artifacts_path}");
                return new ProcessResult<string>() { ExitCode = 0, Value = artifacts_path };
            }
            else
            {
                Debug.LogError($"[SmuleService] - Error processing song: {res.Error}");
                return res;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SmuleService] - Exception occurred while processing song: {ex.Message}");
            return new ProcessResult<string>() { ExitCode = 1, Value = ex.Message, Error = ex.Message };
        }
    }
}
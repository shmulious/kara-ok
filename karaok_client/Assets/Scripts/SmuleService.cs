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
    /// <param name="item">The YouTube URL item to be processed.</param>
    /// <param name="outputFolderPath">The output folder path where the processed song will be saved.</param>
    /// <param name="modelNumber">The model number (between 1 and 4) to be used in the conversion process.</param>
    /// <returns>A Task representing the async operation.</returns>
    public static async Task<ProcessResult<string>> ProcessSong(SongMetadataData item, string outputFolderPath, int modelNumber)
    {
        try
        {
            Debug.Log($"[SmuleService] - Processing song for URL: {item.URL}");

            // Ensure the output folder path exists
            if (!Directory.Exists(outputFolderPath))
            {
                Directory.CreateDirectory(outputFolderPath);
            }

            // Ensure the output path is wrapped in quotes to handle spaces
            var outputPath = $"\"{outputFolderPath}\"";

            // Call the Python script with the convert action
            var res = await _pythonRunner.RunProcess<string>(
                "main/smule.py",
                $"--convert \"{item.URL}\" {outputPath} {modelNumber}"
            );

            Debug.Log($"[SmuleService] - Finished processing {item.URL}");
            Debug.Log($"[SmuleService] - Result: {res.Output ?? "No output returned."}");

            if (res.Success)
            {
                Debug.Log($"[SmuleService] - Conversion successful. Saving thumbnail...");

                // Construct the folder for saving the thumbnail based on song title
                string songTitle = item.Title;//.Replace(" ", "_").Replace("/", "_"); // Ensure valid file/folder name
                string songOutputFolder = Path.Combine(outputFolderPath, "outputs", songTitle);

                // Ensure the song output folder exists
                if (!Directory.Exists(songOutputFolder))
                {
                    Directory.CreateDirectory(songOutputFolder);
                }

                // Save the current thumbnail as a JPG file
                await SaveThumbnailAsJPG(item, songOutputFolder);
                return new ProcessResult<string>() { ExitCode = 0, Value = songOutputFolder };
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

    /// <summary>
    /// Save the current thumbnail from the YouTubeURLListItemView as a JPG file in the specified folder.
    /// </summary>
    /// <param name="item">The YouTubeURLListItemView containing the thumbnail data.</param>
    /// <param name="folderPath">The folder where the JPG file will be saved.</param>
    private static async Task SaveThumbnailAsJPG(SongMetadataData item, string folderPath)
    {
        if (item.ThumbnailData == null)
        {
            Debug.LogError("[SmuleService] - No thumbnails to save.");
            return;
        }

        try
        {
            // Get the current thumbnail data
            ThumbnailData currentThumbnail = item.ThumbnailData;
            string filePath = Path.Combine(folderPath, "thumbnail.jpg");

            Debug.Log($"[SmuleService] - Saving thumbnail to: {filePath}");

            // Convert the texture to a byte array and save it as a JPG file
            byte[] jpgBytes = currentThumbnail.Texture.EncodeToJPG();

            // Ensure the file is saved asynchronously
            await File.WriteAllBytesAsync(filePath, jpgBytes);

            Debug.Log($"[SmuleService] - Thumbnail saved successfully at {filePath}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SmuleService] - Failed to save thumbnail: {ex.Message}");
        }
    }
}
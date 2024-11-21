using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Video;

public class MediaLoader
{
    /// <summary>
    /// Loads an audio or video file from a given path (local or remote) and returns an AudioClip or VideoPlayer object.
    /// </summary>
    /// <param name="filePath">The full system path or URL to the media file.</param>
    /// <typeparam name="T">Expected type: AudioClip or VideoPlayer.</typeparam>
    /// <returns>A Task returning the loaded media (AudioClip or VideoPlayer).</returns>
    public static async Task<T> LoadMediaFromPath<T>(string filePath) where T : class
    {
        if (string.IsNullOrEmpty(filePath))
        {
            throw new ArgumentException("File path is null or empty.");
        }

        // Automatically add "file://" if it's a local file
        string fileUri = filePath.StartsWith("http://") || filePath.StartsWith("https://") || filePath.StartsWith("file://")
            ? filePath
            : $"file://{filePath}";

        Debug.Log($"Loading media from: {fileUri}");

        // Determine the file type
        Type targetType = typeof(T);

        if (targetType == typeof(AudioClip))
        {
            return await LoadAudioClip(fileUri) as T;
        }
        else if (targetType == typeof(VideoPlayer))
        {
            return LoadVideoPlayer(filePath) as T;
        }

        throw new NotSupportedException($"Unsupported media type: {targetType}");
    }

    /// <summary>
    /// Loads an AudioClip from a file URI.
    /// </summary>
    /// <param name="fileUri">The URI of the file to load.</param>
    /// <returns>A Task returning the loaded AudioClip.</returns>
    private static async Task<AudioClip> LoadAudioClip(string fileUri)
    {
        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(fileUri, DetectAudioType(fileUri)))
        {
            var operation = www.SendWebRequest();
            while (!operation.isDone)
            {
                await Task.Yield();
            }

            if (www.result != UnityWebRequest.Result.Success)
            {
                throw new Exception($"Error loading audio: {www.error}");
            }

            return DownloadHandlerAudioClip.GetContent(www);
        }
    }

    /// <summary>
    /// Creates a VideoPlayer configured to play the specified video file.
    /// </summary>
    /// <param name="filePath">The path to the video file.</param>
    /// <returns>The configured VideoPlayer object.</returns>
    private static VideoPlayer LoadVideoPlayer(string filePath)
    {
        var videoPlayer = new GameObject("VideoPlayer").AddComponent<VideoPlayer>();
        videoPlayer.url = filePath.StartsWith("file://") ? filePath.Substring(7) : filePath; // Remove "file://" for VideoPlayer
        videoPlayer.isLooping = false;
        videoPlayer.playOnAwake = false;

        Debug.Log($"VideoPlayer configured for: {filePath}");
        return videoPlayer;
    }

    /// <summary>
    /// Detects the audio type based on the file extension.
    /// </summary>
    /// <param name="fileUri">The URI of the audio file.</param>
    /// <returns>The AudioType corresponding to the file.</returns>
    private static AudioType DetectAudioType(string fileUri)
    {
        string extension = Path.GetExtension(fileUri).ToLower();
        return extension switch
        {
            ".wav" => AudioType.WAV,
            ".mp3" => AudioType.MPEG,
            ".ogg" => AudioType.OGGVORBIS,
            _ => throw new NotSupportedException($"Unsupported audio file type: {extension}")
        };
    }
}
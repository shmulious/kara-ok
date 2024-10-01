using UnityEngine.Networking;
using System.Threading.Tasks;
using UnityEngine;

public class LyricsOvhProvider : ILyricsProvider
{
    public async Task<string> GetLyricsAsync(string artist, string songTitle)
    {
        artist = artist == "Unknown Artist" ? string.Empty : artist;
        string url = artist == "Unknown Artist" ? $"https://api.lyrics.ovh/v1/{songTitle}" : $"https://api.lyrics.ovh/v1/{artist}/{songTitle}";

        using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
        {
            var asyncOp = webRequest.SendWebRequest();
            while (!asyncOp.isDone)
                await Task.Yield();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                string jsonResponse = webRequest.downloadHandler.text;
                // Parsing response JSON to get the "lyrics" field
                if (jsonResponse.Contains("lyrics"))
                {
                    return jsonResponse; // Replace this with actual parsing logic if needed.
                }
                return null;
            }
            else
            {
                Debug.LogError($"Error fetching lyrics from Lyrics.ovh: {webRequest.error}");
                return null;
            }
        }
    }
}
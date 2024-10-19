using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using HtmlAgilityPack;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Xml;

public class GeniusProvider : ILyricsProvider
{
    private readonly string _geniusAccessToken;

    public GeniusProvider(string geniusAccessToken)
    {
        _geniusAccessToken = geniusAccessToken;
    }

    public async Task<string> GetLyricsAsync(string artist, string songTitle)
    {
        // Step 1: Get the lyrics URL using the Genius API
        string lyricsUrl = await GetLyricsUrlAsync(artist, songTitle);

        if (!string.IsNullOrEmpty(lyricsUrl))
        {
            // Step 2: Scrape the lyrics from the Genius page
            string lyrics = await ScrapeLyricsFromGeniusPage(lyricsUrl);
            return lyrics;
        }

        Debug.LogError("Failed to get lyrics URL.");
        return null;
    }

    // Fetch the lyrics URL from the Genius API
    private async Task<string> GetLyricsUrlAsync(string artist, string songTitle)
    {
        artist = artist == "Unknown Artist" ? string.Empty : artist;
        string searchUrl = $"https://api.genius.com/search?q={UnityWebRequest.EscapeURL(artist + " " + songTitle)}&access_token={_geniusAccessToken}";

        using (UnityWebRequest webRequest = UnityWebRequest.Get(searchUrl))
        {
            var asyncOp = webRequest.SendWebRequest();
            while (!asyncOp.isDone)
                await Task.Yield();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                string jsonResponse = webRequest.downloadHandler.text;
                return ParseLyricsUrlFromGeniusResponse(jsonResponse);
            }
            else
            {
                Debug.LogError($"Error fetching lyrics URL from Genius: {webRequest.error}");
                return null;
            }
        }
    }

    // Parse the Genius API response to get the lyrics URL
    private string ParseLyricsUrlFromGeniusResponse(string jsonResponse)
    {
        try
        {
            JObject responseObject = JObject.Parse(jsonResponse);
            var firstHit = responseObject["response"]?["hits"]?.FirstOrDefault()?["result"];

            if (firstHit != null)
            {
                string lyricsUrl = firstHit["url"]?.ToString();
                if (!string.IsNullOrEmpty(lyricsUrl))
                {
                    return lyricsUrl;
                }
            }

            Debug.LogError("No lyrics URL found in Genius response.");
            return null;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error parsing Genius API response: {ex.Message}");
            return null;
        }
    }

    // Scrape the lyrics from the Genius page
    private async Task<string> ScrapeLyricsFromGeniusPage(string lyricsUrl)
    {
        using (UnityWebRequest webRequest = UnityWebRequest.Get(lyricsUrl))
        {
            var asyncOp = webRequest.SendWebRequest();
            while (!asyncOp.isDone)
                await Task.Yield();

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Error fetching lyrics page from Genius: {webRequest.error}");
                return null;
            }

            // Load the HTML into HtmlAgilityPack for parsing
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(webRequest.downloadHandler.text);

            // Find the lyrics element - Genius typically wraps lyrics in a <div> with the class "lyrics" or "Lyrics__Container"
            var lyricsDiv = htmlDoc.DocumentNode.SelectSingleNode("//div[contains(@class, 'Lyrics__Container')]");

            if (lyricsDiv != null)
            {
                // Extract the inner text (lyrics) and return it
                string lyrics = StringCleaner.ConvertHtmlToPlainText(lyricsDiv.InnerHtml);
                return lyrics.Trim();  // Return the cleaned lyrics
            }

            Debug.LogError("Failed to scrape lyrics from Genius page.");
            return null;
        }
    }
}
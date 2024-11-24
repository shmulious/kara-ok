﻿using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using HtmlAgilityPack;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Text;
using System.Web;
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
// Find elements with attributes like this: data-lyrics-container
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

            // Find all elements with the "data-lyrics-container" attribute
            var lyricsNodes = htmlDoc.DocumentNode.SelectNodes("//*[@data-lyrics-container]");

            if (lyricsNodes != null && lyricsNodes.Count > 0)
            {
                // Concatenate the inner texts of all matched elements
                StringBuilder lyricsBuilder = new StringBuilder();
                foreach (var node in lyricsNodes)
                {
                    lyricsBuilder.AppendLine(StringCleaner.ConvertHtmlToPlainText(node.InnerHtml));
                    lyricsBuilder.AppendLine("\n");
                }

                return HttpUtility.HtmlDecode(lyricsBuilder.ToString().Trim()); // Return the cleaned and concatenated lyrics
            }

            Debug.LogError("Failed to scrape lyrics from Genius page.");
            return null;
        }
    }
}
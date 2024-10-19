using System.Threading.Tasks;
using UnityEngine;

public class LyricsProviderHandler
{
    private readonly ILyricsProvider _primaryProvider;
    private readonly ILyricsProvider _secondaryProvider;

    public LyricsProviderHandler(ILyricsProvider primaryProvider, ILyricsProvider secondaryProvider = null)
    {
        _primaryProvider = primaryProvider;
        _secondaryProvider = secondaryProvider == null ? primaryProvider : secondaryProvider;
    }

    public async Task<string> GetLyricsAsync(string artist, string songTitle)
    {
        // Try the primary provider first
        string lyrics = await _primaryProvider.GetLyricsAsync(artist, songTitle);

        if (!string.IsNullOrEmpty(lyrics))
        {
            Debug.Log("Lyrics found with the primary provider.");
            Debug.Log($"{lyrics}");
            return lyrics;
        }

        if (_secondaryProvider != null)
        {
            // If primary fails, fall back to the secondary provider
            Debug.Log("Falling back to the secondary provider.");
            return await _secondaryProvider.GetLyricsAsync(artist, songTitle);
        }

        throw new System.Exception($"Failed to fetch lyrics for {artist} - {songTitle}");
    }
}
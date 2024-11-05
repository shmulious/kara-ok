using System.Collections.Generic;
using Newtonsoft.Json;

public class MediaUrlData
{
    [JsonProperty("artist")]
    public string ArtistName { get; set; }

    [JsonProperty("title")]
    public string Title { get; set; }

    [JsonProperty("mediaUrl")]
    public string MediaUrl { get; set; }

    public override string ToString()
    {
        return $"Artist: {ArtistName}, Title: {Title}, Media URL: {MediaUrl}";
    }
}

public class PlaylistData
{
    [JsonProperty("playlist")]
    public List<PlaylistItemData> Playlist { get; set; } = new List<PlaylistItemData>();
}

public class PlaylistItemData
{
    [JsonProperty("artist")]
    public string ArtistName { get; set; }

    [JsonProperty("title")]
    public string Title { get; set; }

    [JsonProperty("web_url")]
    public string WebUrl { get; set; }

    public override string ToString()
    {
        return $"Artist: {ArtistName}, Title: {Title}, Media URL: {WebUrl}";
    }
}
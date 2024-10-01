using UnityEngine;
using UnityEngine.Networking;
using System.Collections.Generic;
using System.Threading.Tasks;

/// <summary>
/// Represents the data for a downloaded thumbnail, including its texture, sprite, size, and URL.
/// </summary>
public class ThumbnailData
{
    public Vector2 OriginalSize { get; set; }
    public Texture2D Texture { get; set; }
    public Sprite ThumbnailSprite { get; set; }
    public string Url { get; set; }
}

public abstract class JsonValueFromProcess
{
}

public class YoutubeMetadata : JsonValueFromProcess
{
    public string artist;
    public string title;
    public List<string> thumbnails; // List of thumbnail URLs
}
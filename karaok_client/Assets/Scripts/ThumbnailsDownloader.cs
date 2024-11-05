using System.Collections.Generic;
using System.Threading.Tasks;
using DataClasses;
using KtxUnity;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class ThumbnailsDownloader
{
    /// <summary>
    /// Downloads a single image from a URL and stores it as a ThumbnailData object, including the sprite.
    /// Supports JPEG, PNG, and WebP formats.
    /// </summary>
    /// <param name="url">The URL of the image.</param>
    /// <returns>A ThumbnailData object if successful, or null if the download fails.</returns>
    public static async Task<ThumbnailData> DownloadThumbnail(string url)
    {
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            // Use DownloadHandlerBuffer to download the raw data
            request.downloadHandler = new DownloadHandlerBuffer();

            // Send the web request and wait for it to complete
            var operation = request.SendWebRequest();

            while (!operation.isDone)
                await Task.Yield(); // Await the task to avoid blocking

            // Check if there were any errors in the request
            if (request.result != UnityWebRequest.Result.Success)
            {
                KaraokLogger.LogError($"Failed to download image: {request.error}");
                return null;
            }

            // Downloaded raw image data
            byte[] imageData = request.downloadHandler.data;

            // Detect if the URL points to a WebP image by file extension or MIME type
            if (url.EndsWith(".webp", System.StringComparison.OrdinalIgnoreCase))
            {
                KaraokLogger.Log($"Detected WebP image at URL: {url}");

                // Decode WebP image using KtxUnity
                Texture2D webpTexture = await DecodeWebPAsync(imageData);

                if (webpTexture != null)
                {
                    KaraokLogger.Log($"Successfully decoded WebP image from URL: {url}");
                    Sprite sprite = SpriteFromTexture(webpTexture);

                    return new ThumbnailData
                    {
                        Texture = webpTexture,// Store the sprite
                        Url = url
                    };
                }
                else
                {
                    KaraokLogger.LogError("Failed to decode WebP image.");
                    return null;
                }
            }
            else
            {
                // If it's not WebP, try to create a Texture2D from the raw data
                Texture2D texture = new Texture2D(2, 2); // Temporary size; will resize when loaded
                if (texture.LoadImage(imageData))
                {
                    KaraokLogger.Log($"Successfully downloaded image from URL: {url}");
                    Sprite sprite = SpriteFromTexture(texture);

                    return new ThumbnailData
                    {
                        Texture = texture,
                        Url = url
                    };
                }

                KaraokLogger.LogError("Failed to create texture from downloaded image.");
                return null;
            }
        }
    }

    /// <summary>
    /// Decodes WebP data into a Texture2D using KtxUnity.
    /// </summary>
    /// <param name="data">The WebP image data as byte array.</param>
    /// <returns>The decoded Texture2D, or null if decoding fails.</returns>
    private static async Task<Texture2D> DecodeWebPAsync(byte[] data)
    {
        // Convert byte[] to NativeSlice<byte> as required by KtxUnity
        NativeArray<byte> nativeArray = new NativeArray<byte>(data, Allocator.Persistent);
        NativeSlice<byte> nativeSlice = new NativeSlice<byte>(nativeArray);

        // Load the texture using the KtxTexture class
        KtxTexture ktxTexture = new KtxTexture();
        var loadResult = await ktxTexture.LoadFromBytes(nativeSlice);

        // Dispose the NativeArray after usage to avoid memory leaks
        nativeArray.Dispose();

        // Check if the loading succeeded
        if (loadResult != null && loadResult.texture != null)
        {
            Texture2D texture = loadResult.texture as Texture2D;
            return texture;
        }

        KaraokLogger.LogError("Failed to decode the WebP image.");
        return null;
    }

    /// <summary>
    /// Converts a Texture2D to a Sprite.
    /// </summary>
    /// <param name="texture">The Texture2D to convert.</param>
    /// <returns>A Sprite created from the texture.</returns>
    public static Sprite SpriteFromTexture(Texture2D texture)
    {
        return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
    }
}


using System;
using UnityEngine;

public static class Texture2DSerializationHelper
{
    // Serialize Texture2D to a Base64 string
    public static string Texture2DToBase64(Texture2D texture)
    {
        byte[] textureBytes = texture.EncodeToPNG(); // Convert Texture2D to byte array (PNG)
        return Convert.ToBase64String(textureBytes);  // Convert byte array to Base64 string
    }

    // Deserialize Base64 string to Texture2D
    public static Texture2D Base64ToTexture2D(string base64)
    {
        byte[] textureBytes = Convert.FromBase64String(base64); // Convert Base64 string back to byte array
        Texture2D texture = new Texture2D(2, 2); // Create new Texture2D (size will be overwritten by LoadImage)
        texture.LoadImage(textureBytes);          // Load the image data into the Texture2D
        return texture;
    }
}
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine.UI;

public class LoadingAnimationManager : MonoBehaviour
{
    public Sprite loadingImage; // Assign this in the Inspector (your loading icon prefab)
    public float rotationSpeed = 100f; // Speed of rotation for the loading animation

    private Dictionary<Transform, GameObject> activeLoadingIcons = new Dictionary<Transform, GameObject>(); // Track active animations

    // Method to start the loading animation under a specific parent
    public async void StartLoadingScreen(Transform parent, Func<bool> stopCondition, Color? backgroundColor, Action onAnimationEnd = null)
    {
        if (activeLoadingIcons.ContainsKey(parent))
        {
            Debug.LogWarning("A loading animation is already running for this parent.");
            return;
        }
        // Instantiate the loading icon under the given parent transform
        GameObject backgroundColorGO = new GameObject("loadingAnimationBackgroundColor");
        Image backgroundColorImage = backgroundColorGO.AddComponent<Image>();
        backgroundColorImage.color = backgroundColor.HasValue ? backgroundColor.Value : new Color(0,0,0,0);
        // Instantiate the loading icon under the given parent transform
        GameObject loadingIconInstance = new GameObject("loadingAnimation");
        Image loadingImageComponent = loadingIconInstance.AddComponent<Image>();
        loadingImageComponent.sprite = loadingImage;

        // Set parent
        loadingIconInstance.transform.SetParent(backgroundColorGO.transform);
        backgroundColorGO.transform.SetParent(parent);

        // Get RectTransform and stretch it to fill the parent
        foreach (var rectTransform in backgroundColorGO.GetComponentsInChildren<RectTransform>())
        {
            SetSizeAndPosition(rectTransform);
        }

        activeLoadingIcons[parent] = backgroundColorGO;

        // Start the rotation and stop condition checking asynchronously
        await AnimateLoading(loadingIconInstance, stopCondition);

        // Invoke the callback before destroying the animation
        onAnimationEnd?.Invoke();

        // Destroy the loading icon and remove it from the active dictionary
        Destroy(backgroundColorGO);
        activeLoadingIcons.Remove(parent);
    }

    private void SetSizeAndPosition(RectTransform rectTransform)
    {
        rectTransform.anchorMin = new Vector2(0, 0);  // Bottom-left corner
        rectTransform.anchorMax = new Vector2(1, 1);  // Top-right corner
        rectTransform.offsetMin = Vector2.zero;       // No offset from bottom-left
        rectTransform.offsetMax = Vector2.zero;       // No offset from top-right
        rectTransform.pivot = new Vector2(0.5f, 0.5f); // Centered pivot
        rectTransform.localScale = Vector3.one;
    }

    // Task to handle the loading animation and stop condition check
    private async Task AnimateLoading(GameObject loadingIconInstance, Func<bool> stopCondition)
    {
        while (!stopCondition.Invoke()) // Continue rotating while condition is not met
        {
            // Rotate the loading icon
            loadingIconInstance.transform.Rotate(Vector3.forward, rotationSpeed * Time.deltaTime);
            await Task.Yield(); // Yield to the next frame
        }
    }

    // Method to check if a loading animation is active on a specific parent
    public bool IsLoadingActive(Transform parent)
    {
        return activeLoadingIcons.ContainsKey(parent);
    }

    // Method to stop a loading animation manually on a specific parent
    public void StopLoadingScreen(Transform parent)
    {
        if (activeLoadingIcons.ContainsKey(parent))
        {
            Destroy(activeLoadingIcons[parent]);
            activeLoadingIcons.Remove(parent);
        }
    }
}
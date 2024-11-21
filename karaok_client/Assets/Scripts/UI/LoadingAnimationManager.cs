using UnityEngine;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class LoadingAnimationManager : MonoBehaviour
{
    [SerializeField] private Sprite _loadingImage; // Assign this in the Inspector (your loading icon prefab)
    [SerializeField] private Sprite _failureImage; // Assign this in the Inspector (your loading icon prefab)
    [SerializeField] private Sprite _successImage; // Assign this in the Inspector (your loading icon prefab)
    [FormerlySerializedAs("rotationSpeed")] [SerializeField] private  float _rotationSpeed = 100f; // Speed of rotation for the loading animation

    private readonly Dictionary<Transform, GameObject> _activeLoadingIcons = new Dictionary<Transform, GameObject>(); // Track active animations

    // Method to start the loading animation under a specific parent
    public async void StartLoadingScreen(Transform parent, Color? backgroundColor, Action onAnimationEnd = null)
    {
        if (_activeLoadingIcons.ContainsKey(parent))
        {
            Debug.LogWarning("A loading animation is already running for this parent.");
            return;
        }
        var loadingIconInstance = CreateStatusImage(parent, backgroundColor, ImageType.Loading);

        // Start the rotation and stop condition checking asynchronously
        await AnimateLoading(loadingIconInstance);

        // Invoke the callback before destroying the animation
        onAnimationEnd?.Invoke();

        // Destroy the loading icon and remove it from the active dictionary
        Destroy(loadingIconInstance.transform.parent.gameObject);
        _activeLoadingIcons.Remove(parent);
    }

    enum ImageType
    {
        Loading,
        Success,
        Failure
    }
    private GameObject CreateStatusImage(Transform parent, Color? backgroundColor, ImageType imageType)
    {
        // Instantiate the loading icon under the given parent transform
        GameObject backgroundColorGo = new GameObject("statusImageBackgroundColor");
        Image backgroundColorImage = backgroundColorGo.AddComponent<Image>();
        backgroundColorImage.color = backgroundColor.HasValue ? backgroundColor.Value : new Color(0,0,0,0);
        // Instantiate the loading icon under the given parent transform
        var statusImageGo = new GameObject("statusImage");
        Image statusImage = statusImageGo.AddComponent<Image>();

        switch (imageType)
        {
            case ImageType.Loading:
                statusImage.sprite = _loadingImage;
                break;
            case ImageType.Success:
                statusImage.sprite = _successImage;
                break;
            case ImageType.Failure:
                statusImage.sprite = _failureImage;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(imageType), imageType, null);
        }
        
        // Set parent
        statusImageGo.transform.SetParent(backgroundColorGo.transform);
        backgroundColorGo.transform.SetParent(parent);

        // Get RectTransform and stretch it to fill the parent
        foreach (var rectTransform in backgroundColorGo.GetComponentsInChildren<RectTransform>())
        {
            SetSizeAndPosition(rectTransform);
        }

        _activeLoadingIcons[parent] = backgroundColorGo;
        return backgroundColorGo;
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
    private async Task AnimateLoading(GameObject loadingIconInstance)
    {
        while (loadingIconInstance != null) // Continue rotating while condition is not met
        {
            // Rotate the loading icon
            loadingIconInstance.transform.Rotate(Vector3.forward, _rotationSpeed * Time.deltaTime);
            await Task.Yield(); // Yield to the next frame
        }
    }

    // Method to check if a loading animation is active on a specific parent
    public bool IsLoadingActive(Transform parent)
    {
        return _activeLoadingIcons.ContainsKey(parent);
    }

    // Method to stop a loading animation manually on a specific parent
    public void StopLoadingScreen(Transform parent, bool? isSuccess = null)
    {
        if (_activeLoadingIcons.ContainsKey(parent))
        {
            Destroy(_activeLoadingIcons[parent]);
            _activeLoadingIcons.Remove(parent);
            if (isSuccess.HasValue)
            {
                ShowResultIndicator(parent, isSuccess.Value);
            }
        }
    }

    private void ShowResultIndicator(Transform parent, bool isSuccess)
    {
        CreateStatusImage(parent, null, isSuccess? ImageType.Success:ImageType.Failure);
    }
}
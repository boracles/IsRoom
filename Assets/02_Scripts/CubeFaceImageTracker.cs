using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class CubeFaceImageTracker : MonoBehaviour
{
    private ARTrackedImageManager imageManager;

    private readonly Dictionary<string, TrackingState> faceStates = new();
    private readonly Dictionary<string, float> faceLastSeenTime = new();

    private float logTimer = 0f;
    private const float logInterval = 0.5f;

    private void Awake()
    {
        imageManager = GetComponent<ARTrackedImageManager>();
    }

    private void OnEnable()
    {
        if (imageManager != null)
            imageManager.trackablesChanged.AddListener(OnTrackablesChanged);
    }

    private void OnDisable()
    {
        if (imageManager != null)
            imageManager.trackablesChanged.RemoveListener(OnTrackablesChanged);
    }

    private void Update()
    {
        logTimer += Time.deltaTime;

        if (logTimer >= logInterval)
        {
            logTimer = 0f;
            PrintCurrentTrackingState();
        }
    }

    private void OnTrackablesChanged(ARTrackablesChangedEventArgs<ARTrackedImage> args)
    {
        foreach (var image in args.added)
            UpdateFaceState(image);

        foreach (var image in args.updated)
            UpdateFaceState(image);

        foreach (var removed in args.removed)
        {
            string name = removed.Value.referenceImage.name;
            faceStates.Remove(name);
            faceLastSeenTime.Remove(name);
        }
    }

    private void UpdateFaceState(ARTrackedImage image)
    {
        string name = image.referenceImage.name;

        faceStates[name] = image.trackingState;

        if (image.trackingState == TrackingState.Tracking)
            faceLastSeenTime[name] = Time.time;
    }

    private void PrintCurrentTrackingState()
    {
        List<string> activelyTracking = new();

        foreach (var pair in faceStates)
        {
            if (pair.Value == TrackingState.Tracking)
                activelyTracking.Add(pair.Key);
        }

        Debug.Log(
            "Currently Tracking " +
            activelyTracking.Count +
            " face(s): " +
            string.Join(", ", activelyTracking)
        );
    }
}
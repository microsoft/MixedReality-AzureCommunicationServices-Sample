// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using UnityEngine;

/// <summary>
/// This class represents an item inside a video device UI list.
/// </summary>
public class VideoDeviceRepeaterItem : RepeaterItem
{
    private VideoDeviceControls videoDeviceControls = null;
    private string displayName = string.Empty;
    private bool capturing = false;
    private ConcurrentQueue<Action> updateActions = new ConcurrentQueue<Action>();

    [SerializeField]
    [Tooltip("Event fired when display name changes.")]
    private StringChangeEvent displayNameChanged = new StringChangeEvent();

    [SerializeField]
    [Tooltip("Event fired when capturing state chages.")]
    private BooleanChangedEvent capturingChanged = new BooleanChangedEvent();


    public string DisplayName
    {
        get => displayName;

        private set
        {
            if (displayName != value)
            {
                displayName = value;
                updateActions.Enqueue(() => displayNameChanged?.Invoke(displayName));
            }
        }
    }

    public bool Capturing
    {
        get => capturing;

        private set
        {
            if (capturing != value)
            {
                capturing = value;
                updateActions.Enqueue(() => capturingChanged?.Invoke(capturing));
            }
        }
    }

    private void Start()
    {
        displayNameChanged?.Invoke(displayName);
        capturingChanged?.Invoke(capturing);
    }

    private void Update()
    {
        while (updateActions.TryDequeue(out var action))
        {
            action();
        }
    }

    private void OnDestroy()
    {
        SetVideoDeviceControls(null);
    }

    protected override void OnDataSourceChanged(object oldValue, object newValue)  
    {
        Debug.Log("OnDataSourceChanged");
        SetVideoDeviceControls(newValue as VideoDeviceControls);
    }

    private void SetVideoDeviceControls(VideoDeviceControls newValue)
    { 
        Debug.Log("SetVideoDeviceControls");
        if (videoDeviceControls != null)
        {
            videoDeviceControls.CapturingChanged -= OnCapturingChanged;
        }

        videoDeviceControls = newValue;

        if (videoDeviceControls == null)
        {
            Debug.Log("SetVideoDeviceControls 1: " + DisplayName);
            DisplayName = string.Empty;
            Capturing = false;
        }
        else
        {
            Debug.Log("SetVideoDeviceControls 2");
            DisplayName = videoDeviceControls.DisplayName;
            Capturing = videoDeviceControls.Capturing;
            videoDeviceControls.CapturingChanged += OnCapturingChanged;
        }
    }

    private void OnCapturingChanged(object sender, bool capturing)
    {
        Capturing = capturing;
    }

    public void StartCapture()
    {
        videoDeviceControls?.StartCapture();
    }

    public void StopCapture()
    {
        videoDeviceControls?.StopCapture();
    }
}

// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Communication.Calling.UnityClient;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Tasks;
using UnityEditor;

public delegate Task<bool> VideoDeviceControlsAsyncAction(OutgoingVideoStream outgoingVideoStream);

/// <summary>
/// This class hosts VideoDeviceInfo, and manages UI requests to turn on or off the capturing
/// from this video device.
/// </summary>
public class VideoDeviceControls : IDisposable
{
    private VideoDeviceControlsAsyncAction startCaptureCallback;
    private VideoDeviceControlsAsyncAction stopCaptureCallback;
    private bool capturing = false;

    public string DisplayName { get; }

    public OutgoingVideoStream OutgoingVideoStream { get; }

    public bool Capturing
    {
        get => capturing;

        private set
        {
            if (capturing != value)
            {
                capturing = value;
                CapturingChanged?.Invoke(this, value);
            }
        }
    }

    public event Action<object, bool> CapturingChanged;

    public VideoDeviceControls(
        string displayName,
        OutgoingVideoStream outgoingVideoStream,
        VideoDeviceControlsAsyncAction startCaptureCallback, 
        VideoDeviceControlsAsyncAction stopCaptureCallback)
    {
        this.DisplayName = displayName;
        this.OutgoingVideoStream = outgoingVideoStream;
        this.startCaptureCallback = startCaptureCallback;
        this.stopCaptureCallback = stopCaptureCallback;

        if (OutgoingVideoStream is VirtualOutgoingVideoStream)
        {
            ((VirtualOutgoingVideoStream)OutgoingVideoStream).StateChanged += VideoDeviceControls_OnOutgoingVideoStreamStateChanged;
        }
    }

    public void Dispose()
    {
        if (OutgoingVideoStream is VirtualOutgoingVideoStream)
        {
            ((VirtualOutgoingVideoStream)OutgoingVideoStream).StateChanged -= VideoDeviceControls_OnOutgoingVideoStreamStateChanged;
        }
    }

    private void VideoDeviceControls_OnOutgoingVideoStreamStateChanged(object sender, VideoStreamStateChangedEventArgs args)
    {
        var outgoingVideoStream = args.Stream as OutgoingVideoStream;
        if (outgoingVideoStream == null)
        {
            UnityEngine.Debug.LogError("No out going video stream givenin in VideoDeviceControls.");
            Capturing = false;
        }

        switch (outgoingVideoStream.State)
        {
            case VideoStreamState.Available:
                break;
            case VideoStreamState.Started:
                Capturing = true;
                break;
            case VideoStreamState.Stopping:
                break;
            case VideoStreamState.Stopped:
                Capturing = false;
                break;
            case VideoStreamState.NotAvailable:
                Capturing = false;
                break;
        }
    }

    public async void StartCapture()
    {
        if (startCaptureCallback != null && OutgoingVideoStream != null)
        {
            Capturing = await startCaptureCallback(OutgoingVideoStream);
        }
    }

    public async void StopCapture()
    {
        if (stopCaptureCallback != null && OutgoingVideoStream != null)
        {
            Capturing = !(await stopCaptureCallback(OutgoingVideoStream));
        }
    }
}

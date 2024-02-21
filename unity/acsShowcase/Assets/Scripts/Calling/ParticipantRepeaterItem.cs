// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Communication.Calling.UnityClient;
using UnityEngine;

/// <summary>
/// This class represents an item inside a participant UI list.
/// </summary>
public class ParticipantRepeaterItem : RepeaterItem
{
    private RemoteParticipant remoteParticipant = null;

    public RemoteParticipant RemoteParticipant
    {
        get => remoteParticipant;
    }

    private string displayName = string.Empty;

    [SerializeField]
    [Tooltip("The video player for this participant")]
    private VideoStreamPlayer videoStreamPlayer = null;

    [SerializeField]
    [Tooltip("Event fired when display name changes.")]
    private StringChangeEvent displayNameChanged = new StringChangeEvent();


    public VideoStreamPlayer VideoPlayer
    {
        get => videoStreamPlayer;
    }
    public string DisplayName
    {
        get => displayName;

        set
        {
            if (displayName != value)
            {
                displayName = value;
                displayNameChanged?.Invoke(displayName);
            }
        }
    }
    
    public CallIdentifier GetIdentifier()
    {
        return remoteParticipant?.Identifier;
    }


    private void Start()
    {
    }

    private void OnDestroy()
    {
        UnregisterParticipantEventHandlers();

        if (videoStreamPlayer != null)
        {
            videoStreamPlayer.Stream = null;
        }
    }

    protected override void OnDataSourceChanged(object oldValue, object newValue)
    {
        Debug.Log("ParticipantRepeaterItem  OnDataSourceChanged" );
        UnregisterParticipantEventHandlers();
        remoteParticipant = newValue as RemoteParticipant;
        DisplayName = remoteParticipant?.DisplayName;
        Debug.Log("ParticipantRepeaterItem  OnDataSourceChanged " + DisplayName );
        RegisterParticipantEventHandlers();
    }

    private void RegisterParticipantEventHandlers()
    {
        if (remoteParticipant == null)
        {
            return;
        }
        // BUGBUG: OnVideoStreamStateChanged is not fired when participant has an active stream before joining meeting.
        remoteParticipant.VideoStreamStateChanged += OnVideoStreamStateChanged;
    }

    private void UnregisterParticipantEventHandlers()
    {
        if (remoteParticipant == null)
        {
            return;
        }

        remoteParticipant.VideoStreamStateChanged -= OnVideoStreamStateChanged;
    }

    private void OnVideoStreamStateChanged(object sender, VideoStreamStateChangedEventArgs args)
    {
        if (videoStreamPlayer == null)
        {
            return;
        }

        switch (args.Stream.State)
        {
            case VideoStreamState.Available:
                // if the current stream is screen sharing, ignore other kind 
                if (videoStreamPlayer.Stream == null ||
                    videoStreamPlayer.Stream.SourceKind != VideoStreamSourceKind.ScreenSharing)
                {
                    videoStreamPlayer.Stream = args.Stream;
                }
                break;
            case VideoStreamState.Started:
                break;
            case VideoStreamState.Stopping:
                break;
            case VideoStreamState.Stopped:
                break;
            case VideoStreamState.NotAvailable:
                videoStreamPlayer.Stream = null;
                break;
        }
    }
}

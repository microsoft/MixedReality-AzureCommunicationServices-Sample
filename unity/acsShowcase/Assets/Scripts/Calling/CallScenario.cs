// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Communication.Calling.Unity;
using Azure.Communication.Calling.UnityClient;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;


/// <summary>
/// The event arguments used on a call scenario state change.
/// </summary>
[Serializable]
public struct CallScenarioStateChangedEventArgs
{
    public CallScenarioStateChangedEventArgs(CallScenario scenario, CommonCommunicationCall call)
    {
        Scenario = scenario;
        Call = call;
    }

    public CallScenario Scenario { get; private set; }

    public CommonCommunicationCall Call { get; private set; }

    public CallState State => Call?.State ?? CallState.None;
}

/// <summary>
/// Event raised when the call state changes.
/// </summary>
[Serializable]
public class CallScenarioStateChangedEvent : UnityEvent<CallScenarioStateChangedEventArgs>
{
}


/// <summary>
/// This base class manages call status, microphone audio, speaker audio, video devices, and a 
/// participant list.
/// </summary>
public abstract class CallScenario : MonoBehaviour
{
    private List<OutgoingVideoStream> outgoingVideoStreamsAtStart = new List<OutgoingVideoStream>();

    private VirtualOutgoingVideoStream rawOutgoingVideoStream = null;
    private RawOutgoingVideoStreamOptions rawOutgoingVideoStreamOptions = null;
    private OutputVideoResolution rawOutgoingVideoStreamResolution = OutputVideoResolution.Unknown;
    private VideoStreamPixelFormat rawOutgoingVideoStreamFormat = VideoStreamPixelFormat.Rgba;

    private bool rawOutgoingAudioRequested = false;
    private RawOutgoingAudioStream rawOutgoingAudioStream = null;
    private RawOutgoingAudioStreamOptions rawOutgoingAudioOptions = null;

    private bool rawIncomingAudioRequested = false;
    private RawIncomingAudioStream rawIncomingAudioStream = null;
    private RawIncomingAudioStreamOptions rawIncomingAudioOptions = null;

    private SingleAsyncRunner singleAsyncRunner = null;
    private DeviceManager currentDeviceManager = null;
    private CommonCallAgent currentCallAgent = null;
    private CommonCommunicationCall currentCall = null;

    private bool updateStatus = false;
    private bool updateRemoteParticipants = false;
    private bool updateMuteStatus = false;
    private bool updateSpeakerMuteStatus = false;
    private bool updateVideoDeviceControls = false;
    private bool updateListeningStatus = false;
    private bool updateOutgoingAudioStatus = false;
    private bool updateIncomingAudioStatus = false;
    private bool updateVideoCaptureStartedOrEnded = false;
    private int defaultCamIndex = 0;
    const string defaultCameraName = "QC Back Camera";

    private List<VideoDeviceControls> videoDeviceControlList = new List<VideoDeviceControls>();
    protected bool isSharedCamera = true;

    const string labelRaw = "Raw";
    const string labelStandard = "Std";
    protected ConcurrentQueue<Action> pendingActions = new ConcurrentQueue<Action>();

    [Header("Audio Settings")] [SerializeField] [Tooltip("The custom audio source for generating raw outgoing audio.")]
    private CustomAudioSource customAudioSource = null;

    [SerializeField] [Tooltip("The custom audio receiver for processing raw incoming audio.")]
    private CustomAudioReceiver customAudioReceiver = null;

    [SerializeField] [Tooltip("The sample rate of the raw outgoing audio.")]
    private AudioStreamSampleRate customAudioSampleRate = AudioStreamSampleRate.Hz_48000;

    [SerializeField] [Tooltip("The channel mode of the raw outgoing audio.")]
    private AudioStreamChannelMode customAudioChannelMode = AudioStreamChannelMode.Stereo;

    [SerializeField] [Tooltip("The audio format of the raw outgoing audio.")]
    private AudioStreamFormat customAudioFormat = AudioStreamFormat.Pcm16Bit;

    [SerializeField] [Tooltip("The amount of data per block for the raw outgoing audio.")]
    private AudioStreamBufferDuration customAudioTimePerBlock = AudioStreamBufferDuration.Ms20;

    [Header("Video Settings")] [SerializeField] [Tooltip("A video source that captures frames from the Unity scene.")]
    private CustomVideoSource customVideoSource = null;

    [SerializeField] [Tooltip("A video resolution to capture the Unity scene at.")]
    private OutputVideoResolution customVideoResolution = OutputVideoResolution.Resolution_1920x1080;

    [SerializeField] [Tooltip("A video format to capture the Unity scene at.")]
    private VideoStreamPixelFormat customVideoFormat = VideoStreamPixelFormat.Rgba;

    [SerializeField] [Tooltip("Event fired when speaker mute status changes.")]
    private StringChangeEvent speakerMuteStatusChanged = new StringChangeEvent();

    [SerializeField] [Tooltip("Event fired when the active type of outgoing audio changes. You can allow change audio type while there isn't an active call.")]
    private StringChangeEvent outgoingAudioStatus = new StringChangeEvent();

    [SerializeField] [Tooltip("Event fired when the requested type of outgoing audio changes. You can allow change audio type while there isn't an active call.")]
    private StringChangeEvent requestedOutgoingAudioStatus = new StringChangeEvent();

    [SerializeField] [Tooltip("Event fired when the active type of outgoing audio changes. You can allow change audio type while there isn't an active call.")]
    private StringChangeEvent incomingAudioStatus = new StringChangeEvent();

    [SerializeField] [Tooltip("Event fired when the requested type of outgoing audio changes. You can allow change audio type while there isn't an active call.")]
    private StringChangeEvent requestedIncomingAudioStatus = new StringChangeEvent();

    [SerializeField] [Tooltip("Event fired when the active type of outgoing and incoming audio changes. You can allow change audio type while there isn't an active call.")]
    private StringChangeEvent outgoingAndIncomingAudioStatus = new StringChangeEvent();

    [SerializeField] [Tooltip("Event fired when the active type of outgoing and incoming audio changes. You can allow change audio type while there isn't an active call.")]
    private StringChangeEvent requestedOutgoingAndIncomingAudioStatus = new StringChangeEvent();

    [SerializeField] [Tooltip("Event fired when the listening status changed.")]
    private StringChangeEvent listeningStatusChanged = new StringChangeEvent();

    [SerializeField] [Tooltip("Event fired when remote participants changed.")]
    private ObjectChangeEvent remoteParticipantsChanged = new ObjectChangeEvent();

    [SerializeField] [Tooltip("Event fired when output video devices changed.")]
    private ObjectChangeEvent videoDeviceControlsChanged = new ObjectChangeEvent();


    [Header("Base Events")]
    [SerializeField]
    [Tooltip("Event fired when status changes.")]
    private CallScenarioStateChangedEvent statusChanged = new CallScenarioStateChangedEvent();

    /// <summary>
    /// Event fire when status changes.
    /// </summary>
    public CallScenarioStateChangedEvent StatusChanged => statusChanged;


    [SerializeField]
    [Tooltip("Event fired when mic audio is muted.")]
    private UnityEvent muted = new UnityEvent();

    /// <summary>
    /// Event fired when mic audio is unmuted
    /// </summary>
    public UnityEvent Muted => muted;

    [SerializeField]
    [Tooltip("Event fired when mic audio is unmuted.")]
    private UnityEvent unmuted = new UnityEvent();

    /// <summary>
    /// Event fired when mic audio is unmuted
    /// </summary>
    public UnityEvent Unmuted => unmuted;

    [SerializeField]
    [Tooltip("Event fired when video is being captured.")] 
    private UnityEvent videoCaptureStarted = new UnityEvent();

    /// <summary>
    /// Event fired when video is being captured.
    /// </summary>
    public UnityEvent VideoCaptureStarted => videoCaptureStarted;

    [SerializeField]
    [Tooltip("Event fired when video is no longer being captured.")]
    private UnityEvent videoCaptureEnded = new UnityEvent();

    /// <summary>
    /// Event fired when video is no longer being captured.
    /// </summary>
    public UnityEvent VideoCaptureEnded => videoCaptureEnded;

    protected SingleAsyncRunner SingleAsyncRunner
    {
        get
        {
            if (singleAsyncRunner == null)
            {
                singleAsyncRunner = gameObject.EnsureComponent<SingleAsyncRunner>();
            }

            return singleAsyncRunner;
        }
    }

    public CommonCallAgent CallAgent
    {
        get => currentCallAgent;

        set
        {
            if (currentCallAgent != value)
            {
                if (currentCallAgent != null)
                {
                    AddCallAgentEventHandlers(currentCallAgent);
                }

                currentCallAgent = value;

                if (currentCallAgent != null)
                {
                    RemoveCallAgentEventHandlers(currentCallAgent);
                }

                InvalidateListeningStatus();
            }
        }
    }

    protected bool MuteAtStart { get; private set; } = false;

    protected bool MuteSpeakerAtStart { get; private set; } = false;

    protected IReadOnlyList<OutgoingVideoStream> OutgoingVideoStreamsAtStart => outgoingVideoStreamsAtStart.AsReadOnly();

    protected CommonCommunicationCall CurrentCall
    {
        get => currentCall;
        set
        {
            if (currentCall != value)
            {
                if (currentCall != null)
                {
                    currentCall.StateChanged -= OnCurrentCallStateChanged;
                    currentCall.OutgoingAudioStateChanged -= OnCurrentCallMutedChanged;
                    currentCall.RemoteParticipantsUpdated -= OnCurrentCallParticipantsUpdated;
                }

                currentCall = value;

                if (currentCall != null)
                {
                    currentCall.StateChanged += OnCurrentCallStateChanged;
                    currentCall.OutgoingAudioStateChanged += OnCurrentCallMutedChanged;
                    currentCall.RemoteParticipantsUpdated += OnCurrentCallParticipantsUpdated;
                }
                else
                {
                    ReleaseRawOutgoingAudioStream();
                    ReleaseRawIncomingAudioStream();
                }

                InvalidateStatus();
                InvalidateMuteStatus();
                InvalidateSpeakerMuteStatus();
                InvalidateRemoteParticipants();
                InvalidateOutgoingAudioStatus();
                InvalidateIncomingAudioStatus();
                InvalidateVideoCaptureStartedOrEnded();
            }
        }
    }

    protected DeviceManager CurrentDeviceManager
    {
        get => currentDeviceManager;

        private set
        {
            if (currentDeviceManager != value)
            {
                if (currentDeviceManager != null)
                {
                    currentDeviceManager.CamerasUpdated -= OnDeviceManagerCamerasUpdated;
                }

                currentDeviceManager = value;

                if (currentDeviceManager != null)
                {
                    currentDeviceManager.CamerasUpdated += OnDeviceManagerCamerasUpdated;
                }

                InvalidateVideoDeviceControls();
            }
        }
    }

    private void Start()
    {
        if (customVideoSource != null)
        {
            customVideoSource.OnMediaSampleReady += OnSceneVideoFrameReady;
        }

        if (customAudioSource != null)
        {
            customAudioSource.OnMediaSamplesReady += OnCustomAudioSamplesReady;
        }

        InvalidateStatus();
        InvalidateMuteStatus();
        InvalidateSpeakerMuteStatus();
        InvalidateListeningStatus();
        InvalidateOutgoingAudioStatus();
        InvalidateIncomingAudioStatus();
        InvalidateVideoCaptureStartedOrEnded();
        LoadDeviceManager();
        ScenarioStarted();
    }


    private void Update()
    {
        ApplyPendingActions();

        if (rawOutgoingVideoStreamResolution != customVideoResolution ||
            rawOutgoingVideoStreamFormat != customVideoFormat)
        {
            rawOutgoingVideoStreamResolution = customVideoResolution;
            rawOutgoingVideoStreamFormat = customVideoFormat;
            UpdateRawOutgoingVideoStream();
        }


        if (updateStatus)
        {
            updateStatus = false;
            UpdateStatus();
        }

        if (updateMuteStatus)
        {
            updateMuteStatus = false;
            UpdatedMuteStatus();
        }

        if (updateSpeakerMuteStatus)
        {
            updateSpeakerMuteStatus = false;
            UpdatedSpeakerMuteStatus();
        }

        if (updateListeningStatus)
        {
            updateListeningStatus = false;
            UpdateListeningStatus();
        }

        if (updateRemoteParticipants)
        {
            updateRemoteParticipants = false;
            UpdateRemoteParticipants();
        }

        if (updateVideoDeviceControls)
        {
            updateVideoDeviceControls = false;
            UpdateVideoDeviceControls();
        }

        if (updateOutgoingAudioStatus)
        {
            updateOutgoingAudioStatus = false;
            UpdateOutgoingAudioStatus();
        }

        if (updateIncomingAudioStatus)
        {
            updateIncomingAudioStatus = false;
            UpdateIncomingAudioStatus();
        }

        if (updateVideoCaptureStartedOrEnded)
        {
            updateVideoCaptureStartedOrEnded = false;
            UpdateVideoCaptureStartedOnEnded();
        }
    }

    private void ApplyPendingActions()
    {
        while (pendingActions.TryDequeue(out Action action))
        {
            action();
        }
    }


    private void OnApplicationQuit()
    {
        InvokeScenarioDestroyed();
    }

    private void OnDestroy()
    {
        InvokeScenarioDestroyed();
    }

    public async void Mute()
    {
        MuteAtStart = true;
        if (CurrentCall != null)
        {
            await CurrentCall.MuteAsync();
        }

        InvalidateMuteStatus();
    }

    public async void Unmute()
    {
        MuteAtStart = false;
        if (CurrentCall != null)
        {
            await CurrentCall.UnmuteAsync();
        }

        InvalidateMuteStatus();
    }

    public async void MuteSpeaker()
    {
        MuteSpeakerAtStart = true;
        if (CurrentCall != null)
        {
            await CurrentCall.MuteIncomingAudioAsync();
        }

        InvalidateSpeakerMuteStatus();
    }

    public async void UnmuteSpeaker()
    {
        MuteSpeakerAtStart = false;
        if (CurrentCall != null)
        {
            await CurrentCall.UnmuteIncomingAudioAsync();
        }

        InvalidateSpeakerMuteStatus();
    }

    public void RequestRawOutgoingAudio()
    {
        rawOutgoingAudioRequested = true;
        InvalidateOutgoingAudioStatus();
    }

    public void RequestStandardOutgoingAudio()
    {
        rawOutgoingAudioRequested = false;
        InvalidateOutgoingAudioStatus();
    }

    public void RequestRawIncomingAudio()
    {
        rawIncomingAudioRequested = true;
        InvalidateIncomingAudioStatus();
    }

    public void RequestStandardIncomingAudio()
    {
        rawIncomingAudioRequested = false;
        InvalidateIncomingAudioStatus();
    }

    public void ShareCamera()
    {
        isSharedCamera = true;
        if (videoDeviceControlList.Count > 0)
        {
            videoDeviceControlList[defaultCamIndex].StartCapture();
            InvalidateVideoCaptureStartedOrEnded();
        }        
    }

    public void UnShareCamera()
    {
        isSharedCamera = false;
        if (videoDeviceControlList.Count > 0)
        {
            videoDeviceControlList[defaultCamIndex].StopCapture();
            InvalidateVideoCaptureStartedOrEnded();
        }
    }

    /// <summary>
    /// Add event handlers to the call agent.
    /// </summary>
    private void AddCallAgentEventHandlers(CommonCallAgent commonCallAgent)
    {
        if (commonCallAgent != null)
        {
            if (commonCallAgent is CallAgent callAgent)
            {
                callAgent.IncomingCallReceived += OnIncomingCall;
            }
            else if (commonCallAgent is TeamsCallAgent teamsCallAgent)
            {
                teamsCallAgent.IncomingCallReceived += OnIncomingTeamsCall;
            }
            else
            {
                Log.Error<CallScenario>($"Unable register call agent events. Unknown call type {typeof(acsCallAgent)}.");
            }
        }
    }
    /// <summary>
    /// Remove event handlers from the call agent.
    /// </summary>
    private void RemoveCallAgentEventHandlers(CommonCallAgent commonCallAgent)
    {
        if (commonCallAgent != null)
        {
            if (commonCallAgent is CallAgent callAgent)
            {
                callAgent.IncomingCallReceived -= OnIncomingCall;
            }
            else if (commonCallAgent is TeamsCallAgent teamsCallAgent)
            {
                teamsCallAgent.IncomingCallReceived -= OnIncomingTeamsCall;
            }
            else
            {
                Log.Error<CallScenario>($"Unable unregister from call agent events. Unknown call type {typeof(acsCallAgent)}.");
            }
        }
    }

    /// <summary>
    /// Add new participant to this call. This functionality is not implemented.
    /// </summary>
    public RemoteParticipant AddParticipant(CallIdentifier personID)
    {
        Log.Warning<CallScenario>("Add participant currently not implemented");
        return null;
    }

    /// <summary>
    /// Remove a participant from this call 
    /// </summary>
    public async Task RemoveParticipant(RemoteParticipant identifier)
    {
        if (CurrentCall != null && identifier != null)
        {
            await CurrentCall.RemoveParticipantAsync(identifier);
        }
    }

    protected abstract void ScenarioStarted();

    protected abstract void ScenarioDestroyed();
    
    public virtual void Leave()
    {
        if (isSharedCamera)
        {
            UnShareCamera();
        }
        
        SingleAsyncRunner.QueueAsync(async () =>
        {
            await HangUpCurrentCall();
        });
    }

    protected async Task HangUpCurrentCall()
    {
        var call = CurrentCall;
        if (call != null && 
            call.State != CallState.Disconnected &&
            call.State != CallState.Disconnecting)
        {
            try
            {
                await call.HangUpAsync(new HangUpOptions()
                {
                    ForEveryone = false
                });
            }
            catch (Exception e)
            {
                Log.Error<CallScenario>($"Error HangUpCurrentCall. Exception: {e.Message}");
            }
        }
    }

    protected virtual void IncomingCall(CommonIncomingCall call)
    {
    }

    protected OutgoingVideoOptions CreateOutgoingVideoOptions()
    {
        if (CurrentCall != null)
        {
            Log.Error<CallScenario>("Unable to create new outgoing video options while there is an active call.");
            return null;
        }

        var result = new OutgoingVideoOptions();
        if (OutgoingVideoStreamsAtStart != null &&
            OutgoingVideoStreamsAtStart.Count > 0)
        {
            result.Streams = OutgoingVideoStreamsAtStart.ToArray();
        }

        return result;
    }

    protected IncomingVideoOptions CreateIncomingVideoOptions()
    {
        if (CurrentCall != null)
        {
            Log.Error<CallScenario>("Unable to create new incoming video options while there is an active call.");
            return null;
        }

        return new IncomingVideoOptions()
        {
            FrameKind = RawVideoFrameKind.Buffer,
            StreamKind = VideoStreamKind.RawIncoming
        };
    }

    protected OutgoingAudioOptions CreateOutgoingAudioOptions()
    {
        if (CurrentCall != null)
        {
            Log.Error<CallScenario>("Unable to create new outgoing audio options while there is an active call.");
            return null;
        }

        UpdateRawOutgoingAudioStream();

        var result = new OutgoingAudioOptions()
        {
            IsMuted = MuteAtStart,
            Filters = new OutgoingAudioFilters()
            {
                NoiseSuppressionMode = NoiseSuppressionMode.Low
            }
        };

        if (rawOutgoingAudioStream != null)
        {
            result.Stream = rawOutgoingAudioStream;
        }

        return result;
    }

    protected IncomingAudioOptions CreateIncomingAudioOptions()
    {
        if (CurrentCall != null)
        {
            Log.Error<CallScenario>("Unable to create new incoming audio options while there is an active call.");
            return null;
        }

        UpdateRawIncomingAudioStream();

        var result = new IncomingAudioOptions()
        {
            IsMuted = MuteSpeakerAtStart
        };

        if (rawIncomingAudioStream != null)
        {
            result.Stream = rawIncomingAudioStream;
        }

        return result;
    }

    protected void LoadDeviceManager()
    {
        SingleAsyncRunner.QueueAsync(async () =>
        {
            var callClient = CallClientHost.Instance.CallClient;
            CurrentDeviceManager = await callClient.GetDeviceManager();
        });
    }

    protected void UnloadDeviceManager()
    {
        SingleAsyncRunner.QueueAsync(() =>
        {
            CurrentDeviceManager = null;
            return Task.CompletedTask;
        });
    }

    protected void InvalidateStatus()
    {
        updateStatus = true;
        InvalidateOutgoingAudioStatus();
        InvalidateIncomingAudioStatus();
    }

    protected void InvalidateMuteStatus()
    {
        updateMuteStatus = true;
    }

    protected void InvalidateSpeakerMuteStatus()
    {
        updateSpeakerMuteStatus = true;
    }

    protected void InvalidateListeningStatus()
    {
        updateListeningStatus = true;
    }

    protected void InvalidateRemoteParticipants()
    {
        updateRemoteParticipants = true;
    }

    protected void InvalidateVideoDeviceControls()
    {
        updateVideoDeviceControls = true;
    }

    protected void InvalidateOutgoingAudioStatus()
    {
        updateOutgoingAudioStatus = true;
    }

    protected void InvalidateIncomingAudioStatus()
    {
        updateIncomingAudioStatus = true;
    }

    protected void InvalidateVideoCaptureStartedOrEnded()
    {
        updateVideoCaptureStartedOrEnded = true;
    }

    private void OnCurrentCallStateChanged(object sender, PropertyChangedEventArgs args)
    {
        InvalidateStatus();
    }

    private void OnCurrentCallMutedChanged(object sender, PropertyChangedEventArgs args)
    {
        InvalidateMuteStatus();
    }

    private void OnCurrentCallParticipantsUpdated(object sender, ParticipantsUpdatedEventArgs args)
    {
        InvalidateRemoteParticipants();
    }

    private void OnDeviceManagerCamerasUpdated(object sender, VideoDevicesUpdatedEventArgs args)
    {
        InvalidateVideoDeviceControls();
    }

    private void OnCurrentCallAgentIncomingCall(object sender, IncomingCallReceivedEventArgs args)
    {
        IncomingCall(args.IncomingCall);
    }

    private void OnCurrentCallAgentIncomingCall(object sender, TeamsIncomingCallReceivedEventArgs args)
    {
        IncomingCall(args.IncomingCall);
    }

    private void OnOutgoingVideoStreamStateChanged(object sender, VideoStreamStateChangedEventArgs args)
    {
        if (customVideoSource == null)
        {
            return;
        }

        var stream = args.Stream as RawOutgoingVideoStream;

        switch (stream.State)
        {
            case VideoStreamState.Available:
                break;
            case VideoStreamState.Started:
                customVideoSource.Generate(new CustomVideoSourceSettings()
                {
                    Size = rawOutgoingVideoStreamResolution.ToVector(),
                    Format = rawOutgoingVideoStreamFormat.ToTextureFormat()
                });
                break;
            case VideoStreamState.Stopping:
                break;
            case VideoStreamState.Stopped:
                customVideoSource.Stop();
                break;
            case VideoStreamState.NotAvailable:
                break;
        }
    }

    private void OnOutgoingVideoStreamFormatChanged(object sender, VideoStreamFormatChangedEventArgs args)
    {
        Log.Error<CallScenario>("Unable handle outgoing video format change.");
    }

    private void OnRawOutgoingAudioStreamStateChanged(object sender, AudioStreamStateChangedEventArgs args)
    {
        var stream = sender as RawOutgoingAudioStream;
        switch (args.Stream.State)
        {
            case AudioStreamState.Started:
                OnRawOutgingAudioStreamReady(stream);
                break;

            case AudioStreamState.Stopped:
                OnRawOutgoingAudioStreamStopped();
                break;
        }
    }

    private void OnRawOutgingAudioStreamReady(RawOutgoingAudioStream stream)
    {
        if (customAudioSource != null && stream != null)
        {
            customAudioSource.Generate(new CustomAudioSourceSettings()
            {
                Channels = this.customAudioChannelMode.ToInteger(),
                SampleRate = this.customAudioSampleRate.ToInteger(),
                SampleSizeInBytes = this.customAudioFormat.ToSizeInBytes(),
                ExpectedBufferSizeInBytes = stream.ExpectedBufferSizeInBytes
            });
        }
    }

    private void OnRawOutgoingAudioStreamStopped()
    {
        if (customAudioSource != null)
        {
            customAudioSource.Stop();
        }
    }

    private void OnRawIncomingHasMixedAudioBufferAvailable(object sender, IncomingMixedAudioEventArgs args)
    {
        var stream = sender as RawIncomingAudioStream;
        if (customAudioReceiver != null && stream != null)
        {
            customAudioReceiver.AddSamples(args.AudioBuffer.Buffer);
        }
    }

    private void OnRawIncomingHasUnmixedAudioBufferAvailable(object sender, IncomingUnmixedAudioEventArgs args)
    {
        var stream = sender as RawIncomingAudioStream;
        if (customAudioReceiver != null && stream != null && args.AudioBuffer.Count > 0)
        {
            var participantIds = args.RemoteParticipantIds;
            Log.Verbose<CallScenario>($"Using ummixed audio for participant id = {(participantIds != null && participantIds.Count > 0 ? participantIds[0] : "empty")}");
            customAudioReceiver.AddSamples(args.AudioBuffer[0].Buffer);
        }
    }

    private void OnRawIncomingAudioStreamChanged(object sender, AudioStreamStateChangedEventArgs args)
    {
        var stream = sender as RawIncomingAudioStream;
        if (args.Stream.State == AudioStreamState.Stopped)
        {
            OnRawIncomingAudioStreamStopped();
            stream.MixedAudioBufferReceived -= OnRawIncomingHasMixedAudioBufferAvailable;
            stream.UnmixedAudioBufferReceived -= OnRawIncomingHasUnmixedAudioBufferAvailable;
        }
        else
        {
            OnRawIncomingAudioStreamStarted(stream);
            stream.MixedAudioBufferReceived += OnRawIncomingHasMixedAudioBufferAvailable;
            stream.UnmixedAudioBufferReceived += OnRawIncomingHasUnmixedAudioBufferAvailable;
        }
    }


    private void OnRawIncomingAudioStreamStarted(RawIncomingAudioStream stream)
    {
        if (customAudioReceiver != null &&
            !customAudioReceiver.IsProcessing &&
            rawIncomingAudioOptions != null)
        {
            customAudioReceiver.Process(new CustomAudioReceiverSettings()
            {
                Channels = rawIncomingAudioOptions.Properties.ChannelMode.ToInteger(),
                SampleRate = rawIncomingAudioOptions.Properties.SampleRate.ToInteger()
            });
        }
    }

    private void OnRawIncomingAudioStreamStopped()
    {
        if (customAudioReceiver != null)
        {
            customAudioReceiver.Stop();
        }
    }

    private void InvokeScenarioDestroyed()
    {
        if (customVideoSource != null)
        {
            customVideoSource.OnMediaSampleReady -= OnSceneVideoFrameReady;
        }

        if (customAudioSource != null)
        {
            customAudioSource.OnMediaSamplesReady -= OnCustomAudioSamplesReady;
        }

        UnloadDeviceManager();
        StopAndReleaseRawOutgoingVideo();
        ReleaseRawOutgoingAudioStream();
        ReleaseRawIncomingAudioStream();
        ScenarioDestroyed();
    }

    private void UpdateStatus()
    {
        // If the call is disconnected, clear the current call. Send a "disconnected" event and then send
        // a "no call" event.
        if (CurrentCall != null && CurrentCall.State == CallState.Disconnected)
        {
            statusChanged?.Invoke(new CallScenarioStateChangedEventArgs(this, CurrentCall));
            CurrentCall = null;
        }

        statusChanged?.Invoke(new CallScenarioStateChangedEventArgs(this, CurrentCall));
    }

    private void UpdatedMuteStatus()
    {
        bool muted = MuteAtStart;
        if (CurrentCall != null)
        {
            muted = CurrentCall.IsOutgoingAudioMuted;
        }

        if (muted)
        {
            Muted?.Invoke();
        }
        else
        {
            Unmuted?.Invoke();
        }
    }

    private void UpdateListeningStatus()
    {
        listeningStatusChanged?.Invoke(CallAgent == null ? "Not Listening" : "Listening");
    }

    private void UpdatedSpeakerMuteStatus()
    {
        bool muted = MuteSpeakerAtStart;
        if (CurrentCall != null)
        {
            muted = CurrentCall.IsIncomingAudioMuted;
        }

        speakerMuteStatusChanged?.Invoke(muted ? "Muted" : "Unmuted");
    }

    private void UpdateOutgoingAudioStatus()
    {
        requestedOutgoingAudioStatus?.Invoke(rawOutgoingAudioRequested ? labelRaw : labelStandard);
        outgoingAudioStatus?.Invoke(rawOutgoingAudioStream != null ? labelRaw : labelStandard);
        UpdateOutgoingAndIncomingAudioStatus();
    }

    private void UpdateIncomingAudioStatus()
    {
        requestedIncomingAudioStatus?.Invoke(rawIncomingAudioRequested ? labelRaw : labelStandard);
        incomingAudioStatus?.Invoke(rawIncomingAudioStream != null ? labelRaw : labelStandard);
        UpdateOutgoingAndIncomingAudioStatus();
    }

    private void UpdateVideoCaptureStartedOnEnded()
    {
        int activeVideoCaptures = 0;

        if (CurrentCall != null)
        {
            activeVideoCaptures = CurrentCall.OutgoingVideoStreams.Count;
        }

        if (activeVideoCaptures > 0)
        {
            videoCaptureStarted?.Invoke();
        }
        else
        {
            videoCaptureEnded?.Invoke();
        }
    }

    private void UpdateOutgoingAndIncomingAudioStatus()
    {
        requestedOutgoingAndIncomingAudioStatus?.Invoke($"{(rawOutgoingAudioRequested ? labelRaw : labelStandard)} / {(rawIncomingAudioRequested ? labelRaw : labelStandard)}");
        outgoingAndIncomingAudioStatus?.Invoke($"{(rawOutgoingAudioStream != null ? labelRaw : labelStandard)} / {(rawIncomingAudioStream != null ? labelRaw : labelStandard)}");
    }

    private void UpdateRemoteParticipants()
    {
        List<object> remoteParticipants = new List<object>();
        if (CurrentCall != null)
        {
            var inner = CurrentCall.RemoteParticipants;
            foreach (var item in inner)
            {
                remoteParticipants.Add(item);
            }
        }

        remoteParticipantsChanged?.Invoke(remoteParticipants);
    }

    private void ReleaseRawOutgoingAudioStream()
    {
        if (rawOutgoingAudioStream != null)
        {
            rawOutgoingAudioStream.StateChanged -= OnRawOutgoingAudioStreamStateChanged;
            rawOutgoingAudioStream = null;
        }

        rawOutgoingAudioOptions = null;

        if (customAudioSource != null)
        {
            customAudioSource.Stop();
        }
    }

    private void UpdateRawOutgoingAudioStream()
    {
        ReleaseRawOutgoingAudioStream();

        if (rawOutgoingAudioRequested)
        {
            rawOutgoingAudioOptions = new RawOutgoingAudioStreamOptions()
            {
                Properties = new RawOutgoingAudioStreamProperties()
                {
                    ChannelMode = customAudioChannelMode,
                    Format = customAudioFormat,
                    BufferDuration = customAudioTimePerBlock,
                    SampleRate = customAudioSampleRate
                }
            };

            rawOutgoingAudioStream = new RawOutgoingAudioStream(rawOutgoingAudioOptions);
            rawOutgoingAudioStream.StateChanged += OnRawOutgoingAudioStreamStateChanged;
        }

        InvalidateOutgoingAudioStatus();
    }

    private void ReleaseRawIncomingAudioStream()
    {
        if (rawIncomingAudioStream != null)
        {
            rawIncomingAudioStream.StateChanged -= OnRawIncomingAudioStreamChanged;
            rawIncomingAudioStream = null;
        }

        rawIncomingAudioOptions = null;

        if (customAudioReceiver != null)
        {
            customAudioReceiver.Stop();
        }
    }

    private void UpdateRawIncomingAudioStream()
    {
        ReleaseRawIncomingAudioStream();

        if (rawIncomingAudioRequested)
        {
            rawIncomingAudioOptions = new RawIncomingAudioStreamOptions()
            {
                Properties = new RawIncomingAudioStreamProperties()
                {
                    ChannelMode = customAudioChannelMode,
                    Format = customAudioFormat,
                    SampleRate = customAudioSampleRate,
                },
                ReceiveUnmixedAudio = false
            };

            rawIncomingAudioStream = new RawIncomingAudioStream(rawIncomingAudioOptions);
            rawIncomingAudioStream.StateChanged += OnRawIncomingAudioStreamChanged;
        }

        InvalidateIncomingAudioStatus();
    }

    private void StopAndReleaseRawOutgoingVideo()
    {
        if (customVideoSource != null)
        {
            customVideoSource.Stop();
        }

        if (rawOutgoingVideoStream != null)
        {
            _ = StopVideoCapture(rawOutgoingVideoStream);

            try
            {
                rawOutgoingVideoStream.StateChanged -= OnOutgoingVideoStreamStateChanged;
                rawOutgoingVideoStream.FormatChanged -= OnOutgoingVideoStreamFormatChanged;
            }
            catch (Exception)
            {
                // BUGBUG sometimes removing event handlers throws
            }

            rawOutgoingVideoStream = null;
        }

        rawOutgoingVideoStreamOptions = null;
    }

    private void UpdateRawOutgoingVideoStream()
    {
        StopAndReleaseRawOutgoingVideo();

        var rawOutgoingVideoStreamSize = rawOutgoingVideoStreamResolution.ToVector();
        rawOutgoingVideoStreamOptions = new RawOutgoingVideoStreamOptions();
        rawOutgoingVideoStreamOptions.Formats = new VideoStreamFormat[]
        {
            new VideoStreamFormat()
            {
                Width = rawOutgoingVideoStreamSize.x,
                Height = rawOutgoingVideoStreamSize.y,
                FramesPerSecond = 15,
                PixelFormat = rawOutgoingVideoStreamFormat,
                Stride1 = rawOutgoingVideoStreamSize.x * rawOutgoingVideoStreamFormat.PixelSize(),
                Stride2 = 0,
                Stride3 = 0
            }
        };

        rawOutgoingVideoStream = new VirtualOutgoingVideoStream(rawOutgoingVideoStreamOptions);
        rawOutgoingVideoStream.StateChanged += OnOutgoingVideoStreamStateChanged;
        rawOutgoingVideoStream.FormatChanged += OnOutgoingVideoStreamFormatChanged;

        InvalidateVideoDeviceControls();
    }

    private void UpdateVideoDeviceControls()
    {
        var newVideoDeviceControls = new List<object>();

        if (videoDeviceControlList != null)
        {
            foreach (var item in videoDeviceControlList)
            {
                item.CapturingChanged -= OnVideoCapturingChanged;
                item.Dispose();
            }
            videoDeviceControlList.Clear();
        }

        if (rawOutgoingVideoStream != null)
        {
            VideoDeviceControls videoDevCtrl = new VideoDeviceControls("Scene Generate", rawOutgoingVideoStream, StartVideoCapture, StopVideoCapture);
            videoDeviceControlList.Add(videoDevCtrl);
            newVideoDeviceControls.Add(videoDevCtrl);
            videoDevCtrl.CapturingChanged += OnVideoCapturingChanged;

        }

        if (CurrentDeviceManager != null)
        {
            var inner = CurrentDeviceManager.Cameras;
            foreach (var item in inner)
            {
                VideoDeviceControls videoDevCtrl = new VideoDeviceControls(item.Name, new LocalOutgoingVideoStream(item), StartVideoCapture, StopVideoCapture);
                videoDeviceControlList.Add(videoDevCtrl);
                newVideoDeviceControls.Add(videoDevCtrl);
                videoDevCtrl.CapturingChanged += OnVideoCapturingChanged;

                // default camera for HoloLens2
                if (item.Name.Contains(defaultCameraName))
                {
                    defaultCamIndex = videoDeviceControlList.Count - 1;
                }
            }
        }

        videoDeviceControlsChanged?.Invoke(newVideoDeviceControls);
    }

    private async Task<bool> StartVideoCapture(OutgoingVideoStream outgoingVideoStream)
    {
        bool startedOrStarting = false;

        if (outgoingVideoStream != null)
        {
            if (CurrentCall != null)
            {
                try
                {
                    await CurrentCall.StartVideoAsync(outgoingVideoStream);
                    startedOrStarting = true;
                }
                catch (Exception ex)
                {
                    Log.Error<CallScenario>($"Failed to start video. Exception: {ex}");
                }
            }
            else
            {
                startedOrStarting = true;
            }
        }

        if (startedOrStarting)
        {
            outgoingVideoStreamsAtStart.Add(outgoingVideoStream);
        }

        return startedOrStarting;
    }

    private async Task<bool> StopVideoCapture(OutgoingVideoStream outgoingVideoStream)
    {
        bool stopped = false;

        if (outgoingVideoStream != null)
        {
            outgoingVideoStreamsAtStart.Remove(outgoingVideoStream);
            if (outgoingVideoStream.State == VideoStreamState.Stopped ||
                outgoingVideoStream.State == VideoStreamState.NotAvailable)
            {
                stopped = true;
            }
            else if (CurrentCall != null)
            {
                try
                {
                    await CurrentCall.StopVideoAsync(outgoingVideoStream);
                    stopped = true;
                }
                catch (Exception ex)
                {
                    Log.Error<CallScenario>($"Failed to stop video. Exception: {ex}");
                }
            }
            else
            {
                stopped = true;
            }
        }

        return stopped;
    }

    private void OnSceneVideoFrameReady(object sender, MediaSampleArgs args)
    {
        var stream = rawOutgoingVideoStream;

        if (rawOutgoingVideoStream != null &&
            rawOutgoingVideoStream.Format != null &&
            rawOutgoingVideoStream.State == VideoStreamState.Started)
        {
            // Callback is called on a background thread. So ok to block
            rawOutgoingVideoStream.SendRawVideoFrameAsync(new RawVideoFrameBuffer()
            {
                Buffers = new NativeBuffer[] { args.Buffer },
                StreamFormat = rawOutgoingVideoStream.Format,
                TimestampInTicks = stream.TimestampInTicks
            }).Wait();
        }
    }

    private void OnCustomAudioSamplesReady(object sender, MediaSampleArgs args)
    {
        var stream = rawOutgoingAudioStream;

        if (stream != null && args.Container is RawAudioBuffer)
        {
            stream.SendRawAudioBufferAsync((RawAudioBuffer)args.Container).Wait();
        }
    }

    private void OnVideoCapturingChanged(object sender, bool capturing)
    {
        InvalidateVideoCaptureStartedOrEnded();
    }
}

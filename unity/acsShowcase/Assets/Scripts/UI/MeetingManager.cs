// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. 

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using Azure.Communication.Calling.Unity;
using Azure.Communication.Calling.Unity.Rest;
using Azure.Communication.Calling.UnityClient;


/// <summary>
/// Manage login authentication, teams meeting, incoming call, et... 
/// </summary>
public class MeetingManager : MonoBehaviour
{
    [Header("Authentication")] [SerializeField] [Tooltip("The Authentication manager for signing into Azure Active Directory.")]
    private AuthenticationManager authenticationManager = null;

    [SerializeField] [Tooltip("Clear the token cache during log-in.")]
    private bool clearTokenCache = false;

    [Header("Azure Communication Services")] [SerializeField] [Tooltip("The Azure Communication Services endpoint.")]
    private string communicationEndpoint = null;

    [SerializeField] [Tooltip("The Azure Communication authentication key. If specified, this will be used instead of obtaining an ACS access token from the configured function app.")]
    private string communicationKey = null;

    [SerializeField] [Tooltip("The Azure Communication user access token and id. If specified, this will be used instead of obtaining an ACS access token from the configured function app, and instead of the configured authentication key.")]
    private BringYourOwnIdentityToken communicationUserAccessToken = new BringYourOwnIdentityToken();

    [SerializeField] [Tooltip("The request for an Azure Active Directory access token containing the Azure Communication Services scopes. This will be exchanged for an Azure Communication Services Identity access token, via the Funciton App wrappers.")]
    private AuthenticationRequest communicationAzureActiveDirectoryAccess = null;

    [Header("Azure Function App Wrapper")] [SerializeField] [Tooltip("The Azure Function App endpoint that is wrapping Azure Communication Services access token creation.")]
    private string functionAppEndpoint = null;

    [SerializeField] [Tooltip("The request for an access token containing the Function App scope. This is the custom Function App wrapping the Azure Communciation Serivces identity APIs for multi-tenant use.")]
    private AuthenticationRequest functionAppAccess = null;

    [SerializeField] [Tooltip("The request for an access token containing the Microsoft Graph scopes.")]
    private AuthenticationRequest graphAccess = null;

    [SerializeField] [Tooltip("Should the user sign into Teams as a guest.")]
    private bool forceSignInAsGuest = false;

    [Header("Meeting Settings")] [SerializeField] [Tooltip("The default meeting locator to use during a Join() request.")]
    private MeetingLocator defaultMeetingLocator = null;

    [SerializeField] [Tooltip("The local wecam video automatically be started on joining meeting.")]
    private bool autoShareLocalVideo = false;

    public bool AutoShareLocalVideo
    {
        get => autoShareLocalVideo;
    }

    [Header("Events")] [SerializeField] [Tooltip("GraphEvent raised when joined a call.")]
    private UnityEvent joinedCall = new UnityEvent();

    [SerializeField] [Tooltip("GraphEvent raised when joining a call.")]
    private UnityEvent joiningCall = new UnityEvent();

    [SerializeField] [Tooltip("GraphEvent raised when left a call.")]
    private UnityEvent leftCall = new UnityEvent();

    [SerializeField] [Tooltip("GraphEvent raised when microphone is muted.")]
    private UnityEvent muted = new UnityEvent();

    [SerializeField] [Tooltip("GraphEvent raised when microphone is unmuted.")]
    private UnityEvent unmuted = new UnityEvent();

    [SerializeField] [Tooltip("GraphEvent raised when video capture started.")]
    private UnityEvent videoCaptureStarted = new UnityEvent();

    [SerializeField] [Tooltip("GraphEvent raised when video capture ended.")]
    private UnityEvent videoCaptureEnded = new UnityEvent();

    [SerializeField] [Tooltip("GraphEvent raised when status changes.")]
    private MeetingMaanagerStatusStringEvent statusChanged = new MeetingMaanagerStatusStringEvent();

    [SerializeField] [Tooltip("GraphEvent raised when there is an incoming meeting.")]
    private MeetingManagerIncomingMeetingCallEvent incomingCall = new MeetingManagerIncomingMeetingCallEvent();

    [SerializeField] [Tooltip("GraphEvent raised when incoming call accepted")]
    private UnityEvent incomingCallAccepted = new UnityEvent();

    [SerializeField] [Tooltip("Reference to the team meeting handler ")]
    private JoinTeamsMeeting teamMeeting;

    [SerializeField] [Tooltip("Reference to the incoming call handler")]
    private HandleIncomingCall handleIncomingCall;

    /// <summary>
    /// is mute changed event 
    /// </summary>
    public event Action<MeetingManager, bool> IsMutedChanged;

    /// <summary>
    /// is loggedIn change event 
    /// </summary>
    public event Action<MeetingManager, bool> IsLoggedInChanged;

    /// <summary>
    /// meeting status changed event 
    /// </summary>
    public event Action<MeetingManager, MeetingStatus> StatusChanged;

    /// <summary>
    /// meeting last call state 
    /// </summary>
    private MeetingCallState lastCallState = MeetingCallState.Disconnecting;

    /// <summary>
    /// logged in task 
    /// </summary>
    private Task<bool> loggedInTask = null;

    /// <summary>
    /// is logged in?
    /// </summary>
    private bool loggedIn;

    public bool IsLoggedIn
    {
        get => loggedIn;

        private set
        {
            if (loggedIn != value)
            {
                loggedIn = value;
                IsLoggedInChanged?.Invoke(this, value);
            }
        }
    }

    /// <summary>
    /// pending action 
    /// </summary>
    private ConcurrentQueue<Action> pendingActions = new ConcurrentQueue<Action>();

    /// <summary>
    /// authetication cancel
    /// </summary>
    private CancellationTokenSource autheticationCancel;

    /// <summary>
    /// service identity provider 
    /// </summary>
    private MeetingServiceIdentityProvider serviceIdentityProvider = new MeetingServiceIdentityProvider();

    /// <summary>
    /// service identity
    /// </summary>
    MeetingServiceIdentity serviceIdentity = null;

    /// <summary>
    /// communication azure active directory access token response
    /// </summary>
    private TokenResponse communicationAzureActiveDirectoryAccessTokenResponse = TokenResponse.Invalid;

    /// <summary>
    /// functionApp access token response
    /// </summary>
    private TokenResponse functionAppAccessTokenResponse = TokenResponse.Invalid;

    /// <summary>
    /// graph access token response
    /// </summary>
    private TokenResponse graphAccessTokenResponse = TokenResponse.Invalid;

    /// <summary>
    /// keep track of the current active call 
    /// </summary>
    private CallScenario currentActiveCall = null;

    /// <summary>
    /// enable to start listening to the incoming call 
    /// </summary>
    private bool startListeningComingCall = false;

    /// <summary>
    /// is camera enabled?
    /// </summary>
    private bool isSharedCamera = true;

    /// <summary>
    /// is microphone muted?
    /// </summary>
    private bool isMute = false;

    /// <summary>
    /// is speaker muted?
    /// </summary>
    private bool isSpeakerOff = false;

    /// <summary>
    /// current meeting status
    /// </summary>
    private MeetingStatus status = new MeetingStatus();

    /// <summary>
    /// Get the current status
    /// </summary>
    public MeetingStatus Status
    {
        get => status;

        set
        {
            if (status != value)
            {
                Log.Verbose<MeetingManager>("Status {0}->{1}", status, value);
                status = value;
                StatusChanged?.Invoke(this, status);
            }
        }
    }

    /// <summary>
    /// The display name of the signed in user.
    /// </summary>
    public string DisplayName
    {
        get;
        private set;
    }

    /// <summary>
    /// OnEnable
    /// </summary>
    private void OnEnable()
    {
        ParticipantPanelController.OnAddParticipant += ParticipantPanelControllerOnAddParticipant;
        ParticipantPanelController.OnRemoveParticipant += ParticipantPanelControllerOnRemoveParticipant;
    }

    /// <summary>
    /// OnDisable
    /// </summary>
    private void OnDisable()
    {
        ParticipantPanelController.OnAddParticipant -= ParticipantPanelControllerOnAddParticipant;
        ParticipantPanelController.OnRemoveParticipant -= ParticipantPanelControllerOnRemoveParticipant;
    }

    /// <summary>
    /// Update
    /// </summary>
    public void Update()
    {
        ApplyPendingActions();
        if (startListeningComingCall)
        {
            if (currentActiveCall != null)
            {
                // wait for previous call agent to be destroyed before start listening 
                if (currentActiveCall.CurrentCallAgent == null)
                {
                    handleIncomingCall.StartListening(DisplayName, forceSignInAsGuest);
                    SetCurrentActiveCall(null);
                    startListeningComingCall = false;
                }
            }
        }
    }

    /// <summary>
    /// Start the sign-in process.
    /// </summary>
    public void SignIn()
    {
        LogInIfPossible();
    }

    /// <summary>
    /// Cancel the sign-in process, if it is in progress still.
    /// </summary>
    public void CancelSignIn()
    {
        if (!IsLoggedIn)
        {
            _ = CancelPreviousLoginAndCreateNewCancellationToken();
        }
    }

    /// <summary>
    /// Sign out of the meeting services.
    /// </summary>
    public void SignOut()
    {
        LogoutWorker();
    }

    /// <summary>
    /// Join a default meeting 
    /// </summary>
    public void Join()
    {
        Log.Verbose<MeetingManager>("Joining with default meeting locator ({0})", defaultMeetingLocator);
        Join(defaultMeetingLocator);
    }

    /// <summary>
    /// join a meeting 
    /// </summary>
    /// <param name="locator"></param>
    public void Join(MeetingLocator locator)
    {
        if (locator == null)
        {
            Log.Error<MeetingManager>("Invalid meeting locator");
        }
        else
        {
            teamMeeting.Token = serviceIdentity.CommunicationAccessToken;
            TeamsUrlLocator teamLocator = locator as TeamsUrlLocator;
            if (teamLocator != null)
            {
                handleIncomingCall.StopListening();
                Log.Verbose<MeetingManager>("Token: {0}", teamMeeting.Token);
                Log.Verbose<MeetingManager>("Team URL: {0}", teamLocator.Url);
                teamMeeting.JoinUrl = teamLocator.Url;
                teamMeeting.Join(DisplayName);
            }

            SetCurrentActiveCall(teamMeeting);
        }
    }


    /// <summary>
    /// Add new participant to current team call
    /// </summary>
    public RemoteParticipant AddParticipant(GraphUser person)
    {
        if (person.userPrincipalName == "")
        {
            String[] name = person.mail.Split(new[] { '@' });
            string[] firstAndLast = name[0].Split(new[] { '.' });

            person.displayName = name[0];
            person.userPrincipalName = name[0];
            person.givenName = firstAndLast[0];
            person.surname = firstAndLast[1];
        }

        var newUser = new TeamsUserIdentity(person);
        if (currentActiveCall != null)
            return currentActiveCall.AddParticipant(newUser.CreateIdentifier());
        else
            return null;
    }

    /// <summary>
    /// remove a participant from current team call
    /// </summary>
    public async Task RemoveParticipant(RemoteParticipant identifier)
    {
        if (currentActiveCall != null)
        {
            await currentActiveCall.RemoveParticipant(identifier);
        }
    }

    /// <summary>
    /// get called when removing participant 
    /// </summary>
    /// <param name="participant"></param>
    private async void ParticipantPanelControllerOnRemoveParticipant(RemoteParticipant participant)
    {
        try
        { 
            await RemoveParticipant(participant);
        }
        catch (Exception ex)
        {
            Log.Error<MeetingManager>("Failed to remove participant. {0}", ex);
        }
    }

    /// <summary>
    /// get called when adding participant 
    /// </summary>
    /// <param name="participant"></param>
    private void ParticipantPanelControllerOnAddParticipant(GraphUser person)
    {
        AddParticipant(person);
    }


    /// <summary>
    /// use for 1-1 calling, ACS SDK not yet supporting calling to Teams user. 
    /// </summary>
    /// <param name="user"></param>
    public void Create(UserIdentity user)
    {
        if (user == null)
        {
            Log.Error<MeetingManager>("Can't create call. Invalid user");
        }
        else
        {
            // 1-1 calling not working 
            //LogInIfPossible(joinLocator: null, user);
        }
    }

    /// <summary>
    /// set shared camera
    /// </summary>
    public void SetShareCamera(bool enable)
    {
        if (enable)
            ShareCamera();
        else
            UnshareCamera();
    }

    /// <summary>
    /// enable camera
    /// </summary>
    public void ShareCamera()
    {
        isSharedCamera = true;
        UpdateCamera();
    }

    /// <summary>
    /// disable camera 
    /// </summary>
    public void UnshareCamera()
    {
        isSharedCamera = false;
        UpdateCamera();
    }

    /// <summary>
    /// update camera 
    /// </summary>
    private void UpdateCamera()
    {
        if (currentActiveCall != null)
        {
            if (isSharedCamera)
            {
                currentActiveCall.ShareCamera();
            }
            else
            {
                currentActiveCall.UnShareCamera();
            }
        }
    }

    /// <summary>
    /// mute microphone 
    /// </summary>
    public void Mute()
    {
        isMute = true;
        UpdateMicrophone();
    }

    /// <summary>
    /// unmute microphone 
    /// </summary>
    public void Unmute()
    {
        isMute = false;
        UpdateMicrophone();
    }


    /// <summary>
    /// update microphone 
    /// </summary>
    private void UpdateMicrophone()
    {
        if (currentActiveCall != null)
        {
            if (isMute)
            {
                currentActiveCall.Mute();
            }
            else
            {
                currentActiveCall.Unmute();
            }
        }
    }

    /// <summary>
    /// mute speaker 
    /// </summary>
    public void MuteSpeaker()
    {
        isSpeakerOff = true;
        UpdateSpeaker();
    }

    /// <summary>
    /// unmute speaker 
    /// </summary>
    public void UnmuteSpeaker()
    {
        isSpeakerOff = false;
        UpdateSpeaker();
    }

    /// <summary>
    /// update speaker 
    /// </summary>
    private void UpdateSpeaker()
    {
        if (currentActiveCall != null)
        {
            if (isSpeakerOff)
                currentActiveCall.MuteSpeaker();
            else
                currentActiveCall.UnmuteSpeaker();
        }
    }


    /// <summary>
    /// leave a current call
    /// </summary>
    public void Leave()
    {
        if (currentActiveCall != null)
        {
            currentActiveCall.Leave();
            startListeningComingCall = true;
        }
    }


    /// <summary>
    /// login/authenticate and join a meeting 
    /// </summary>
    /// <param name="joinLocator"></param>
    /// <param name="createCallWithUser"></param>
    private async void LogInIfPossible()
    {
        var loggedIdTask = loggedInTask;
        if (loggedIdTask == null || !loggedIn)
        {
            loggedInTask = loggedIdTask = LoginWorker();
        }

        if (loggedIdTask == null)
        {
            Log.Verbose<MeetingManager>("LogIntoMeetingServices request ignored, already logged in");
        }
        else if (await loggedIdTask)
        {
            Log.Verbose<MeetingManager>("LogIntoMeetingServices completes");
        }
    }

    /// <summary>
    /// login worker task 
    /// </summary>
    private async Task<bool> LoginWorker()
    {
        CancellationToken cancellationToken = CancelPreviousLoginAndCreateNewCancellationToken();

        Log.Verbose<MeetingManager>("LoginWorker");

        bool obtainedAuthenticationTokens = false;
        if (await RequestAzureTokens(cancellationToken) &&
            !cancellationToken.IsCancellationRequested)
        {
            communicationAzureActiveDirectoryAccessTokenResponse = communicationAzureActiveDirectoryAccess?.TokenResponse ?? TokenResponse.Invalid;
            functionAppAccessTokenResponse = functionAppAccess?.TokenResponse ?? TokenResponse.Invalid;
            graphAccessTokenResponse = graphAccess?.TokenResponse ?? TokenResponse.Invalid;

            obtainedAuthenticationTokens = 
                communicationAzureActiveDirectoryAccessTokenResponse.IsValid() &&
                functionAppAccessTokenResponse.IsValid() &&
                graphAccessTokenResponse.IsValid();
        }

        bool loggedIntoMeetingServices = false;
        if (obtainedAuthenticationTokens)
        { 
            loggedIntoMeetingServices = await LogIntoMeetingServices(cancellationToken);
        }

        if (cancellationToken.IsCancellationRequested)
        {
            Log.Verbose<MeetingManager>("Log in cancelled.");
        }
        else if (loggedIntoMeetingServices)
        {
            Log.Verbose<MeetingManager>("Logged in.");
            IsLoggedIn = true;

            if (ProfileGetter.Profile != null)
            {
                DisplayName = ProfileGetter.Profile.displayName;
            }
            else
            {
                DisplayName = string.Empty;
            }            

            // start listening to the incoming call
            handleIncomingCall.Token = serviceIdentity.CommunicationAccessToken;
            handleIncomingCall.StartListening(DisplayName, forceSignInAsGuest);
        }
        else
        {
            Log.Error<MeetingManager>("Log in failed.");
            IsLoggedIn = false;
        }

        return IsLoggedIn;
    }

    /// <summary>
    /// Log into meeting services.
    /// </summary>
    private async Task<bool> LogIntoMeetingServices(CancellationToken cancellationToken)
    {
        Log.Verbose<MeetingManager>("Logging in");

        Status = MeetingStatus.NoCall(MeetingAuthenticationState.LoggingIn);

        string name = DisplayName;
        try
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                name = await SystemUser.GetName();
                DisplayName = name;
            }
        }
        catch (Exception ex)
        {
            Log.Error<MeetingManager>("Failed to find user's display name. {0}", ex);
        }

        var options = new MeetingServiceIdentityProviderOptions()
        {
            communicationEndpoint = communicationEndpoint,
            communicationAzureActiveDirectoryAccess = communicationAzureActiveDirectoryAccessTokenResponse,
            communicationUserAccessToken = communicationUserAccessToken,
            communicationKey = communicationKey,
            functionAppEndpoint = functionAppEndpoint,
            functionAppAccess = functionAppAccessTokenResponse,
            graphAccessToken = graphAccessTokenResponse,
            displayName = DisplayName
        };

        try
        {
            if (options.communicationAzureActiveDirectoryAccess.IsValid() && !forceSignInAsGuest)
            {
                serviceIdentity = await serviceIdentityProvider.CreateTeamsUser(options);
            }
            else
            {
                serviceIdentity = await serviceIdentityProvider.CreateGuest(options);
            }
        }
        catch (Exception ex)
        {
            Log.Error<MeetingManager>("LogIntoMeetingServices failed. {0}", ex);
        }

        if (serviceIdentity == null || cancellationToken.IsCancellationRequested)
        {
            Status = MeetingStatus.LoginError();
            return false;
        }
        else
        {
            Status = MeetingStatus.NoCall(MeetingAuthenticationState.LoggedIn);
            return true;              
        }
    }

    /// <summary>
    /// Logout worker 
    /// </summary>
    private void LogoutWorker()
    {
        communicationAzureActiveDirectoryAccessTokenResponse = TokenResponse.Invalid;
        functionAppAccessTokenResponse = TokenResponse.Invalid; 
        graphAccessTokenResponse = TokenResponse.Invalid;

        serviceIdentity = null;
        IsLoggedIn = false;
        DisplayName = string.Empty;
        _ = CancelPreviousLoginAndCreateNewCancellationToken();
    }

    /// <summary>
    /// Obtain an authentication token for Azure services.
    /// </summary>
    private async Task<bool> RequestAzureTokens(CancellationToken cancellationToken)
    {
        var manager = authenticationManager;
        
        if (manager == null)
        {
            Log.Error<MeetingManager>("No authentication manager. Unable to login.");
            return false;
        }

        if (clearTokenCache)
        {
            await manager.ClearAuthenticationAsync();
        }

        List<AuthenticationRequest> input = new List<AuthenticationRequest>();
        if (communicationAzureActiveDirectoryAccess != null)
        {
            input.Add(communicationAzureActiveDirectoryAccess);
        }

        if (functionAppAccess != null)
        {
            input.Add(functionAppAccess);
        }

        if (graphAccess != null)
        {
            input.Add(graphAccess);
        }

        var output = await manager.AuthenticateAsync(input.ToArray(), cancellationToken);
        return output != null && output.Length >= input.Count;
    }

    /// <summary>
    /// cancel login
    /// </summary>
    /// <returns></returns>
    private CancellationToken CancelPreviousLoginAndCreateNewCancellationToken()
    {
        var oldAuthenicationCancel = autheticationCancel;
        if (oldAuthenicationCancel != null)
        {
            oldAuthenicationCancel.Cancel();
            oldAuthenicationCancel.Dispose();
        }

        var authenicationCancel = autheticationCancel = new CancellationTokenSource();
        return authenicationCancel.Token;
    }

    /// <summary>
    /// apply status 
    /// </summary>
    /// <param name="status"></param>
    private void ApplyStatus(MeetingStatus status)
    {
        SetStatusText(status.CreateDisplayString());

        if (lastCallState != status.CallState)
        {
            lastCallState = status.CallState;
            switch (lastCallState)
            {
                case MeetingCallState.Connected:
                    joinedCall?.Invoke();
                    break;

                case MeetingCallState.Disconnected:
                case MeetingCallState.Disconnecting:
                    leftCall?.Invoke();
                    break;

                case MeetingCallState.Connecting:
                case MeetingCallState.Ringing:
                case MeetingCallState.InLobby:
                    joiningCall?.Invoke();
                    break;

                case MeetingCallState.None:
                    Log.Verbose<MeetingManager>("Unhandled call state {0}", lastCallState);
                    break;

                default:
                    Log.Warning<MeetingManager>("Unhandled call state {0}", lastCallState); 
                    break;
            }
        }

        StatusChanged?.Invoke(this, status);
    }


    /// <summary>
    /// apply mute 
    /// </summary>
    /// <param name="isMuted"></param>
    private void ApplyIsMuted(bool isMuted)
    {
        IsMutedChanged?.Invoke(this, isMuted);
        if (isMuted)
        {
            muted.Invoke();
        }
        else
        {
            unmuted.Invoke();
        }
    }

    /// <summary>
    /// set status text 
    /// </summary>
    /// <param name="status"></param>
    private void SetStatusText(string status)
    {
        statusChanged?.Invoke(status ?? string.Empty);
    }


    /// <summary>
    /// process pending actions 
    /// </summary>
    private void ApplyPendingActions()
    {
        while (pendingActions.TryDequeue(out Action action))
        {
            action();
        }
    }

    /// <summary>
    /// get called when there is an incoming call
    /// </summary>
    /// <param name="callerName"></param>
    public void OnIncomingCall(string callerName)
    {
        if (handleIncomingCall.IsValidIncomingCall())
        {
            // if there is an active call, ignore the incoming call 
            if (currentActiveCall == null)
            {
                this.incomingCall?.Invoke(callerName);
            }
        }
    }

    /// <summary>
    /// join a coming call with audio only, no video
    /// </summary>
    public void AcceptIncomingCallWithAudio()
    {
        if (handleIncomingCall.IsValidIncomingCall())
        {
            handleIncomingCall.Accept();
            isSharedCamera = false;
            SetCurrentActiveCall(handleIncomingCall);
            incomingCallAccepted?.Invoke();
        }
    }

    /// <summary>
    /// join an incoming call with audio and video
    /// </summary>
    public void AcceptIncomingCallWithVideo()
    {
        if (handleIncomingCall.IsValidIncomingCall())
        {
            handleIncomingCall.Accept();
            isSharedCamera = true;
            SetCurrentActiveCall(handleIncomingCall);
            incomingCallAccepted?.Invoke();
        }
    }

    /// <summary>
    /// reject an incoming call
    /// </summary>
    public void RejectIncomingCall()
    {
        if (handleIncomingCall.IsValidIncomingCall())
        {
            handleIncomingCall.Reject();
        }
    }


    /// <summary>
    /// enable caption
    /// </summary>
    public async void EnableCaption()
    {
        if (currentActiveCall != teamMeeting)
        {
            return;
        }

        try
        {
            await teamMeeting.EnableCaption();
        }
        catch (Exception ex)
        {
            Log.Error<MeetingManager>("Failed to enable caption. {0}", ex);
        }
    }

    /// <summary>
    /// disable caption
    /// </summary>
    public void DisableCaption()
    {
        if (currentActiveCall != teamMeeting)
        {
            return;
        }
        teamMeeting.StopCaption();
    }

    /// <summary>
    /// set current active call
    /// </summary>
    /// <param name="activeCall"></param>
    private void SetCurrentActiveCall(CallScenario activeCall)
    {
        if (currentActiveCall != null)
        {
            currentActiveCall.Muted.RemoveListener(OnCurrentCallMuted);
            currentActiveCall.Unmuted.RemoveListener(OnCurrentCallUnmuted);
            currentActiveCall.VideoCaptureStarted.RemoveListener(OnCurrentCallVideoCaptureStarted);
            currentActiveCall.VideoCaptureEnded.RemoveListener(OnCurrentCallVideoCaptureEnded);
        }

        currentActiveCall = activeCall;

        if (currentActiveCall != null)
        {
            currentActiveCall.Muted.AddListener(OnCurrentCallMuted);
            currentActiveCall.Unmuted.AddListener(OnCurrentCallUnmuted);
            currentActiveCall.VideoCaptureStarted.AddListener(OnCurrentCallVideoCaptureStarted);
            currentActiveCall.VideoCaptureEnded.AddListener(OnCurrentCallVideoCaptureEnded);
        }

        UpdateMicrophone();
        UpdateSpeaker();
        UpdateCamera();
    }

    /// <summary>
    /// return true if the current active call is a Teams meeting 
    /// </summary>
    /// <returns></returns>
    public bool IsCurrentActiveCallTeamsMeeting()
    {
        return currentActiveCall == teamMeeting;
    }

    public void OnCallStateChanged(CallState state)
    {
        switch (state)
        {
            case CallState.Connecting:
                status = MeetingStatus.LoggedIn(MeetingCallState.Connecting);
                break;
            case CallState.Connected:
                status = MeetingStatus.LoggedIn(MeetingCallState.Connected);
                break;
            case CallState.Disconnecting:
                status = MeetingStatus.LoggedIn(MeetingCallState.Disconnecting);
                break;
            case CallState.Disconnected:
                status = MeetingStatus.LoggedIn(MeetingCallState.Disconnected);
                break;
            case CallState.None:
                status = MeetingStatus.LoggedIn(MeetingCallState.None);
                break;
            case CallState.Ringing:
                status = MeetingStatus.LoggedIn(MeetingCallState.Ringing);
                break;
        }
        ApplyStatus(status);
    }

    private void OnCurrentCallMuted()
    {
        ApplyIsMuted(true);
    }

    private void OnCurrentCallUnmuted()
    {
        ApplyIsMuted(false);
    }

    private void OnCurrentCallVideoCaptureStarted()
    {
        videoCaptureStarted?.Invoke();        
    }

    private void OnCurrentCallVideoCaptureEnded()
    {
        videoCaptureEnded?.Invoke();
    }
}

[Serializable]
public class MeetingMaanagerStatusStringEvent : UnityEvent<string>
{
}

[Serializable]
public class MeetingManagerIncomingMeetingCallEvent : UnityEvent<string>
{
    public string CallerName;
}



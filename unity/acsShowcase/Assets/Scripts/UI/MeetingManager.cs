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
    private CallScenario currentActiveScenario = null;

    /// <summary>
    /// current call agent shared with scenarios 
    /// </summary>
    private CallAgent currentCallAgent = null;

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
    /// OnEnable
    /// </summary>
    private void OnEnable()
    {
        ParticipantPanelController.OnAddParticipant += ParticipantPanelControllerOnAddParticipant;
        ParticipantPanelController.OnRemoveParticipant += ParticipantPanelControllerOnRemoveParticipant;
        handleIncomingCall.IncomingCallFromChanged.AddListener(OnIncomingCall);
    }

    /// <summary>
    /// OnDisable
    /// </summary>
    private void OnDisable()
    {
        ParticipantPanelController.OnAddParticipant -= ParticipantPanelControllerOnAddParticipant;
        ParticipantPanelController.OnRemoveParticipant -= ParticipantPanelControllerOnRemoveParticipant;
        handleIncomingCall.IncomingCallFromChanged.RemoveListener(OnIncomingCall);
    }

    /// <summary>
    /// Update
    /// </summary>
    public void Update()
    {
        ApplyPendingActions();
    }

    /// <summary>
    /// On destroying this component, sign out of the meeting services.
    /// </summary>
    private void OnDestroy()
    {
        SignOut();
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
    /// <param guestName="locator"></param>
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
                Log.Verbose<MeetingManager>("Token: {0}", teamMeeting.Token);
                Log.Verbose<MeetingManager>("Team URL: {0}", teamLocator.Url);
                teamMeeting.JoinUrl = teamLocator.Url;
                teamMeeting.Join(displayName, forceSignInAsGuest);
            }

            SetCurrentActiveCallScenario(teamMeeting);
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
        if (currentActiveScenario != null)
            return currentActiveScenario.AddParticipant(newUser.CreateIdentifier());
        else
            return null;
    }

    /// <summary>
    /// remove a participant from current team call
    /// </summary>
    public async Task RemoveParticipant(RemoteParticipant identifier)
    {
        if (currentActiveScenario != null)
        {
            await currentActiveScenario.RemoveParticipant(identifier);
        }
    }

    /// <summary>
    /// get called when removing participant 
    /// </summary>
    /// <param guestName="participant"></param>
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
    /// <param guestName="participant"></param>
    private void ParticipantPanelControllerOnAddParticipant(GraphUser person)
    {
        AddParticipant(person);
    }


    /// <summary>
    /// use for 1-1 calling, ACS SDK not yet supporting calling to Teams user. 
    /// </summary>
    /// <param guestName="user"></param>
    public void Create(UserIdentity user)
    {
        if (user == null)
        {
            Log.Error<MeetingManager>("Can't create call. Invalid user");
        }
        else
        {
            Log.Warning<MeetingManager>("1:1 calling is not yet implemented by this sample.");
        }
    }

    /// <summary>
    /// set shared camera
    /// </summary>
    public void SetShareCamera(bool enable)
    {
        if (enable)
        {
            ShareCamera();
        }
        else
        {
            UnshareCamera();
        }
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
        if (currentActiveScenario != null)
        {
            if (isSharedCamera)
            {
                currentActiveScenario.ShareCamera();
            }
            else
            {
                currentActiveScenario.UnShareCamera();
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
        if (currentActiveScenario != null)
        {
            if (isMute)
            {
                currentActiveScenario.Mute();
            }
            else
            {
                currentActiveScenario.Unmute();
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
        if (currentActiveScenario != null)
        {
            if (isSpeakerOff)
            {
                currentActiveScenario.MuteSpeaker();
            }
            else
            {
                currentActiveScenario.UnmuteSpeaker();
            }
        }
    }


    /// <summary>
    /// leave a current call
    /// </summary>
    public void Leave()
    {
        if (currentActiveScenario != null)
        {
            currentActiveScenario.Leave();
        }
    }

    /// <summary>
    /// login/authenticate and join a meeting 
    /// </summary>
    /// <param guestName="joinLocator"></param>
    /// <param guestName="createCallWithUser"></param>
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
        ApplyStatus(MeetingStatus.NoCall(MeetingAuthenticationState.LoggingIn));

        Log.Verbose<MeetingManager>("LoginWorker");

        bool obtainedAuthenticationTokens = false;
        if (await RequestAzureTokens(cancellationToken) && !cancellationToken.IsCancellationRequested)
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
        if (obtainedAuthenticationTokens && !cancellationToken.IsCancellationRequested)
        { 
            loggedIntoMeetingServices = await LogIntoMeetingServices(cancellationToken);
        }

        bool createdCallAgent = false;
        if (loggedIntoMeetingServices && !cancellationToken.IsCancellationRequested)
        {
            createdCallAgent = await CreateCallAgent(cancellationToken);
        }

        if (cancellationToken.IsCancellationRequested)
        {
            Log.Verbose<MeetingManager>("Log in cancelled.");
        }
        else if (!createdCallAgent)
        {
            Log.Error<MeetingManager>("Log in failed.");
            IsLoggedIn = false;
            Status = MeetingStatus.LoginError();
        }
        else
        { 
            Log.Verbose<MeetingManager>("Logged in.");
            IsLoggedIn = true;
            Status = MeetingStatus.NoCall(MeetingAuthenticationState.LoggedIn);

            teamMeeting.Token = serviceIdentity.CommunicationAccessToken;
            handleIncomingCall.Token = serviceIdentity.CommunicationAccessToken;

            teamMeeting.CallAgent = currentCallAgent;
            handleIncomingCall.CallAgent = currentCallAgent;
        }

        return IsLoggedIn;
    }

    /// <summary>
    /// Log into meeting services.
    /// </summary>
    private async Task<bool> LogIntoMeetingServices(CancellationToken cancellationToken)
    {
        Log.Verbose<MeetingManager>("Logging into meeting services");

        string guestName = string.Empty;
        if (graphAccessTokenResponse.IsValid())
        {
            try
            {
                var user = await User.Get(graphAccessTokenResponse.Token);
                guestName = user.displayName;
            }
            catch (Exception ex)
            {
                Log.Error<MeetingManager>("Microsoft Graph reques failed. Failed to find a name to use when joining without a Teams account. {0}", ex);
            }
        }

        if (string.IsNullOrEmpty(guestName) && !cancellationToken.IsCancellationRequested)
        {
            try
            {
                guestName = await SystemUser.GetName();
            }
            catch (Exception ex)
            {
                Log.Error<MeetingManager>("Failed to find a name to use when joining without a Teams account. {0}", ex);
            }
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
            guestName = string.IsNullOrEmpty(guestName) ? "Guest" : guestName
        };

        if (!cancellationToken.IsCancellationRequested)
        {
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
        }

        return serviceIdentity != null && !cancellationToken.IsCancellationRequested;
    }

    /// <summary>
    /// Logout worker 
    /// </summary>
    private void LogoutWorker()
    {
        _ = CancelPreviousLoginAndCreateNewCancellationToken();

        Leave();
        communicationAzureActiveDirectoryAccessTokenResponse = TokenResponse.Invalid;
        functionAppAccessTokenResponse = TokenResponse.Invalid;
        graphAccessTokenResponse = TokenResponse.Invalid;
        serviceIdentity = null;
        teamMeeting.Token = null;
        handleIncomingCall.Token = null;
        teamMeeting.CallAgent = null;
        handleIncomingCall.CallAgent = null;
        SetCurrentActiveCallScenario(null);
        currentCallAgent?.Dispose();
        currentCallAgent = null;
        IsLoggedIn = false;
        ApplyStatus(MeetingStatus.LoggedOut());
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

    private async Task<bool> CreateCallAgent(CancellationToken cancellationToke)
    {
        if (currentCallAgent != null)
        {   
            Log.Verbose<MeetingManager>("Call agent already exists.");
            return true;
        }

        var callClient = CallClientHost.Instance.CallClient;
        var credential = new CallTokenCredential(serviceIdentity.CommunicationAccessToken);

        var callAgentOptions = new CallAgentOptions()
        {
            DisplayName = serviceIdentity.LocalParticipant.DisplayName,
            EmergencyCallOptions = new EmergencyCallOptions()
            {
                CountryCode = "US"
            }
        };

        CallAgent agent = null;
        try
        {
            Log.Verbose<MeetingManager>("Creting new call agent.");
            agent = await callClient.CreateCallAgent(credential, callAgentOptions);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to create call agent. Exception: {ex}");
        }

        if (!cancellationToke.IsCancellationRequested)
        {
            Log.Verbose<MeetingManager>("Saving new call agent.");
            currentCallAgent = agent;
        }

        return currentCallAgent != null && !cancellationToke.IsCancellationRequested;
    }

    /// <summary>
    /// apply status 
    /// </summary>
    private void ApplyStatus(MeetingStatus status)
    {
        SetStatusText(status.CreateDisplayString());

        if (Status != status)
        {
            Log.Verbose<MeetingManager>("Status changing {0}->{1}", Status, status);
            Status = status;

            // Handle call state changes
            if (Status.AuthenticationState == MeetingAuthenticationState.LoggedIn)
            {
                HandleCallStateChanges(Status.CallState);
            }
            else
            {
                HandleCallStateChanges(MeetingCallState.NoCall);
            }
            Log.Verbose<MeetingManager>("Status changed {0}", Status);
        }
    }

    private void HandleCallStateChanges(MeetingCallState newCallState)
    {
        if (newCallState != lastCallState)
        {
            Log.Verbose<MeetingManager>("Call state changing {0}->{1}", lastCallState, newCallState);
            lastCallState = newCallState;
            switch (lastCallState)
            {
                case MeetingCallState.Connected:
                    HandleConnected();
                    break;

                case MeetingCallState.NoCall:
                case MeetingCallState.Disconnected:
                    HandleDisconnected();
                    break;

                case MeetingCallState.Connecting:
                    HandlingConnecting();
                    break;

                case MeetingCallState.Disconnecting:
                case MeetingCallState.Unknown:
                case MeetingCallState.EarlyMedia:
                case MeetingCallState.Ringing:
                case MeetingCallState.InLobby:
                    Log.Verbose<MeetingManager>("Ingnoring call state {0}", lastCallState);
                    break;

                default:
                    Log.Warning<MeetingManager>("Unhandled call state {0}", lastCallState);
                    break;
            }
            Log.Verbose<MeetingManager>("Call state changed {0}", lastCallState);
        }
    }

    private void HandleConnected()
    {
        Debug.Assert(currentActiveScenario != null, "Current active scenario should not be null if connected to a call.");
        joinedCall?.Invoke();
    }

    private void HandleDisconnected()
    {
        if (currentActiveScenario != null)
        {
            Leave();
            SetCurrentActiveCallScenario(null);
            leftCall?.Invoke();
        }
    }

    private void HandlingConnecting()
    {
        joiningCall?.Invoke();
    }


    /// <summary>
    /// apply mute 
    /// </summary>
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
    public void OnIncomingCall(string callerName)
    {
        if (handleIncomingCall.IsValidIncomingCall())
        {
            // if there is an active call, ignore the incoming call 
            if (currentActiveScenario == null)
            {
                incomingCall?.Invoke(callerName);
            }
            else
            {
                RejectIncomingCall();
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
            SetCurrentActiveCallScenario(handleIncomingCall);
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
            SetCurrentActiveCallScenario(handleIncomingCall);
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
        if (currentActiveScenario != teamMeeting)
        {
            Log.Verbose<MeetingManager>("Ignoring request to enable captions. Not in a Teams meeting.");
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
        if (currentActiveScenario != teamMeeting)
        {
            Log.Verbose<MeetingManager>("Ignoring request to disable captions. Not in a Teams meeting.");
            return;
        }

        teamMeeting.StopCaption();
    }

    /// <summary>
    /// set current active call
    /// </summary>
    /// <param guestName="activeCall"></param>
    private void SetCurrentActiveCallScenario(CallScenario activeCall)
    {
        if (currentActiveScenario != null)
        {
            currentActiveScenario.Muted.RemoveListener(OnCurrentCallMuted);
            currentActiveScenario.Unmuted.RemoveListener(OnCurrentCallUnmuted);
            currentActiveScenario.VideoCaptureStarted.RemoveListener(OnCurrentCallVideoCaptureStarted);
            currentActiveScenario.VideoCaptureEnded.RemoveListener(OnCurrentCallVideoCaptureEnded);
            currentActiveScenario.StatusChanged.RemoveListener(OnCallStateChanged);
        }

        currentActiveScenario = activeCall;

        if (currentActiveScenario != null)
        {
            currentActiveScenario.Muted.AddListener(OnCurrentCallMuted);
            currentActiveScenario.Unmuted.AddListener(OnCurrentCallUnmuted);
            currentActiveScenario.VideoCaptureStarted.AddListener(OnCurrentCallVideoCaptureStarted);
            currentActiveScenario.VideoCaptureEnded.AddListener(OnCurrentCallVideoCaptureEnded);
            currentActiveScenario.StatusChanged.AddListener(OnCallStateChanged);
        }

        UpdateMicrophone();
        UpdateSpeaker();
        UpdateCamera();
    }

    /// <summary>
    /// return true if the current active call is a Teams meeting 
    /// </summary>
    /// <returns></returns>
    public bool IsCurrentActiveCallScenarioTeamsMeeting()
    {
        return currentActiveScenario == teamMeeting;
    }

    public void OnCallStateChanged(CallScenarioStateChangedEventArgs args)
    {
        if (args.Scenario != currentActiveScenario)
        {
            Log.Verbose<MeetingManager>("Ignoring call state change for inactive scenario.");
            return;
        }

        Debug.Assert(Status.AuthenticationState == MeetingAuthenticationState.LoggedIn, "Call state changes should only be received when logged in.");
        switch (args.State)
        {
            case CallState.Connecting:
                ApplyStatus(MeetingStatus.LoggedIn(MeetingCallState.Connecting));
                break;
            case CallState.Connected:
                ApplyStatus(MeetingStatus.LoggedIn(MeetingCallState.Connected));
                break;
            case CallState.Disconnecting:
                ApplyStatus(MeetingStatus.LoggedIn(MeetingCallState.Disconnecting));
                break;
            case CallState.Disconnected:
                ApplyStatus(MeetingStatus.LoggedIn(MeetingCallState.Disconnected));
                break;
            case CallState.None:                 
                ApplyStatus(MeetingStatus.LoggedIn(args.Call == null ? MeetingCallState.NoCall : MeetingCallState.Unknown));
                break;
            case CallState.Ringing:
                ApplyStatus(MeetingStatus.LoggedIn(MeetingCallState.Ringing));
                break;
            case CallState.InLobby:
                ApplyStatus(MeetingStatus.LoggedIn(MeetingCallState.InLobby));
                break;
            case CallState.EarlyMedia:
                ApplyStatus(MeetingStatus.LoggedIn(MeetingCallState.EarlyMedia));
                break; 
            case CallState.LocalHold:
                ApplyStatus(MeetingStatus.LoggedIn(MeetingCallState.LocalHold));
                break;
            case CallState.RemoteHold:
                ApplyStatus(MeetingStatus.LoggedIn(MeetingCallState.RemoteHold));
                break;
            default:
                Log.Warning<MeetingManager>($"Unknown call state change {args.State}. Ignoring");
                break;
        }
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



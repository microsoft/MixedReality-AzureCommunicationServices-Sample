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

    [SerializeField] [Tooltip("The display name to use when signing into Teams as a guest.")]
    private string displayName = null;

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
    /// started 
    /// </summary>
    private bool started = false;

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
    /// pending join 
    /// </summary>
    private MeetingLocator pendingJoin = null;

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
    /// OnEnable
    /// </summary>
    private void OnEnable()
    {
        leftCall?.Invoke();
        ParticipantPanelController.OnAddParticipant += ParticipantPanelControllerOnAddParticipant;
        ParticipantPanelController.OnRemoveParticipant += ParticipantPanelControllerOnRemoveParticipant;

        started = true;
        displayName = "";
        LogInAndJoinOrCreateMeetingIfPossible(pendingJoin, createCallWithUser: null);
    }

    /// <summary>
    /// OnDisable
    /// </summary>
    private void OnDisable()
    {
        loggedIn = false;
        displayName = "";
    }

    /// <summary>
    /// OnDestroy
    /// </summary>
    private async void OnDestroy()
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
            if (currentActiveCall is not null)
            {
                // wait for previous call agent to be destroyed before start listening 
                if (currentActiveCall.CurrentCallAgent == null)
                {
                    handleIncomingCall.StartListening(displayName, forceSignInAsGuest);
                    SetCurrentActiveCall(null);
                    startListeningComingCall = false;
                }
            }
        }
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
                teamMeeting.Join(displayName);
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
        if (currentActiveCall is not null)
            return currentActiveCall.AddParticipant(newUser.CreateIdentifier());
        else
            return null;
    }

    /// <summary>
    /// remove a participant from current team call
    /// </summary>
    public async Task RemoveParticipant(RemoteParticipant identifier)
    {
        if (currentActiveCall is not null)
            await currentActiveCall.RemoveParticipant(identifier);
    }

    /// <summary>
    /// get called when removing participant 
    /// </summary>
    /// <param name="participant"></param>
    private void ParticipantPanelControllerOnRemoveParticipant(RemoteParticipant participant)
    {
        RemoveParticipant(participant);
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
            //LogInAndJoinOrCreateMeetingIfPossible(joinLocator: null, user);
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
        if (currentActiveCall is not null)
        {
            if (isSharedCamera)
                currentActiveCall.ShareCamera();
            else
                currentActiveCall.UnShareCamera();
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
        if (currentActiveCall is not null)
        {
            if (isMute)
                currentActiveCall.Mute();
            else
                currentActiveCall.Unmute();
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
        if (currentActiveCall is not null)
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
    public async void Leave()
    {
        if (currentActiveCall is not null)
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
    private async void LogInAndJoinOrCreateMeetingIfPossible(MeetingLocator joinLocator, UserIdentity createCallWithUser)
    {
        var loggedIdTask = loggedInTask;
        if (started && (loggedIdTask == null || !loggedIn))
        {
            loggedInTask = loggedIdTask = LoginWorker();
        }

        if (loggedIdTask == null)
        {
            Log.Verbose<MeetingManager>("Waiting to join meeting ({0})", joinLocator);
            pendingJoin = joinLocator;
        }
        else if (await loggedIdTask)
        {
            Log.Verbose<MeetingManager>("Login completes");
            if (joinLocator != null)
            {
                //await JoinMeetingIfNotNull(joinLocator);
            }
            else if (createCallWithUser != null)
            {
                //await CreateMeetingIfNotNull(createCallWithUser);
            }
        }
        else if (joinLocator != null)
        {
            Log.Error<MeetingManager>("Log in failed. Can't join meeting ({0})", joinLocator);
        }
        else if (createCallWithUser != null)
        {
            Log.Error<MeetingManager>("Log in failed. Can't create meeting ({0})", createCallWithUser);
        }
    }

    /// <summary>
    /// login worker task 
    /// </summary>
    /// <returns></returns>
    private async Task<bool> LoginWorker()
    {
        CancellationToken cancellationToken = CancelPreviousLoginAndCreateNewCancellationToken();
        bool result;

        Log.Verbose<MeetingManager>("LoginWorker");

        // If can't login yet, try obtaining additional scoped access tokens.
        if (await AuthenticateAsync(cancellationToken) &&
            !cancellationToken.IsCancellationRequested)
        {
            communicationAzureActiveDirectoryAccessTokenResponse = communicationAzureActiveDirectoryAccess?.TokenResponse ?? TokenResponse.Invalid;
            functionAppAccessTokenResponse = functionAppAccess?.TokenResponse ?? TokenResponse.Invalid;
            graphAccessTokenResponse = graphAccess?.TokenResponse ?? TokenResponse.Invalid;
        }

        result = await Login(cancellationToken);

        if (cancellationToken.IsCancellationRequested)
        {
            Log.Verbose<MeetingManager>("Log in cancelled.");
            result = false;
        }
        else if (result)
        {
            Log.Verbose<MeetingManager>("Logged in.");
            IsLoggedIn = true;
            if (string.IsNullOrEmpty(displayName) || (displayName.Equals(SystemUser.UNKNOWN_USER) || displayName.ToLower() == "unknown user"))
            {
                displayName = displayName;
                if (ProfileGetter.Profile != null)
                    displayName = ProfileGetter.Profile.displayName;
            }

            // start listening to the incoming call
            handleIncomingCall.Token = serviceIdentity.CommunicationAccessToken;
            handleIncomingCall.StartListening(displayName, forceSignInAsGuest);
        }
        else
        {
            Log.Error<MeetingManager>("Log in failed.");
            IsLoggedIn = false;
        }

        return result;
    }

    /// <summary>
    /// Log into meeting services.
    /// </summary>
    public async Task<bool> Login(CancellationToken cancellationToken)
    {
        Log.Verbose<MeetingManager>("Logging in");

        Status = MeetingStatus.NoCall(MeetingAuthenticationState.LoggingIn);

        string name = displayName;
        try
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                name = await SystemUser.GetName();
                displayName = name;
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
            displayName = displayName
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
            Log.Error<MeetingManager>("Login failed. {0}", ex);
        }

        if (serviceIdentity == null || cancellationToken.IsCancellationRequested)
        {
            Status = MeetingStatus.LoginError();
        }
        else
        {
            Status = MeetingStatus.NoCall(MeetingAuthenticationState.LoggedIn);
        }

        return true;
    }


    /// <summary>
    /// logout worker 
    /// </summary>
    private async void LogoutWorker()
    {
        _ = CancelPreviousLoginAndCreateNewCancellationToken();
        IsLoggedIn = false;
    }

    /// <summary>
    /// authenticate 
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    private async Task<bool> AuthenticateAsync(CancellationToken cancellationToken)
    {
        var manager = authenticationManager;

        // If no authentication has been configured assume success
        if (manager == null)
        {
            return communicationAzureActiveDirectoryAccess == null && functionAppAccess == null;
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

                case MeetingCallState.None:
                case MeetingCallState.Disconnected:
                    leftCall?.Invoke();
                    break;

                default:
                    joiningCall?.Invoke();
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
            SetCurrentActiveCall(handleIncomingCall);
            incomingCallAccepted?.Invoke();
            UnshareCamera();
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
            currentActiveCall = handleIncomingCall;
            incomingCallAccepted?.Invoke();
            ShareCamera();
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
    public void EnableCaption()
    {
        if (currentActiveCall != teamMeeting) return;
        teamMeeting.EnableCaption();
    }

    /// <summary>
    /// disable caption
    /// </summary>
    public void DisableCaption()
    {
        if (currentActiveCall != teamMeeting) return;
        teamMeeting.StopCaption();
    }

    /// <summary>
    /// set current active call
    /// </summary>
    /// <param name="activeCall"></param>
    private void SetCurrentActiveCall(CallScenario activeCall)
    {
        currentActiveCall = activeCall;
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

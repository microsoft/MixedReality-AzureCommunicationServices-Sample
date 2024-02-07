// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Communication.Calling.UnityClient;
using System;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// An manual test scenario that configures Azure Communication Calling to listen for incoming calls.
/// When an incoming call is received the UI can then decide whether to accept or reject the most
/// recent call. This scenario only handles one incoming call at a time.
/// </summary>
public class HandleIncomingCall : CallScenario
{
    private IncomingCall incomingCall = null;
    
    private string caller = null;

    public string Token { get; set; }

    public string IncomingCallFrom
    {
        get => caller;

        private set
        {
            if (!string.IsNullOrEmpty(value))
            {
                caller = value;
                incomingCallFromChanged?.Invoke(caller);
            }
        }
    }

    public bool IsValidIncomingCall()
    {
        return (incomingCall is not null);
    }
    
    [SerializeField]
    [Tooltip("Event fired when the current caller has changed.")]
    private StringChangeEvent incomingCallFromChanged = new StringChangeEvent();


    protected override void ScenarioStarted()
    {
        incomingCallFromChanged?.Invoke(caller);
    }

    protected override void ScenarioDestroyed()
    {
        Leave();
        DestroyCallAgent();
    }

    public void StartListening(string displayName, bool isGuest)
    {
        CreateCallAgent(displayName, isGuest);
    }

    public void StopListening()
    {
        DestroyCallAgent();
    }

    public void Accept()
    {
        SingleAsyncRunner.QueueAsync(async () =>
        {
            var acceptCall = incomingCall;
            incomingCall = null;
            IncomingCallFrom = null;

            if (acceptCall != null)
            {
                var acceptCallOptions = new AcceptCallOptions()
                {
                    OutgoingAudioOptions = CreateOutgoingAudioOptions(),
                    IncomingAudioOptions = CreateIncomingAudioOptions(),
                    OutgoingVideoOptions = CreateOutgoingVideoOptions(),
                    IncomingVideoOptions = CreateIncomingVideoOptions()
                };

                await HangUpCurrentCall();
                CurrentCall = await acceptCall.AcceptAsync(acceptCallOptions);
            }
        });
    }

    public void Reject()
    {
        SingleAsyncRunner.QueueAsync(async () =>
        {
            var rejectCall = incomingCall;
            incomingCall = null;
            IncomingCallFrom = null;

            if (rejectCall != null)
            {
                await rejectCall.RejectAsync();
            }
        });
    }

    
    protected override void IncomingCall(IncomingCall call)
    {
        SingleAsyncRunner.QueueAsync(() =>
        {
            if (CurrentCall == null)
            {
                incomingCall = call;
                IncomingCallFrom = incomingCall?.CallerDetails.DisplayName;
                return Task.CompletedTask;
            }
            else
            {
                return call.RejectAsync();
            }
        });
    }

    private void CreateCallAgent(string displayName, bool isGuest)
    {
        SingleAsyncRunner.QueueAsync(async () =>
        {
            
            if (CurrentCallAgent != null)
            {
                return;
            }
            
            if (string.IsNullOrEmpty(displayName))
                displayName = "Test User";

            var callClient = CallClientHost.Instance.CallClient;
            var credential = new CallTokenCredential(Token);

            var callAgentOptions = new CallAgentOptions()
            {
                DisplayName = displayName,
                EmergencyCallOptions = new EmergencyCallOptions()
                {
                    CountryCode = "US"
                }
            };

            try
            {
                CurrentCallAgent = await callClient.CreateCallAgent(credential, callAgentOptions);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to create call agent. Exception: {ex}");
            }
        });
    }

    private void DestroyCallAgent()
    { 
        CurrentCallAgent = null;
    }

}

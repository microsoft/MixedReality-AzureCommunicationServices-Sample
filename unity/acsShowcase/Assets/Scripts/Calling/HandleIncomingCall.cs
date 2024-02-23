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

    public string IncomingCallFrom { get; private set; }

    public bool IsValidIncomingCall()
    {
        return (incomingCall != null);
    }
    
    [SerializeField]
    [Tooltip("Event fired when the current caller has changed.")]
    private StringChangeEvent incomingCallFromChanged = new StringChangeEvent();

    /// <summary>
    /// Event fired when the current caller has changed.
    /// </summary>
    public StringChangeEvent IncomingCallFromChanged => incomingCallFromChanged;


    protected override void ScenarioStarted()
    {
        if (!string.IsNullOrEmpty(IncomingCallFrom))
        {
            incomingCallFromChanged?.Invoke(IncomingCallFrom);
        }
    }

    protected override void ScenarioDestroyed()
    {
        Leave();
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
            var incomingCallFrom = call?.CallerDetails.DisplayName;

            if (CurrentCall == null &&
                incomingCallFromChanged != null &&
                string.IsNullOrEmpty(incomingCallFrom))
            {
                incomingCallFromChanged.Invoke(caller);
                return Task.CompletedTask;
            }
            else
            {
                return call.RejectAsync();
            }
        });
    }
}

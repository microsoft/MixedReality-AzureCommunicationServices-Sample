// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Communication.Calling.UnityClient;
using System;
using UnityEngine;

/// <summary>
/// An manual test scenario that creates a new Azure Communication's call with another ACS User.
/// This scenario currently only handles inviting a single user.
/// </summary>
public class StartCall : CallScenario
{
    public string Token { get; set; }

    /// <summary>
    /// The Identity to call
    /// </summary>
    public string CalleeIdentity { get; set; }

    protected override void ScenarioStarted()
    {
    }

    protected override void ScenarioDestroyed()
    {
        Leave();
    }

    public void Create()
    {
        if (CurrentCall != null)
        {
            return;
        }

        SingleAsyncRunner.QueueAsync(async () =>
        {          
            var callClient = CallClientHost.Instance.CallClient;
            var credential = new CallTokenCredential(Token);
            var communicationIdentifier = new UserCallIdentifier(CalleeIdentity);

            var callAgentOptions = new CallAgentOptions()
            {
                DisplayName = "Test User",
                EmergencyCallOptions = new EmergencyCallOptions()
                {
                    CountryCode = "US"
                }
            };

            var startCallOptions = new StartCallOptions()
            {
                OutgoingAudioOptions = CreateOutgoingAudioOptions(),
                IncomingAudioOptions = CreateIncomingAudioOptions(),
                OutgoingVideoOptions = CreateOutgoingVideoOptions(),
                IncomingVideoOptions = CreateIncomingVideoOptions()
            };

            if (CurrentCallAgent == null)
            {
                try
                {
                    CurrentCallAgent = await callClient.CreateCallAgent(credential, callAgentOptions);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to create call agent. Exception: {ex}");
                }
            }

            if (CurrentCallAgent != null)
            {
                try
                {
                    CurrentCall = await CurrentCallAgent.StartCallAsync(new CallIdentifier[] { communicationIdentifier }, startCallOptions);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to start call. Exception: {ex}");
                }
            }
        });
    }

    public void Leave()
    {
        SingleAsyncRunner.QueueAsync(async () =>
        {
            if (CurrentCall != null)
            {
                await CurrentCall.HangUpAsync(new HangUpOptions()
                {
                    ForEveryone = false
                });

                CurrentCall = null;
            }

            CurrentCallAgent = null;
            InvalidateStatus();
        });
    }
}
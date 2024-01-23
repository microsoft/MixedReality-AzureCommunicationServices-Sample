// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Communication.Calling.UnityClient;
using System;
using System.Linq;
using UnityEngine;

/// <summary>
/// An manual test scenario that joins a Azure Communication's group call by id.
/// </summary>
public class JoinGroup : CallScenario
{
    private string groupId = null;

    public string Token { get; set; }

    public string GroupId
    {
        get => groupId;

        set
        {
            if (groupId != value)
            {
                groupId = value;
                groupIdChanged?.Invoke(groupId);
            }
        }
    }

    [SerializeField]
    [Tooltip("Event fired when group id changes.")]
    private StringChangeEvent groupIdChanged = new StringChangeEvent();

    protected override void ScenarioStarted()
    {
    }

    protected override void ScenarioDestroyed()
    {
        Leave();
    }

    public void Join()
    {
        if (CurrentCall != null)
        {
            return;
        }

        Guid groupId;
        if (!Guid.TryParse(GroupId, out groupId))
        {
            groupId = Guid.NewGuid();
            GroupId = groupId.ToString();
        }

        SingleAsyncRunner.QueueAsync(async () =>
        {          
            var callClient = CallClientHost.Instance.CallClient;
            var credential = new CallTokenCredential(Token);
            var teamLocator = new GroupCallLocator(groupId);

            var callAgentOptions = new CallAgentOptions()
            {
                DisplayName = "Test User",
                EmergencyCallOptions = new EmergencyCallOptions()
                {
                    CountryCode = "US"
                }
            };

            var joinCallOptions = new JoinCallOptions()
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
                    CurrentCall = await CurrentCallAgent.JoinAsync(teamLocator, joinCallOptions);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to join call. Exception: {ex}");
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

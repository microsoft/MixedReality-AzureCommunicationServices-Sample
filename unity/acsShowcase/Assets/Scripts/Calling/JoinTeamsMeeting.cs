// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Communication.Calling.UnityClient;
using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// An manual test scenario that joins a Team's meeting as an ACS user.
/// </summary>
public class JoinTeamsMeeting : CallScenario
{
    public string Token { get; set; }

    public string JoinUrl { get; set; }
    
    [SerializeField] [Tooltip("Event occures when receiving new caption text")]
    private UnityEvent<string> onCaptionReceived = null;

    [SerializeField] [Tooltip("Configure your caption language")]
    private string captionLanguage = "en-us";

    private TeamsCaptions teamsCaptions = null;

    protected override void ScenarioStarted()
    {
    }

    protected override void ScenarioDestroyed()
    {
        Leave();
    }



    public void Join(string displayName, bool isGuest)
    {
        if (CurrentCall != null)
        {
            return;
        }

        SingleAsyncRunner.QueueAsync(async () =>
        {
            var callClient = CallClientHost.Instance.CallClient;
            var credential = new CallTokenCredential(Token);
            var teamLocator = new TeamsMeetingLinkLocator(JoinUrl);
            if (string.IsNullOrEmpty(displayName))
                displayName = "Test User";
            var callAgentOptions = new CallAgentOptions()
            {
                DisplayName = displayName,
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
                    if (isGuest)
                    {
                        CurrentCallAgent = await callClient.CreateCallAgentAsync(credential, callAgentOptions);
                    }
                    else
                    {
                        CurrentCallAgent = await callClient.CreateTeamsCallAgentAsync(credential);
                    }
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
                    if (CurrentCallAgent is TeamsCallAgent teamsCallAgent)
                    {
                        CurrentCall = await teamsCallAgent.JoinAsync(teamLocator, joinCallOptions);
                    }
                    else if (CurrentCallAgent is CallAgent acsCallAgent)
                    {
                        CurrentCall = await acsCallAgent.JoinAsync(teamLocator, joinCallOptions);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to join call. Exception: {ex}");
                }
            }
        });
    }

    private void OnCaptionsReceived(object sender, TeamsCaptionsReceivedEventArgs e)
    {
        string message = e.Speaker.DisplayName + ": " + e.SpokenText;
        pendingActions.Enqueue((() => onCaptionReceived?.Invoke(message)));
    }

    private void OnIsCaptionsEnabledChanged(object sender, PropertyChangedEventArgs e)
    {
    }
    
    public async Task EnableCaption()
    {
        CaptionsCallFeature captionsCallFeature = CurrentCall.Features.Captions;
        
        if (captionsCallFeature != null)
        {
            CallCaptions callCaptions = await captionsCallFeature.GetCaptionsAsync();
            if (callCaptions.CaptionsKind == CaptionsType.TeamsCaptions)
            {
                teamsCaptions = callCaptions as TeamsCaptions;
                teamsCaptions.CaptionsEnabledChanged += OnIsCaptionsEnabledChanged;
                teamsCaptions.CaptionsReceived += OnCaptionsReceived;

                var options = new StartCaptionsOptions
                {
                    SpokenLanguage = captionLanguage
                };
                try
                {
                    await teamsCaptions.StartCaptionsAsync(options);
                }
                catch (Exception ex)
                {
                    Debug.LogError("Cannot StartCaptionsAsync. Error: " + ex.Message);
                }
            }
        }
        else
        {
            Debug.LogError("captionsCallFeature == null");
        }

    }

    public async void StopCaption()
    {
        if (teamsCaptions == null) return;
        
        try
        {
            await teamsCaptions.StopCaptionsAsync();
        }
        catch (Exception ex)
        {
            Debug.LogError("Cannot stop caption. " + ex.Message);
        }
    }
    

}

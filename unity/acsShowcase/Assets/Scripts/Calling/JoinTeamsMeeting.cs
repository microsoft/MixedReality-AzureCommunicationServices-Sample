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

    public void Join()
    {
        if (CurrentCall != null)
        {
            return;
        }

        var currentCallAgent = CallAgent;
        if (CallAgent == null)
        {
            throw new InvalidOperationException("CallAgent is invalid.");
        }

        SingleAsyncRunner.QueueAsync(async () =>
        {
            var callClient = CallClientHost.Instance.CallClient;
            var credential = new CallTokenCredential(Token);
            var teamLocator = new TeamsMeetingLinkLocator(JoinUrl);

            var joinCallOptions = new JoinCallOptions()
            {
                OutgoingAudioOptions = CreateOutgoingAudioOptions(),
                IncomingAudioOptions = CreateIncomingAudioOptions(),
                OutgoingVideoOptions = CreateOutgoingVideoOptions(),
                IncomingVideoOptions = CreateIncomingVideoOptions()
            };

            if (currentCallAgent != null)
            {
                try
                {
                    if (CallAgent is TeamsCallAgent teamsCallAgent)
                    {
                        CurrentCall = await teamsCallAgent.JoinAsync(teamLocator, joinCallOptions);
                    }
                    else if (CallAgent is CallAgent acsCallAgent)
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
    
    /// <summary>
    /// Enable the Teams meeting captions.
    /// </summary>
    public async Task EnableCaption()
    {
        CaptionsCallFeature captionsCallFeature = CurrentCall?.Features?.Captions;        
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

    /// <summary>
    /// Stop the Teams meeting captions.
    /// </summary>
    public async void StopCaption()
    {
        var captionsToStop = teamsCaptions;
        teamsCaptions = null;

        if (captionsToStop != null)
        {
            try
            {
                await captionsToStop.StopCaptionsAsync();
            }
            catch (Exception ex)
            {
                Debug.LogError("Cannot stop caption. " + ex.Message);
            }
        }
    }
}

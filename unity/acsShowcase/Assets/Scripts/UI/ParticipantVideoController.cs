// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq; 
using Azure.Communication.Calling.UnityClient;
using MixedReality.Toolkit.SpatialManipulation;
using TMPro; 
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// This class manages the display of the incoming video. It only shows the video the current active speaker. Other inactive speakers' video are turned off for better performance 
/// </summary>
public class ParticipantVideoController : MonoBehaviour
{
    private enum SpeakerVideoVisibility
    {
        None = 0,
        HasStream = 1,
        NoStream = 2
    }


    [SerializeField] 
    [Tooltip("The active speaker name text display")]
    private TextMeshProUGUI speakerName;

    [SerializeField] 
    [Tooltip("The maximum idle time to switch active speaker")]
    private float maxIdleTime = 3;

    [SerializeField]
    [Tooltip("The user initial text display is shown when speaker has camera turned off")]
    private TextMeshProUGUI initials;

    [SerializeField] 
    [Tooltip("The user profile icon is shown when the speaker has camera turned off")]
    private RawImage profileIcon;

    [SerializeField] 
    [Tooltip("Reference to the parent of the profile icon to hide/show ")]
    private GameObject profileIconParent;

    [SerializeField]
    [Tooltip("The video canvas ")]
    private GameObject canvasObject;

    [SerializeField]
    [Tooltip("The video panel ")]
    private MeshRenderer videoPanel;

    [SerializeField]
    [Tooltip("initial position offset ")]
    private Vector3 initialPosOffset;

    [SerializeField]
    [Tooltip("The solver used to position the video.")]
    private Solver solver;

    [SerializeField]
    [Tooltip("The layout element that sizes the video.")]
    private RectTransform videoContainer; 

    [SerializeField]
    [Tooltip("The main panel to position the video during enablement.")]
    private GameObject mainPanel;
    
    /// <summary>
    /// all participants 
    /// </summary>
    private ParticipantRepeaterItem[] allParticipants = null;
    
    /// <summary>
    /// current spekaer 
    /// </summary>
    private ParticipantRepeaterItem currentSpeaker = null;
    
    /// <summary>
    /// current speaker initial 
    /// </summary>
    private string currentSpeakerInitials = null;
    
    /// <summary>
    /// keep track of last not speaker time to determine the current active speaker 
    /// </summary>
    private float[] lastNotSpeakTimes = null;

    /// <summary>
    /// last update time
    /// </summary>
    private float lastTimeCheck = 0;
     
    /// <summary>
    /// current active speaker index 
    /// </summary>
    private int curActiveSpeakerIndex = 0;

    /// <summary>
    /// The index of the speaker to restore as "active" when this component is re-enabled.
    /// </summary>
    private int restoreActiveSpeakerIndex = -1;

    /// <summary>
    /// active speaker video status 
    /// </summary>
    private SpeakerVideoVisibility activeSpeakerVideoStatus = SpeakerVideoVisibility.None;

    /// <summary>
    /// background color 
    /// </summary>
    private Color backgroundColor = Color.white;
    
    /// <summary>
    /// background color list 
    /// </summary>
    private List<Color> backgroundColorsList = new List<Color>() { new Color(170 / 255f, 1, 241 / 255f, 1), new Color(1, 136 / 255f, 145 / 255f, 1), new Color(238 / 255f, 160 / 255f, 1, 1) };
    
    /// <summary>
    /// initial color list 
    /// </summary>
    private List<Color> initialsTextColorsList = new List<Color>() { new Color(63 / 255f, 118 / 255f, 192 / 255f, 1), new Color(183 / 255f, 32 / 255f, 35 / 255f, 1), new Color(149 / 255f, 32 / 255f, 183 / 255f, 1) };

    /// <summary>
    /// Never allow the video to be larger than this size.
    /// </summary>
    private Vector3 initialVideoContainerSize;

    // Start is called before the first frame update
    void Start()
    {
        SetIconAndTextColors();

        if (videoContainer != null)
        {
            initialVideoContainerSize = videoContainer.sizeDelta;
        }
    }

    /// <summary>
    /// OnEnable 
    /// </summary>
    private void OnEnable()
    {
        if (mainPanel != null)
        {
            transform.position = mainPanel.transform.position + mainPanel.transform.TransformVector(initialPosOffset);
            transform.transform.rotation = mainPanel.transform.rotation;
        }

        StartCoroutine(EnableCanvas());
        SetActiveSpeaker(restoreActiveSpeakerIndex);
    }    

    /// <summary>
    /// OnDisable 
    /// </summary>
    private void OnDisable()
    {
        restoreActiveSpeakerIndex = curActiveSpeakerIndex;
        speakerName.text = "";
        SetIconVisibility(false);
        SetActiveSpeaker(-1);
        activeSpeakerVideoStatus = SpeakerVideoVisibility.None;
    }

    // Update is called once per frame
    void Update()
    {
        if (Time.time - lastTimeCheck > 1.0f)
        {
            lastTimeCheck = Time.time;
            if (allParticipants != null && allParticipants.Length > 0)
            {
                float currentSpeakersIdleTime = float.MaxValue;
                if (curActiveSpeakerIndex > 0 && curActiveSpeakerIndex < lastNotSpeakTimes.Length)
                {
                    currentSpeakersIdleTime = Time.time - lastNotSpeakTimes[curActiveSpeakerIndex];
                }

                CallVideoStream currentSpeakerStream = null;
                if (curActiveSpeakerIndex >= 0 && curActiveSpeakerIndex < allParticipants.Length)
                {
                    currentSpeakerStream = allParticipants[curActiveSpeakerIndex].VideoPlayer.Stream;
                }

                bool currentSpeakerIsScreenSharing = false;
                if (currentSpeakerStream != null && currentSpeakerStream.SourceKind != VideoStreamSourceKind.ScreenSharing)
                {
                    currentSpeakerIsScreenSharing = true;
                }

                int newSpeakerIndex = -1;
                for (int i = 0; i < allParticipants.Length; i++)
                {
                    if (allParticipants[i].RemoteParticipant.IsSpeaking)
                    {
                        lastNotSpeakTimes[i] = Time.time;

                        // Perfer to keep the last active speaker, otherwise perfer the participant with lowest index.
                        if (newSpeakerIndex < 0 || i == curActiveSpeakerIndex)
                        {
                            newSpeakerIndex = i;
                        }
                    }
                }

                // if the active speaker is sharing screen, do not switch 
                if (newSpeakerIndex >= 0 &&
                    newSpeakerIndex != curActiveSpeakerIndex && 
                    currentSpeakersIdleTime > maxIdleTime && 
                    !currentSpeakerIsScreenSharing)
                {
                    SetActiveSpeaker(newSpeakerIndex);
                    activeSpeakerVideoStatus = SpeakerVideoVisibility.None;
                }
            }
            else if (allParticipants == null || allParticipants.Length == 0)
            {
                SetActiveSpeaker(-1);
                activeSpeakerVideoStatus = SpeakerVideoVisibility.None;
            }
        }
        // check current active speaker video on/off 
        if (allParticipants != null && curActiveSpeakerIndex >= 0 && curActiveSpeakerIndex < allParticipants.Length)
        {
            var isSpeakerVideoOn = allParticipants[curActiveSpeakerIndex].VideoPlayer.Stream != null ?
                SpeakerVideoVisibility.HasStream :
                SpeakerVideoVisibility.NoStream;

            if (isSpeakerVideoOn != activeSpeakerVideoStatus)
            {
                activeSpeakerVideoStatus = isSpeakerVideoOn;
                SetIconVisibility(activeSpeakerVideoStatus != SpeakerVideoVisibility.HasStream);
            }
        } 
    }

    /// <summary>
    /// set all participants  
    /// </summary>
    /// <param name="participants"></param>
    public void SetAllParticipant(ParticipantRepeaterItem[] participants)
    {
        if (allParticipants == participants)
        {
            return;
        }

        if (allParticipants != null)
        {
            for (int i = 0; i < allParticipants.Length; i++)
            {
                allParticipants[i].VideoPlayer.StopStreaming();
            }
        }

        allParticipants = participants;
        lastNotSpeakTimes = null;

        if (allParticipants != null)
        {
            lastNotSpeakTimes = new float[allParticipants.Length];
            for (int i = 0; i < allParticipants.Length; i++)
            {
                lastNotSpeakTimes[i] = Time.time;
            }
        }

        // show video of the first participant 
        SetActiveSpeaker(0);
    }

    /// <summary>
    /// set active speaker index 
    /// </summary>
    /// <param name="newActiveSpeakerIndex"></param>
    private void SetActiveSpeaker(int newActiveSpeakerIndex)
    {
        if (curActiveSpeakerIndex == newActiveSpeakerIndex)
        {
            return;
        }

        if (allParticipants != null && curActiveSpeakerIndex >= 0 && curActiveSpeakerIndex < allParticipants.Length)
        {
            var oldSpeaker = allParticipants[curActiveSpeakerIndex];
            RemoveVideoSizeChangeHandlers(oldSpeaker);
            oldSpeaker.VideoPlayer.StopStreaming();
            oldSpeaker.VideoPlayer.AutoStart = false;
            oldSpeaker.VideoPlayer.VideoRenderer = null;
        }

        curActiveSpeakerIndex = newActiveSpeakerIndex;
        
        if (allParticipants != null && curActiveSpeakerIndex >= 0 && curActiveSpeakerIndex < allParticipants.Length)
        {
            var newSpeaker = allParticipants[curActiveSpeakerIndex];

            videoPanel.enabled = true;
            profileIconParent.gameObject.SetActive(true);
            speakerName.text = newSpeaker.DisplayName;
            initials.text = GetInitials(speakerName.text);

            newSpeaker.VideoPlayer.VideoRenderer = videoPanel;
            newSpeaker.VideoPlayer.AutoStart = true;
            AddVideoSizeChangeHandlers(newSpeaker);;
        }
        else
        {
            videoPanel.enabled = false;
            profileIconParent.gameObject.SetActive(false);
            speakerName.text = string.Empty;
            initials.text = string.Empty;
        }
    }

    /// <summary>
    /// Handle video size changes.
    /// </summary>
    /// <param name="participant"></param>
    private void AddVideoSizeChangeHandlers(ParticipantRepeaterItem participant)
    {
        if (participant != null)
        {
            participant.VideoPlayer.VideoSizeChanged.AddListener(VideoSizeChanged);

            if (participant.VideoPlayer.VideoFormat != null)
            {
                VideoSizeChanged(new Vector2(
                    participant.VideoPlayer.VideoFormat.Width,
                    participant.VideoPlayer.VideoFormat.Height));
            }
        }
    }

    /// <summary>
    /// Stop handling video size changes.
    /// </summary>
    /// <param name="participant"></param>
    private void RemoveVideoSizeChangeHandlers(ParticipantRepeaterItem participant)
    {
        if (participant != null)
        {
            participant.VideoPlayer.VideoSizeChanged.RemoveListener(VideoSizeChanged);
        }
    }

    /// <summary>
    /// wait for few frames so the video bounding box is calculated correctly before enable canvas
    /// otherwise we cannot adjust video size 
    /// </summary>
    /// <returns></returns>
    private IEnumerator EnableCanvas()
    {
        if (!canvasObject.activeSelf)
        {
            yield return null;
            yield return null; 
            canvasObject.SetActive(true);
        }
    }
    
    /// <summary>
    /// show/hide icon when video is not available 
    /// </summary>
    /// <param name="visible"></param>
    private void SetIconVisibility(bool visible)
    {   
        profileIconParent.gameObject.SetActive(visible);

        // reset aspect ratio
        if (visible)
        {
            VideoSizeChanged(initialVideoContainerSize);
        }

        var currentSpeakerID = string.Empty;
        if (allParticipants != null && curActiveSpeakerIndex >= 0 && curActiveSpeakerIndex < allParticipants.Length)
        {
            currentSpeakerID = allParticipants[curActiveSpeakerIndex].RemoteParticipant.Identifier.RawId;
        }

        bool iconFoundInRecentUsers = false;
        foreach(var recentUser in UserController.UserProfiles)
        {
            if(recentUser.Id == currentSpeakerID)
            { 
                if(recentUser.Icon != null)
                {
                    iconFoundInRecentUsers = true;
                    initials.gameObject.SetActive(false);
                    profileIcon.color = Color.white;
                    profileIcon.texture = recentUser.Icon; 
                } 
                break;
            }
        }
        if (!iconFoundInRecentUsers)
        {
            profileIcon.texture = null;
            profileIcon.color = backgroundColor;
            initials.gameObject.SetActive(true);
        }

    }
    
    /// <summary>
    /// Get user initial 
    /// </summary>
    /// <returns></returns>
    private static string GetInitials(string displaceName)
    {
        var firstChars = displaceName.Where((ch, index) => ch != ' '
            && (index == 0 || displaceName[index - 1] == ' '));
        return new String(firstChars.ToArray());
    }
    
    /// <summary>
    /// set user icon and initial text colors 
    /// </summary>
    private void SetIconAndTextColors()
    {
        var randomColor = UnityEngine.Random.Range(1, 3);
        backgroundColor = backgroundColorsList[randomColor];
        initials.color = initialsTextColorsList[randomColor];
    }
    /// <summary>
    /// Called when when video size changed
    /// </summary>
    /// <param name="newScale"></param>
    private void VideoSizeChanged(Vector2 newSize)
    {
        if (videoContainer == null)
        {
            return;
        }

        var scaleSize = newSize;

        if (newSize.x > newSize.y)
        {
            scaleSize.y = newSize.y * initialVideoContainerSize.x / newSize.x;
            scaleSize.x = initialVideoContainerSize.x;
        }
        else
        {
            scaleSize.x = newSize.x * initialVideoContainerSize.y / newSize.y;
            scaleSize.y = initialVideoContainerSize.y;
        }

        videoContainer.sizeDelta = scaleSize;
    }
}

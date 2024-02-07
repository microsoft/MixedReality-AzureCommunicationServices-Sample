// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq; 
using Azure.Communication.Calling.UnityClient;
using TMPro; 
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// This class manages the display of the incoming video. It only shows the video the current active speaker. Other inactive speakers' video are turned off for better performance 
/// </summary>
public class ParticipantVideoController : MonoBehaviour
{
    [SerializeField] [Tooltip("The active speaker name text display")]
    private TextMeshPro speakerName;
    [SerializeField] [Tooltip("The maximum idle time to switch active speaker")]
    private float maxIdleTime = 3;
    [SerializeField] [Tooltip("The user initial text display is shown when speaker has camera turned off")]
    private TextMeshProUGUI initials;
    [SerializeField] [Tooltip("The user profile icon is shown when the speaker has camera turned off")]
    private RawImage profileIcon;
    [SerializeField] [Tooltip("Reference to the parent of the profile icon to hide/show ")]
    private GameObject profileIconParent;
    [SerializeField] [Tooltip("The video canvas ")]
    private GameObject canvasObject;
    [SerializeField] [Tooltip("The video panel ")]
    private MeshRenderer videoPanel;
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject caption;
    [SerializeField] private RectTransform speakerNameRect;
    [SerializeField] private GameObject pinControl;
    
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
    /// active speaker video status 
    /// </summary>
    private int activeSpeakerVideoEnabledStatus = -1;
    
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


    // Start is called before the first frame update
    void Start()
    {
        SetIconAndTextColors();
    }

    /// <summary>
    /// OnEnable 
    /// </summary>
    private void OnEnable()
    {
        videoPanel.transform.position = mainPanel.transform.position; 
        videoPanel.transform.rotation = mainPanel.transform.rotation;
        VideoStreamPlayer.s_VideoSizeChangeEvent += VideoSizeChanged;
        StartCoroutine(EnableCanvas());
    }
    

    /// <summary>
    /// OnDisable 
    /// </summary>
    private void OnDisable()
    {
        speakerName.text = "";
        SetIconVisibility(false);
        activeSpeakerVideoEnabledStatus = -1;
        VideoStreamPlayer.s_VideoSizeChangeEvent -= VideoSizeChanged;
    }

    // Update is called once per frame
    void Update()
    {
        if (Time.time - lastTimeCheck > 1.0f)
        {
            lastTimeCheck = Time.time;
            if (allParticipants != null && allParticipants.Length > 0)
            {
                videoPanel.enabled = true;
                for (int i = 0; i < allParticipants.Length; i++)
                {
                    if (allParticipants[i].RemoteParticipant.IsSpeaking)
                    {
                        lastNotSpeakTimes[i] = Time.time;
                        if (i != curActiveSpeakerIndex && (Time.time - lastNotSpeakTimes[curActiveSpeakerIndex]) > maxIdleTime)
                        {
                            // if the active speaker is sharing screen, do not switch 
                            if (allParticipants[curActiveSpeakerIndex].VideoPlayer.Stream == null || 
                                allParticipants[curActiveSpeakerIndex].VideoPlayer.Stream.SourceKind != VideoStreamSourceKind.ScreenSharing)
                            {
                                SwitchSpeakerVideo(i);
                                activeSpeakerVideoEnabledStatus = -1;
                            }
                        }
                    }
                }
            }
            else if (allParticipants == null || allParticipants.Length == 0)
            {
                videoPanel.enabled = false;
                activeSpeakerVideoEnabledStatus = -1;
                profileIconParent.gameObject.SetActive(false);
                speakerName.text = "";
            }
        }
        // check current active speaker video on/off 
        if (allParticipants != null && curActiveSpeakerIndex >= 0 && curActiveSpeakerIndex < allParticipants.Length)
        {
            int isSpeakerVideoOn = allParticipants[curActiveSpeakerIndex].VideoPlayer.Stream != null? 1: 0;
            if (isSpeakerVideoOn != activeSpeakerVideoEnabledStatus)
            {
                activeSpeakerVideoEnabledStatus = isSpeakerVideoOn;
                SetIconVisibility(activeSpeakerVideoEnabledStatus != 1);
            }
        } 
    }

    /// <summary>
    /// set all participants  
    /// </summary>
    /// <param name="participants"></param>
    public void SetAllParticipant(ParticipantRepeaterItem[] participants)
    {
        allParticipants = participants;
        lastNotSpeakTimes = new float[allParticipants.Length];
        for (int i = 0; i < allParticipants.Length; i++)
        {
            lastNotSpeakTimes[i] = Time.time;
            if (i != 0)
                allParticipants[i].VideoPlayer.StopStreaming();
        }

        // show video of the first participant 
        SetActiveSpeaker(0);
    }

    /// <summary>
    /// switch the display video to the new active speaker's video 
    /// </summary>
    /// <param name="newActiveSpeakerIndex"></param>
    private void SwitchSpeakerVideo(int newActiveSpeakerIndex)
    {
        if (newActiveSpeakerIndex >= 0 && newActiveSpeakerIndex < allParticipants.Length)
        {
            allParticipants[curActiveSpeakerIndex].VideoPlayer.StopStreaming();
            allParticipants[curActiveSpeakerIndex].VideoPlayer.VideoRenderer = null;
            SetActiveSpeaker(newActiveSpeakerIndex);
        }
    }

    /// <summary>
    /// set active speaker index 
    /// </summary>
    /// <param name="newActiveSpeakerIndex"></param>
    private void SetActiveSpeaker(int newActiveSpeakerIndex)
    {
        curActiveSpeakerIndex = newActiveSpeakerIndex;
        if (curActiveSpeakerIndex >= 0 && curActiveSpeakerIndex < allParticipants.Length)
        {
            allParticipants[curActiveSpeakerIndex].VideoPlayer.VideoRenderer = gameObject.GetComponent<Renderer>();
            allParticipants[curActiveSpeakerIndex].VideoPlayer.StartStreaming();
            speakerName.text = allParticipants[curActiveSpeakerIndex].DisplayName;
            currentSpeakerInitials = GetInitials();
            initials.text = currentSpeakerInitials;
            Debug.Log("SetActiveSpeaker " + newActiveSpeakerIndex);
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
        //reset aspect ratio
        if(visible)
        {
            VideoSizeChanged(new Vector3(0.5f,0.4f,0.1f));
        }
        var currentSpeakerDisplayName = allParticipants[curActiveSpeakerIndex].RemoteParticipant.DisplayName;
        bool iconFoundInRecentUsers = false;
        foreach(var recentUser in UserController.UserProfiles)
        {
            if(recentUser.DisplayName == currentSpeakerDisplayName)
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
    private string GetInitials()
    {
        var name = allParticipants[curActiveSpeakerIndex].RemoteParticipant.DisplayName;
        var firstChars = name.Where((ch, index) => ch != ' '
                                                   && (index == 0 || name[index - 1] == ' '));
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
    private void VideoSizeChanged(Vector3 newScale)
    {
        float xRatio = newScale.x/this.transform.localScale.x;
        float yRatio = newScale.y/this.transform.localScale.y;
        float zRatio = newScale.z/this.transform.localScale.z;

        this.transform.localScale = newScale;
        //adjust the caption and speaker name, and the pin control size so that look good
        // (not squeeze, not stretched) with the video size
        caption.transform.localScale = new Vector3(caption.transform.localScale.x / xRatio,
            caption.transform.localScale.y / yRatio,
            caption.transform.localScale.z / zRatio);
        
        speakerNameRect.localScale = new Vector3(speakerNameRect.localScale.x / xRatio,
            speakerNameRect.localScale.y / yRatio,
            speakerNameRect.localScale.z / zRatio);
        
        pinControl.transform.localScale = new Vector3(pinControl.transform.localScale.x / xRatio,
            pinControl.transform.localScale.y / yRatio,
            pinControl.transform.localScale.z / zRatio);
    }
}

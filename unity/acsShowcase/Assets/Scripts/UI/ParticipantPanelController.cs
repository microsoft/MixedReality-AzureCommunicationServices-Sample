// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Azure.Communication.Calling.Unity.Rest;
using Azure.Communication.Calling.UnityClient;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// This class manages the the participant panel  
/// </summary>
public class ParticipantPanelController : MonoBehaviour
{
    [SerializeField] [Tooltip("The reference to call preview manager to get the attendee info")]
    private CallPreviewManager callPreviewManager;
    [SerializeField] [Tooltip("The reference to video controller to set the participants to control which participant video to be displayed")]
    private ParticipantVideoController videoController;
    [SerializeField] [Tooltip("The reference to participant list item to retrieve the participants")]
    private GameObject allParticipantListItem;
    [SerializeField] [Tooltip("in meeting header text to show the total number")]
    private TextMeshProUGUI inMeetingHeaderText;
    [SerializeField] [Tooltip("meeting panel")]
    private GameObject inMeetingPanel;
    [SerializeField] [Tooltip("not in meeting header text to show the total number")]
    private TextMeshProUGUI notInMeetingHeaderText;
    [SerializeField] [Tooltip("not inmeeting panel")]
    private GameObject notInMeetingPanel;
    [SerializeField] [Tooltip("attendee prefab UI")]
    private GameObject attendeePrefab;
    [SerializeField] [Tooltip("content fitter to refresh")]
    private ContentSizeFitter contentFitter;
    [SerializeField] [Tooltip("attendee status display")]
    private TextMeshProUGUI attendeeStatus;
    [SerializeField] [Tooltip("participant status display")]
    private TextMeshProUGUI participantStatus;
    [SerializeField] [Tooltip("remove participant button")]
    private GameObject removeParticipantButton;
    [SerializeField] [Tooltip("participant panel")]
    private GameObject participantsPanelView;
    [SerializeField] [Tooltip("meeting manager ")]
    private MeetingManager meetingManager;
    
    /// <summary>
    /// User controler 
    /// </summary>
    private UserController userControler;

    /// <summary>
    /// Add participant event
    /// </summary>
    public static event Action<GraphUser> OnAddParticipant;
    
    /// <summary>
    /// Remove participant event  
    /// </summary>
    public static event Action<RemoteParticipant> OnRemoveParticipant;


    private void Awake()
    {
        userControler = GameObject.FindObjectOfType<UserController>();
    }

    /// <summary>
    /// OnEnable
    /// </summary>
    private void OnEnable()
    {
        UserObject.OnSelectedParticipantsChanged += OnSelectedParticipantsChanged;
        UserObject.OnSelectedParticipantToAdd += UserObjectOnSelectedParticipantToAdd;
    }
    
    /// <summary>
    /// OnDisable
    /// </summary>
    private void OnDisable()
    {
        UserObject.OnSelectedParticipantsChanged -= OnSelectedParticipantsChanged;
        UserObject.OnSelectedParticipantToAdd -= UserObjectOnSelectedParticipantToAdd;
    }

    /// <summary>
    /// Update participant list 
    /// </summary>
    public void OnParticipantListUpdate()
    {
        StartCoroutine(OnParticipantListUpdateCoroutine());
    }

    /// <summary>
    /// Update participant list 
    /// </summary>
    /// <returns></returns>
    private IEnumerator OnParticipantListUpdateCoroutine()
    {
        yield return null;

        if (meetingManager.IsCurrentActiveCallTeamsMeeting())
        {
            if (attendeeStatus != null && participantStatus != null)
                participantStatus.text = attendeeStatus.text;
        }
        else
        {
            participantStatus.text = "";
        }
        
        if (inMeetingPanel != null && callPreviewManager != null)
        {
            var allInMeetingParticipants = allParticipantListItem.GetComponentsInChildren<ParticipantRepeaterItem>();
            videoController.SetAllParticipant(allInMeetingParticipants);

            // remove previous not in meeting participants
            var childrenInMeeting = new List<GameObject>();
            foreach (Transform child in inMeetingPanel.transform) childrenInMeeting.Add(child.gameObject);
            childrenInMeeting.ForEach(child => Destroy(child));

            List<AttendeeInfo> allAttendeeList = callPreviewManager.AllAttendeeInfos.ToList();

            
            // retrieve participant info from attendee list 
            inMeetingHeaderText.text = string.Format("In this meeting ({0})", allInMeetingParticipants.Length);
            foreach (var participantItem in allInMeetingParticipants)
            {
                //Debug.Log("ParticipantPanelController participant: " + participant.GetIdentifier().RawId);
                bool foundMatchAttendee = false;

                GameObject participant = Instantiate(attendeePrefab, inMeetingPanel.transform);

                UserObject userObjectParticipant = participant.gameObject.GetComponent<UserObject>();
                AttendeeInfo responseStatusParticipant = participant.gameObject.GetComponent<AttendeeInfo>();
                if (responseStatusParticipant != null)
                    responseStatusParticipant.ParentGameObject = participantItem.gameObject;
                if (userObjectParticipant != null)
                {
                    userObjectParticipant.SetName(participantItem.DisplayName);
                }

                foreach (var attendee in allAttendeeList)
                {
                    // found participant in attendee list  
                    if (participantItem.GetIdentifier().RawId.Equals(attendee.ID))
                    {
                        UserObject userObjectAttendee = attendee.gameObject.GetComponent<UserObject>();
                        AttendeeInfo responseStatusAttendee = attendee.gameObject.GetComponent<AttendeeInfo>();
                        if (userObjectParticipant != null && userObjectAttendee != null &&
                            responseStatusParticipant != null && responseStatusAttendee != null)
                        {
                            userObjectParticipant.Copy(userObjectAttendee);
                            userObjectParticipant.UserObjectPageType = PageType.Participants;
                            responseStatusParticipant.Copy(responseStatusAttendee);
                        }

                        foundMatchAttendee = true;
                        allAttendeeList.Remove(attendee);
                        break;
                    }
                }

                if (!foundMatchAttendee)
                {
                    UserProfile profile = userControler.GetUserProfileFromID(participantItem.RemoteParticipant.Identifier.RawId);
                    if (profile != null)
                    {
                        userObjectParticipant.SetVariablesAndUI(profile.Id, profile.Email, PageType.Participants, profile.DisplayName, profile.Icon, profile.Presence);
                        userObjectParticipant.UserObjectPageType = PageType.Participants;
                    }
                    else
                    {
                        userObjectParticipant.SetPresenceIcon(PresenceAvailability.Offline);
                        userObjectParticipant.SetProfileIcon(null);
                        responseStatusParticipant.SetStatus("Unknown");
                        userObjectParticipant.UserObjectPageType = PageType.Participants;    
                    }
                    
                }
            }


            if (notInMeetingPanel != null)
            {
                // remove previous not in meeting participants
                var children = new List<GameObject>();
                foreach (Transform child in notInMeetingPanel.transform) children.Add(child.gameObject);
                children.ForEach(child => Destroy(child));

                // show not in meeting participant for teams meeting only
                if (meetingManager.IsCurrentActiveCallTeamsMeeting())
                {
                    // at this point, allAttendeeList contains only the participant not yet joined the meeting
                    notInMeetingHeaderText.text = string.Format("Other invited ({0})", allAttendeeList.Count);
                    foreach (var attendee in allAttendeeList)
                    {
                        GameObject participant = Instantiate(attendeePrefab, notInMeetingPanel.transform);
                        UserObject userObjectParticipant = participant.GetComponent<UserObject>();
                        AttendeeInfo responseStatusParticipant = participant.gameObject.GetComponent<AttendeeInfo>();
                        UserObject userObjectAttendee = attendee.gameObject.GetComponent<UserObject>();
                        AttendeeInfo responseStatusAttendee = attendee.gameObject.GetComponent<AttendeeInfo>();
                        if (userObjectParticipant != null && userObjectAttendee != null &&
                            responseStatusParticipant != null && responseStatusAttendee != null)
                        {
                            userObjectParticipant.Copy(userObjectAttendee);
                            userObjectParticipant.UserObjectPageType = PageType.Participants;
                            responseStatusParticipant.Copy(responseStatusAttendee);
                        }

                        if (userObjectParticipant != null)
                            userObjectParticipant.SetInteractability(false);
                    }
                }
                else
                {
                    // at this point, allAttendeeList contains only the participant not yet joined the meeting
                    notInMeetingHeaderText.text = string.Format("Other invited ({0})", 0);
                }
            }
        }

        yield return null;
        if (contentFitter != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentFitter.gameObject.GetComponent<RectTransform>());
    }
    
    /// <summary>
    /// Event occurs when participant selection has changed 
    /// </summary>
    private void OnSelectedParticipantsChanged()
    {
        if (UserController.SelectedUserObjects.Count > 0 && participantsPanelView.activeSelf)
            removeParticipantButton.SetActive(true);
        else
            removeParticipantButton.SetActive(false);
    }

    /// <summary>
    /// Remove participant 
    /// </summary>
    public void RemoveParticipants()
    {
        List<ParticipantRepeaterItem> allInMeetingParticipants = new List<ParticipantRepeaterItem>();
        if (inMeetingPanel != null)
        {
            allInMeetingParticipants = inMeetingPanel.GetComponentsInChildren<ParticipantRepeaterItem>().ToList();
        }

        foreach (var user in UserController.SelectedUserObjects)
        {
            var attendeeInfo = user.gameObject.GetComponent<AttendeeInfo>();
            if (attendeeInfo != null && attendeeInfo.ParentGameObject != null)
            {
                var participantItem = attendeeInfo.ParentGameObject.GetComponent<ParticipantRepeaterItem>();
                if (participantItem != null)
                {
                    OnRemoveParticipant?.Invoke(participantItem.RemoteParticipant);
                    UserController.SelectedUserObjects.Remove(user);
                    GameObject.DestroyImmediate(user.gameObject);
                    removeParticipantButton.SetActive(false);
                    OnParticipantListUpdate();
                }
            }
        }

        if (UserController.SelectedUserObject != null)
        {
            UserController.SelectedUserObject.DeSelect();
            UserController.SelectedUserObject = null;
        }
    }

    /// <summary>
    /// Add participant 
    /// </summary>
    private void UserObjectOnSelectedParticipantToAdd()
    {
        if (participantsPanelView.activeSelf)
        {
            //Add via api
            OnAddParticipant?.Invoke(ConvertToIUser(UserController.SelectedUserObject));
            OnParticipantListUpdate();
            UserController.SelectedUserObject.DeSelect();
        }
    }

    /// <summary>
    /// Convert user object to graph user 
    /// </summary>
    /// <param name="userObject"></param>
    /// <returns></returns>
    public GraphUser ConvertToIUser(UserObject userObject)
    {
        GraphUser returnedUser = new GraphUser();
        returnedUser.id = userObject.Id;
        returnedUser.mail = userObject.Email;
        returnedUser.displayName = userObject.name;
        return returnedUser;
    }
}
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure.Communication.Calling.Unity;
using Azure.Communication.Calling.Unity.Rest;
using TMPro;
using UnityEngine;

/// <summary>
/// This class controls the window for a call preview, after clicking join from a meeting on the main panel.
/// </summary>
public class CallPreviewManager : AuthenticatedOperation
{
    [SerializeField] [Tooltip("The parent game object that contains the list of all the attendees")]
    private GameObject attendeeContainer;
    
    [SerializeField] [Tooltip("The attendee UI prefab that is used to instantiate new attendee")]
    private GameObject attendeePrefab;
    
    [SerializeField] [Tooltip("the meeting title text display")]
    private TextMeshProUGUI meetingTitle;

    [SerializeField] [Tooltip("The text display of all attendee status of the meeting")]
    private TextMeshProUGUI attendeeStatus;
    
    [SerializeField] [Tooltip("The button to remove attendee")]
    private GameObject removeAttendeeButton;
    
    [SerializeField]  [Tooltip("The call preview UI panel")]
    private GameObject callPreviewPanel;

    /// <summary>
    /// Current meeting 
    /// </summary>
    private EventView curMeeting = null;

    /// <summary>
    /// User controler 
    /// </summary>
    private UserController userControler;
    
    /// <summary>
    /// People getter, to get user info
    /// </summary>
    private PeopleGetter peopleGetter;
    
    /// <summary>
    /// Photo getter, to get user icon 
    /// </summary>
    private PhotoGetter photoGettter;
    
    /// <summary>
    /// Keep track of all the attendees 
    /// </summary>
    private List<AttendeeInfo> allAttendeeInfos = new List<AttendeeInfo>();
    public List<AttendeeInfo> AllAttendeeInfos
    {
        get { return allAttendeeInfos;  }
    }
    /// <summary>
    /// Join URL of the meeting 
    /// </summary>
    private string joinURL;
    
    /// <summary>
    /// Meeting ID 
    /// </summary>
    private string meetingID;

    
    /// <summary>
    /// Awake 
    /// </summary>
    private void Awake()
    {
        userControler = GameObject.FindObjectOfType<UserController>();
        peopleGetter = GameObject.FindObjectOfType<PeopleGetter>();
        photoGettter = GameObject.FindObjectOfType<PhotoGetter>();
    }

    /// <summary>
    /// OnEnable
    /// </summary>
    private void OnEnable()
    {
        UserObject.OnSelectedParticipantsChanged += OnSelectedAttendeesChanged;
        UserObject.OnSelectedParticipantToAdd += OnSelectedAddingAttendee;
    }

    /// <summary>
    /// OnDisable
    /// </summary>
    private void OnDisable()
    {
        UserObject.OnSelectedParticipantsChanged -= OnSelectedAttendeesChanged;
        UserObject.OnSelectedParticipantToAdd -= OnSelectedAddingAttendee;
        attendeeStatus.text = "";
    }

    /// <summary>
    /// Show the meeting info: meeting title, all attendees with name and respsonse status
    /// this meeting info is coming from MS graph
    /// </summary>
    /// <param name="meeting"></param>
    public async void ShowMeetingInfo(EventView meeting )
    {
        curMeeting = meeting;
        if (curMeeting.Data == null) return;
        if (meetingTitle != null)
        {
            meetingTitle.text = curMeeting.Data.subject;
        }
        joinURL = curMeeting.Data.onlineMeeting.joinUrl;
        var meetingInfo = await GetOnlineMeetingInfo();
        meetingID = meetingInfo.value.FirstOrDefault().id;

        if (attendeeContainer != null)
        {
            // remove previous meeting attendees 
            for (int i = 0; i < attendeeContainer.transform.childCount; i++)
            {
                Destroy(attendeeContainer.transform.GetChild(i).gameObject);
            }
        }

        if (curMeeting.Data.attendees == null) return;
        if (curMeeting.Data.attendees.value == null) return;
        if (userControler == null) return;

        int responseAccepted = 0;
        int responseDeclined = 0;
        int responseTentative = 0;
        var allUsers = gameObject.GetComponentsInChildren<UserObject>();
        
        allAttendeeInfos.Clear();
        
        //Fill the Relevant Contacts list
        List<string> notFoundProfileEmailList = new List<string>();
        foreach (var attendee in curMeeting.Data.attendees.value)
        {
            GameObject newAttendee = Instantiate(attendeePrefab,attendeeContainer.transform);
            AttendeeInfo attendeeInfo = newAttendee.GetComponent<AttendeeInfo>();
            attendeeInfo.SetInfo(attendee);
            allAttendeeInfos.Add(attendeeInfo);
            if (attendee.status.response == EventResponse.accepted)
            {
                responseAccepted++;
            }
            else if (attendee.status.response == EventResponse.declined)
            {
                responseDeclined++;
            }
            else
            {
                responseTentative++;
            }

            attendeeStatus.text = responseAccepted.ToString() + " Accepted, " + responseDeclined.ToString() + " Declined, " + responseTentative.ToString() + " Tentative";
             
            UserProfile profile = userControler.GetUserProfile(attendee.emailAddress.address);
            if (profile == null)
            {
                // this attendee is not found in the the most relevant contact list
                // we have to query their photo and status 
                notFoundProfileEmailList.Add(attendee.emailAddress.address);
                var userObject = newAttendee.GetComponent<UserObject>();
                if (userObject != null)
                {
                    userObject.SetVariablesAndUI("", "", PageType.Participants, attendee.emailAddress.address, null, PresenceAvailability.Offline); ;
                }
            }
            else
            {
                // this attendee is found in the most relevant contact list, get info from there
                var userObject = newAttendee.GetComponent<UserObject>();
                if (userObject != null)
                {
                    userObject.SetVariablesAndUI(profile.Id, profile.Email, PageType.Participants, profile.DisplayName, profile.Icon, profile.Presence);
                    attendeeInfo.ID = profile.Id;
                }
            }
        }
        
        // get user profile from email 
        if (notFoundProfileEmailList.Count > 0)
            peopleGetter.GetPeopleWorker(notFoundProfileEmailList, GetPeopleHandler);
        
    }

    /// <summary>
    /// Retrieve user profile from email address 
    /// </summary>
    /// <param name="userList"></param>
    private void GetPeopleHandler(IUsers userList)
    {
        // retrieve photos for these people who are not in relevant contact list 
        photoGettter.UpdateProfilesWorkerAsync(userList, OnAllPhotosLoaded);

    }

    /// <summary>
    /// Handler when attendees photo are loaded 
    /// </summary>
    /// <param name="usersProfile"></param>
    public void OnAllPhotosLoaded(List<StaticUserProfile> usersProfile)
    {
        if (usersProfile.Count == 0) return;
        var allAttendeeInfo = gameObject.GetComponentsInChildren<AttendeeInfo>();
        foreach (var profile in usersProfile)
        {
            foreach (var attendee in allAttendeeInfo)
            {
                if (string.Compare(attendee.Email, profile.Email, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    var userObject = attendee.GetComponent<UserObject>();
                    if (userObject != null)
                    {
                        userObject.SetVariablesAndUI(profile.Id, profile.Email, PageType.Participants, profile.DisplayName, profile.Icon, profile.Presence);
                        attendee.ID = profile.Id;
                    }   
                }
            }
        }
    } 
    /// <summary>
    /// Get called when attendee selection changes, if one or more attendees are selected
    /// the remove attendee button will be shown
    /// </summary>
    private void OnSelectedAttendeesChanged()
    {
        if (UserController.SelectedUserObjects.Count > 0 && callPreviewPanel.activeSelf)
            removeAttendeeButton.SetActive(true);
        else
            removeAttendeeButton.SetActive(false);
    }
    
    /// <summary>
    /// Remove selected attendees by updating meeting
    /// https://learn.microsoft.com/en-us/graph/api/onlinemeeting-update?view=graph-rest-1.0&tabs=http
    /// NOTE: this feature is not working properly. The change is updated accordingly and can be verified using graph API
    /// however the change does not reflect properly on the Teams app.   
    /// </summary>
    /// <returns></returns>
    public async Task<string> RemoveAttendeesAsync()
    {
        var requestBody = "{\r\n\"participants\": {\r\n\"attendees\": [";
        //Add non-removed attendees to the request body
        foreach (var attendee in allAttendeeInfos.ToList())
        {
            if (UserController.SelectedUserObjects.Where(user => attendee.Email == user.Email).Count() == 0)
            {
                requestBody += "\r\n\r\n\r\n{\"upn\": \"" + attendee.Email + "\"}\r\n,";
            }
            else
            {
                allAttendeeInfos.Remove(attendee);
            }
        }
        requestBody += "\r\n]\r\n}\r\n}";
        //Delete game objects of removed attendees 
        foreach (var user in UserController.SelectedUserObjects.ToList())
        { 
            //Remove via api  
            UserController.SelectedUserObjects.Remove(user);
            GameObject.DestroyImmediate(user.gameObject);
            removeAttendeeButton.SetActive(false); 
        }
        if (UserController.SelectedUserObject != null)
        {
            UserController.SelectedUserObject.DeSelect();
            UserController.SelectedUserObject = null;
        }

        //Call API to update attendees
        var url = "https://graph.microsoft.com/v1.0/me/onlineMeetings/";
        return await OnlineMeeting.Patch(Token, url, meetingID, requestBody);
         
    }
    /// <summary>
    /// Get online meeting info, used to retrieve meeting ID
    /// </summary>
    /// <returns></returns>
    public async Task<IOnlineMeetingResponse> GetOnlineMeetingInfo()
    {   
        //Call API to update attendees
        return await OnlineMeeting.Get(Token, joinURL);
    }
    
    /// <summary>
    /// Called when clicking on the remove button 
    /// </summary>
    public async void OnRemoveButtonClicked()
    {
        var task = RemoveAttendeesAsync();
        string removedAttendeeResponse = await task;  
    } 
    
    /// <summary>
    /// Add new attendees by updating meeting
    /// https://learn.microsoft.com/en-us/graph/api/onlinemeeting-update?view=graph-rest-1.0&tabs=http
    /// NOTE: this feature is not working properly. The change is updated accordingly and can be verified using graph API
    /// however the change does not reflect properly on the Teams app.   
    /// </summary>
    /// <returns></returns>
    private async void OnSelectedAddingAttendee()
    {
        if (callPreviewPanel.activeSelf)
        {
            bool alreadyAttending = false;
            var requestBody = "{\r\n\"participants\": {\r\n\"attendees\": [";
            foreach (var attendee in allAttendeeInfos)
            {
                requestBody += "\r\n\r\n\r\n{\"upn\": \"" + attendee.Email + "\"}\r\n,";
                //if user exists already do not add
                if (UserController.SelectedUserObjects.Where(user => attendee.Email == user.Email).Count() == 1)
                {
                    alreadyAttending = true;
                }
            }
            //user not in attending list
            if (!alreadyAttending)
            {
                requestBody += "\r\n\r\n\r\n{\"upn\": \"" + UserController.SelectedUserObject.Email + "\"}\r\n,";
            }
            requestBody += "\r\n]\r\n}\r\n}";

            GameObject newAttendee = Instantiate(attendeePrefab, attendeeContainer.transform);
            var userObject = newAttendee.GetComponent<UserObject>();
            var newAttendeeInfo = newAttendee.GetComponent<AttendeeInfo>();
            if (userObject != null)
            {
                userObject.SetVariablesAndUI(
                    UserController.SelectedUserObject.Id, 
                    UserController.SelectedUserObject.Email, 
                    PageType.Participants, 
                    UserController.SelectedUserObject.DisplayName, 
                    null, 
                    UserController.SelectedUserObject.Presence);

                newAttendeeInfo.Email = UserController.SelectedUserObject.Email;
                newAttendeeInfo.ID = UserController.SelectedUserObject.Id;
                newAttendeeInfo.ParentGameObject = newAttendee;
            } 

            //Call API to update attendees
            var url = "https://graph.microsoft.com/v1.0/me/onlineMeetings/";
            var returnValue = await OnlineMeeting.Patch(Token, url, meetingID, requestBody);

            allAttendeeInfos.Add(newAttendeeInfo);


            if (UserController.SelectedUserObject != null)
            {
                UserController.SelectedUserObject.DeSelect();
                UserController.SelectedUserObject = null;
            }

        }
    }

    /// <summary>
    /// Join this meeting 
    /// </summary>
    public void JoinMeeting()
    {
        if (curMeeting != null)
        {
            curMeeting.JoinMeeting();
        }
    }

    /// <summary>
    /// Called when the app is authenticated and has an authentication token.
    /// </summary>
    protected override void OnAuthenticated()
    {
    }
}

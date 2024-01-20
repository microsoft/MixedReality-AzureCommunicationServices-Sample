// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. 

using System;
using Azure.Communication.Calling.Unity;
using Azure.Communication.Calling.Unity.Rest;
using MixedReality.Toolkit;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;


/// <summary>
/// This class represent the meeting info
/// </summary>
public class EventView : RepeaterItem
{
    protected IEvent data = null;
    public IEvent Data => data;

    protected IEventActions actions = null;

    [SerializeField] [Tooltip("The display name text field")]
    protected TextMeshProUGUI displayName = null;

    [SerializeField] [Tooltip("The start time text field")]
    protected TextMeshProUGUI timeInterval = null;


    [SerializeField] [Tooltip("The name of the attendees text field")]
    protected TextMeshProUGUI attendeeName = null;


    [SerializeField] [Tooltip("The button used to start a call")]
    protected StatefulInteractable startButton = null;


    [SerializeField] [Tooltip("call preview manager")]
    private CurrentEventView curEventView = null;

    [SerializeField] [Tooltip("Upcoming meeting start interval")]
    private float upcomingMeetingStartInterval = 15f;

    [SerializeField] [Tooltip("Upcoming meeting end interval")]
    private float upcomingMeetingEndInterval = 5f;

    public UnityEvent<EventView> OnSelectEventHandler;

    private void Awake()
    {
        ClearFields();
    }

    
    /// <summary>
    /// clear all current selected users 
    /// </summary>
    public void ClearSelectedUsers()
    {
        UserController.ClearSelectedUserObjects();
    }

    /// <summary>
    /// update data source 
    /// </summary>
    /// <param name="oldValue"></param>
    /// <param name="newValue"></param>
    protected override void OnDataSourceChanged(object oldValue, object newValue)
    {
        data = newValue as IEvent;
        actions = newValue as IEventActions;
        UpdateFields();
        Layout();
    }

    /// <summary>
    /// update layout 
    /// </summary>
    private void Layout()
    {
        var layout = GetComponent<VerticalLayoutGroup>();
        if (layout != null)
        {
            layout.SetLayoutHorizontal();
            layout.SetLayoutVertical();
        }

        var sizer = GetComponent<ContentSizeFitter>();
        if (sizer != null)
        {
            sizer.SetLayoutHorizontal();
            sizer.SetLayoutVertical();
        }
    }

    /// <summary>
    /// clear the display 
    /// </summary>
    private void ClearFields()
    {
        if (displayName != null)
        {
            displayName.text = string.Empty;
        }

        if (timeInterval != null)
        {
            timeInterval.text = string.Empty;
        }

        if (attendeeName != null)
        {
            attendeeName.text = string.Empty;
        }
    }

    /// <summary>
    /// update meeting info, make it as incoming meeting if the condition matches 
    /// </summary>
    private void UpdateFields()
    {
        if (data is null) return;

        // ignore if this event is not Teams meeting 
        if (data.onlineMeetingProvider != EventOnlineMeetingProviderType.teamsForBusiness)
        {
            Destroy(this.gameObject);
            return;
        }

        if (displayName != null)
        {
            displayName.text = data.subject;
        }

        if (timeInterval != null)
        {
            var timeNow = DateTimeOffset.Now;
            IEventDateTime startMeeting = data.start;
            IEventDateTime endMeeting = data.end;
            var startFromNow = startMeeting.dateTimeOffset - timeNow;
            // check if this is upcoming meeting ? 
            if (startFromNow.TotalMinutes <= upcomingMeetingStartInterval)
            {
                var endFromNow = timeNow - endMeeting.dateTimeOffset;
                if (endFromNow.TotalMinutes <= upcomingMeetingEndInterval)
                {
                    curEventView?.OnDataSourceChanged(null, data);
                }
            }

            timeInterval.text = startMeeting.dateTimeOffset.ToLocalTime().ToString("MMM dd h:mm tt") + " - " + endMeeting.dateTimeOffset.ToLocalTime().ToString("h:mm tt");
        }

        if (attendeeName != null)
        {
            string allNames = string.Empty;
            if (data.organizer != null && data.attendees != null)
            {
                allNames = data.attendees.ToString();
            }

            attendeeName.text = allNames;
        }
    }

    /// <summary>
    /// join this meeting 
    /// </summary>
    public void JoinMeeting()
    {
        actions?.Join();
    }

    /// <summary>
    /// raise on select event 
    /// </summary>
    public void RaiseOnSelectEvent()
    {
        OnSelectEventHandler?.Invoke(this);
    }
}

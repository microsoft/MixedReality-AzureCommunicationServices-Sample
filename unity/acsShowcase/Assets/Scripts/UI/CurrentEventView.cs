// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. 

using System;
using Azure.Communication.Calling.Unity;
using Azure.Communication.Calling.Unity.Rest;
using UnityEngine;
using UnityEngine.UI;


/// <summary>
/// This class represents the current meeting event view
/// </summary>
public class CurrentEventView : EventView
{
    
    [SerializeField] [Tooltip("The upcoming meeting panel, to show/hide upcoming event")]
    private GameObject upcomingMeetingSubPanel;
    
    [SerializeField] [Tooltip("The no-upcoming meeting panel. to show/hide no upcoming event")]
    private GameObject noUpcomingMeetingSubPanel;

    private void Awake()
    {
        ClearFields();
        Reset();
    }

    /// <summary>
    /// update current meeting 
    /// </summary>
    /// <param name="oldValue"></param>
    /// <param name="newValue"></param>
    protected override void OnDataSourceChanged(object oldValue, object newValue)
    {
        IEvent newData = newValue as IEvent;

        if (data != null)
        {
            // if there is already upcoming meeting, ignore meeting that has ended 

            var timeNow = DateTimeOffset.Now;
            var startNewFromNow = newData.start.dateTimeOffset - timeNow;
            var endNewFromNow = timeNow - newData.end.dateTimeOffset;

            var startCurFromNow = data.start.dateTimeOffset - timeNow;
            var endCurFromNow = timeNow - data.end.dateTimeOffset;
            if (startCurFromNow.TotalMinutes > 0 && startCurFromNow.TotalMinutes < startNewFromNow.TotalMinutes)
            {
                // the new meeting will start later than current
                return;  
            }
            else if ( endCurFromNow.TotalMinutes < 0 )
            { 
                // current meeting has not ended
                return;
            }
        }

        data = newData;
        actions = newValue as IEventActions;
        UpdateFields();
        Layout();
    }

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
    /// display upcoming meeting info
    /// </summary>
    private void UpdateFields()
    {
        if (displayName != null && data != null)
        {
            displayName.text = data.subject;
        }
        
        if (timeInterval != null && !string.IsNullOrEmpty(displayName.text))
        {
            var timeNow = DateTimeOffset.Now;
            IEventDateTime startMeeting = data.start;
            IEventDateTime endMeeting = data.end;
            var startFromNow = startMeeting.dateTimeOffset - timeNow;
            if (startFromNow.TotalMinutes <= 0)
            {
                timeInterval.text = "now";
            }
            else
            {
                timeInterval.text = startMeeting.dateTimeOffset.ToLocalTime().ToString("MMM dd h:mm tt") + " - " + endMeeting.dateTimeOffset.ToLocalTime().ToString("h:mm tt");
            }

            if (upcomingMeetingSubPanel != null)
            {
                upcomingMeetingSubPanel.SetActive(true);
            }

            if (noUpcomingMeetingSubPanel != null)
            {
                noUpcomingMeetingSubPanel.SetActive(false);
            }
        }
        else
        {
            if (upcomingMeetingSubPanel != null)
            {
                upcomingMeetingSubPanel.SetActive(false);
            }

            if (noUpcomingMeetingSubPanel != null)
            {
                noUpcomingMeetingSubPanel.SetActive(true);
            }
        }

        if (attendeeName != null && data != null)
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
    /// clear the upcoming display 
    /// </summary>
    public void Reset()
    {
        if (upcomingMeetingSubPanel != null)
        {
            upcomingMeetingSubPanel.SetActive(false);
        }

        if (noUpcomingMeetingSubPanel != null)
        {
            noUpcomingMeetingSubPanel.SetActive(true);
        }

        data = null;
    }
}
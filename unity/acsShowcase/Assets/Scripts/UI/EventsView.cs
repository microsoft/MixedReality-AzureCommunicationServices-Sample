// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. 

using Azure.Communication.Calling.Unity.Rest;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Azure.Communication.Calling.Unity;
using TMPro;
using UnityEngine;


public class EventsView : MonoBehaviour
{
    [SerializeField] [Tooltip("The list repeater to insert items into")]
    private Repeater repeater = null;

    [SerializeField] [Tooltip("The text element displaying the date for the collection of events.")]
    private TextMeshProUGUI displayTime = null;

    /// <summary>
    /// Data collection
    /// </summary>
    private ObservableCollection<IEvent> data = new ObservableCollection<IEvent>();
    
    /// <summary>
    /// Pending data to be processed 
    /// </summary>
    private IReadOnlyCollection<IEvent> pendingData = null;
    
    /// <summary>
    /// Display time value 
    /// </summary>
    private IDisplayTime displayTimeValue = null;
    
    private void Start()
    {
        if (repeater != null)
        {
            repeater.DataSource = data;
        }
    }

    private void Update()
    {
        if (pendingData != null && repeater != null)
        {
            var tempData = pendingData;
            pendingData = null;
            Clear();
            Add(tempData);
            if (repeater != null)
            {
                repeater.DataSource = this.data;
            }
            repeater.UpdateLayout();
        }
    }

    public void UpdateEvents(EventsChangedEventArgs args)
    {
        pendingData = args?.Events ?? new List<IEvent>();
    }

    public void UpdateDisplayTime(DisplayTimeChangedEventArgs args)
    {
        displayTimeValue = args.DisplayTime;
        UpdateDisplayTime();
    }

    public void UpdateDisplayTime()
    {
        if (displayTime != null)
        {
            displayTime.text = displayTimeValue?.displayTime;
        }
    }

    private void Clear()
    {
        data.Clear();
    }

    private void Add(IReadOnlyCollection<IEvent> events)
    {
        // sort events basing on start time 
        var sortableList = new List<IEvent>(events);
        sortableList.Sort(delegate(IEvent x, IEvent y)
        {
            var xStartTime = x.start.dateTimeOffset;
            var yStartTime = y.start.dateTimeOffset;
            return xStartTime.CompareTo(yStartTime);
        });

        foreach (IEvent action in sortableList)
        {
            Add(action);
        }
    }

    private void Add(IEvent action)
    {
        try
        {
            data.Add(action);
            Log.Verbose<EventsView>("Added event ({0})", action?.subject);
        }
        catch (Exception ex)
        {
            Log.Error<EventsView>("Failed to add event ({0}). Exception: {1}", action?.subject, ex);
        }
    }
}
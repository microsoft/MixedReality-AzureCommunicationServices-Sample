// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using TMPro;
using UnityEngine;
/// <summary>
/// This class controls the display for the users current time
/// </summary>
public class CurrentTimeDisplay : MonoBehaviour
{
    [SerializeField] [Tooltip("The time text display")]
    private TextMeshProUGUI timeText;

    /// <summary>
    /// Keep track of previous refreshed time 
    /// </summary>
    private float prevTime = 0;
    
    // Update is called once per frame
    void Update()
    {
        // update time every second
        if (Time.time - prevTime > 1f)
        {
            timeText.text = System.DateTime.Now.ToString("h:mm tt");
            prevTime = Time.time;
        }

    }
}

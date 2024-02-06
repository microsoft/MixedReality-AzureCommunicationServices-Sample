// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using UnityEngine;
using UnityEngine.Events;

public class UIVisibilityController : MonoBehaviour
{
    [SerializeField] [Tooltip("meeting manager")]
    private MeetingManager meetingManager = null;

    [SerializeField] [Tooltip("camera on button")]
    private GameObject cameraOnButton = null;

    [SerializeField] [Tooltip("camera off button")]
    private GameObject cameraOffButton = null;

    [SerializeField] [Tooltip("event raised when showing the main panel")]
    private UnityEvent showMainPanel = new UnityEvent();

    [SerializeField] [Tooltip("event raised when hiding the main panel")]
    private UnityEvent hideMainPanel = new UnityEvent();

    [SerializeField] [Tooltip("event raised when showing call preview panel")]
    private UnityEvent showCallPreviewPanel = new UnityEvent();

    [SerializeField] [Tooltip("event raised when hiding call preview panel")]
    private UnityEvent hideCallPreviewPanel = new UnityEvent();


    [SerializeField] [Tooltip("event raised when showing in call panel")]
    private UnityEvent showInCallPanel = new UnityEvent();

    [SerializeField] [Tooltip("event raised when hiding in call panel")]
    private UnityEvent hideInCallPanel = new UnityEvent();

    /// <summary>
    /// show the main panel 
    /// </summary>
    public void ShowMainPanel()
    {
        showMainPanel.Invoke();
    }

    /// <summary>
    /// Hide the main panel 
    /// </summary>
    public void HideMainPanel()
    {
        hideMainPanel.Invoke();
    }

    /// <summary>
    /// Show the call preview panel
    /// </summary>
    public void ShowCallPreviewPanel()
    {
        if (meetingManager != null)
        {
            if (cameraOnButton != null) cameraOnButton.SetActive(meetingManager.AutoShareLocalVideo);
            if (cameraOffButton != null) cameraOffButton.SetActive(!meetingManager.AutoShareLocalVideo);
            meetingManager.SetShareCamera(meetingManager.AutoShareLocalVideo);
        }
        showCallPreviewPanel.Invoke();
    }

    /// <summary>
    /// Hide the call preview panel 
    /// </summary>
    public void HideCallPreviewPanel()
    {
        hideCallPreviewPanel.Invoke();
    }

    /// <summary>
    /// show the the in-call panel 
    /// </summary>
    public void ShowInCallPanel()
    {
        showInCallPanel.Invoke();
    }

    /// <summary>
    /// Hide the in-call panel 
    /// </summary>
    public void HideInCallPanel()
    {
        hideInCallPanel.Invoke();
    }
}

// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Communication.Calling.Unity;
using MixedReality.Toolkit.UX;
using System;
using UnityEngine;
using UnityEngine.Events;

public enum UIVisibilityControllerState
{
    Unknown,
    SignInPanel,
    MainPanel,
    SearchPanel,
    CallPreviewPanel,
    InCallPanel
}

[Serializable]
public class UIVisibilityControllerStateEvent : UnityEvent<UIVisibilityControllerState> { } 

public class UIVisibilityController : MonoBehaviour
{
    [SerializeField]
    [Tooltip("meeting manager")]
    private MeetingManager meetingManager = null;

    [SerializeField]
    [Tooltip("camera on button")]
    private GameObject cameraOnButton = null;

    [SerializeField]
    [Tooltip("camera off button")]
    private GameObject cameraOffButton = null;

    [SerializeField]
    [Tooltip("event raised when the state is changing.")]
    private UIVisibilityControllerStateEvent stateChanging = new UIVisibilityControllerStateEvent();

    [SerializeField]
    [Tooltip("event raised when the state changes.")]
    private UIVisibilityControllerStateEvent stateChanged = new UIVisibilityControllerStateEvent();

    [SerializeField]
    [Tooltip("event raised when showing the sign in panel")]
    private UnityEvent showSignInPanel = new UnityEvent();

    [SerializeField]
    [Tooltip("event raised when hiding the sign in panel")]
    private UnityEvent hideSigninPanel = new UnityEvent();

    [SerializeField]
    [Tooltip("event raised when showing the main panel")]
    private UnityEvent showMainPanel = new UnityEvent();

    [SerializeField] 
    [Tooltip("event raised when hiding the main panel")]
    private UnityEvent hideMainPanel = new UnityEvent();

    [SerializeField]
    [Tooltip("event raised when showing search panel")]
    private UnityEvent showSearchPanel = new UnityEvent();

    [SerializeField]
    [Tooltip("event raised when hiding search panel")]
    private UnityEvent hideSearchPanel = new UnityEvent();

    [SerializeField] 
    [Tooltip("event raised when showing call preview panel")]
    private UnityEvent showCallPreviewPanel = new UnityEvent();

    [SerializeField] 
    [Tooltip("event raised when hiding call preview panel")]
    private UnityEvent hideCallPreviewPanel = new UnityEvent();

    [SerializeField] 
    [Tooltip("event raised when showing in call panel")]
    private UnityEvent showInCallPanel = new UnityEvent();

    [SerializeField] 
    [Tooltip("event raised when hiding in call panel")]
    private UnityEvent hideInCallPanel = new UnityEvent();
    
    public bool IsInCallPanelVisible
    {
        get => State == UIVisibilityControllerState.InCallPanel;
    }

    /// <summary>
    /// Get the current state of the UI visibility controller.
    /// </summary>
    public UIVisibilityControllerState State { get; private set; } = UIVisibilityControllerState.Unknown;

    private void Start()
    {
        TransitionToSignInPanel();
    }

    /// <summary>
    /// Tranisition to the "Sign In" panel.
    /// </summary>
    public void TransitionToSignInPanel()
    {
        TryTransition(UIVisibilityControllerState.SignInPanel);
    }

    /// <summary>
    /// Tranisition to the "Main" panel.
    /// </summary>
    public void TransitionToMainPanel()
    {
        TryTransition(UIVisibilityControllerState.MainPanel);
    }

    /// <summary>
    /// Tranisition to the "Search" panel.
    /// </summary>
    public void TransitionToSearchPanel()
    {
        TryTransition(UIVisibilityControllerState.SearchPanel);
    }

    /// <summary>
    /// Tranisition to the "Call Preview" panel.
    /// </summary>
    public void TransitionToCallPreviewPanel()
    {   
        TryTransition(UIVisibilityControllerState.CallPreviewPanel);
    }

    /// <summary>
    /// Tranisition to the "In Call" panel.
    /// </summary>
    public void TransitionToInCallPanel()
    {
        TryTransition(UIVisibilityControllerState.InCallPanel);
    }

    /// <summary>
    /// Tranisition to the given state, if possible.
    /// </summary>
    public bool TryTransition(UIVisibilityControllerState state)
    {
        if (state == State)
        {
            Log.Verbose<UIVisibilityController>($"Already in state {state}");
            return true;
        }

        stateChanging?.Invoke(state);
        bool succsess = false;
        switch (state)
        {
            case UIVisibilityControllerState.SignInPanel:
                if (State == UIVisibilityControllerState.Unknown || 
                    State == UIVisibilityControllerState.MainPanel)
                {
                    HideCallPreviewPanel();
                    HideInCallPanel();
                    HideSearchPanel();
                    HideMainPanel();
                    ShowSignInPanel();
                    succsess = true;
                }
                break;
            case UIVisibilityControllerState.MainPanel:
                if (State == UIVisibilityControllerState.SignInPanel ||
                    State == UIVisibilityControllerState.SearchPanel ||
                    State == UIVisibilityControllerState.CallPreviewPanel ||
                    State == UIVisibilityControllerState.InCallPanel)
                {
                    HideSignInPanel();
                    HideSearchPanel();
                    HideCallPreviewPanel();
                    HideInCallPanel();
                    ShowMainPanel();
                    succsess = true;
                }
                break;
            case UIVisibilityControllerState.SearchPanel:
                if (State == UIVisibilityControllerState.MainPanel)
                {
                    HideMainPanel();
                    ShowSearchPanel();
                    succsess = true;
                }
                break;
            case UIVisibilityControllerState.CallPreviewPanel:
                if (State == UIVisibilityControllerState.MainPanel)
                {
                    HideMainPanel();
                    ShowCallPreviewPanel();
                    succsess = true;
                }
                break;
            case UIVisibilityControllerState.InCallPanel:
                if (State == UIVisibilityControllerState.MainPanel ||
                    State == UIVisibilityControllerState.SearchPanel ||
                    State == UIVisibilityControllerState.CallPreviewPanel)
                {
                    HideSearchPanel();
                    HideMainPanel();
                    HideCallPreviewPanel();
                    ShowInCallPanel();
                    succsess = true;
                }
                break;
        }

        if (succsess)
        {
            Log.Verbose<UIVisibilityController>($"Transitioned from {State} to {state}");
            State = state;
            stateChanged?.Invoke(State);
        }
        else
        {
            Log.Error<UIVisibilityController>($"Failed to transition from {State} to {state}");
        }

        return succsess;

    }

    /// <summary>
    /// show the sign in panel
    /// </summary>
    private void ShowSignInPanel()
    {
        showSignInPanel?.Invoke();
    }

    /// <summary>
    /// Hide the sign in panel
    /// </summary>
    private void HideSignInPanel()
    {
        hideSigninPanel?.Invoke();
    }

    /// <summary>
    /// show the main panel 
    /// </summary>
    private void ShowMainPanel()
    {
        showMainPanel?.Invoke();
    }

    /// <summary>
    /// Hide the main panel 
    /// </summary>
    private void HideMainPanel()
    {
        hideMainPanel?.Invoke();
    }

    /// <summary>
    /// Show the search panel
    /// </summary>
    private void ShowSearchPanel()
    {
        showSearchPanel?.Invoke();
    }

    /// <summary>
    /// Hide the search panel.
    /// </summary>
    private void HideSearchPanel() 
    {
        hideSearchPanel?.Invoke();
    }

    /// <summary>
    /// Show the call preview panel
    /// </summary>
    private void ShowCallPreviewPanel()
    {
        if (meetingManager != null)
        {
            if (cameraOnButton != null) cameraOnButton.SetActive(meetingManager.AutoShareLocalVideo);
            if (cameraOffButton != null) cameraOffButton.SetActive(!meetingManager.AutoShareLocalVideo);
            meetingManager.SetShareCamera(meetingManager.AutoShareLocalVideo);
        }
        showCallPreviewPanel?.Invoke();
    }

    /// <summary>
    /// Hide the call preview panel 
    /// </summary>
    private void HideCallPreviewPanel()
    {
        hideCallPreviewPanel?.Invoke();
    }

    /// <summary>
    /// show the the in-call panel 
    /// </summary>
    private void ShowInCallPanel()
    {
        showInCallPanel?.Invoke();
    }

    /// Hide the in-call panel 
    /// </summary>
    private void HideInCallPanel()
    {
        hideInCallPanel?.Invoke();
    }
}

// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Communication.Calling.Unity;
using UnityEngine;
using UnityEngine.Events;


/// <summary>
/// This class handle sign in
/// </summary>
public class SignInManager : MonoBehaviour
{
    [SerializeField]
    [Tooltip("Meeting manager used for signing in.")]
    private MeetingManager meetingManager;

    [SerializeField]
    [Tooltip("UI visibility controller used to show the main panel after sign-in.")]
    private UIVisibilityController uiController;

    private void Start()
    {
        if (meetingManager == null)
        {
            Log.Error<SignInManager>("Meeting manager is not set.");
            return;
        }

        if (uiController == null)
        {
            Log.Error<SignInManager>("UI visibility controller is not set.");
            return;
        }

        meetingManager.IsLoggedInChanged += OnIsLoggedInChanged;
    }


    private void OnDestroy()
    {
        if (meetingManager != null)
        {
            meetingManager.IsLoggedInChanged -= OnIsLoggedInChanged;
        }
    }

    public void SignIn()
    {
        meetingManager.SignIn();
    }

    public void CancelSignIn()
    {
        meetingManager.CancelSignIn();
    }

    public void SignOut()
    {
        meetingManager.SignOut();
    }

    private void OnIsLoggedInChanged(MeetingManager sender, bool loggedIn)
    {
        if (loggedIn)
        {
            uiController.TryTransition(UIVisibilityControllerState.MainPanel);
        }
        else
        {
            uiController.TryTransition(UIVisibilityControllerState.SignInPanel);
        }
    }
}

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
    
    /// <summary>
    ///  event fired when the authentication is successful  
    /// </summary>
    public UnityEvent OnAuthenticatedSucess; 
    
    public void OnAuthenticationEvent(AuthenticationEventData eventData)
    {
        if (eventData.EventType == AuthenticationEventData.Type.Authenticated)
        {
            OnAuthenticatedSucess?.Invoke();
        }
    }
}

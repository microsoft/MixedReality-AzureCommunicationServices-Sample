// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Communication.Calling.Unity.Rest; 
using UnityEngine;

/// <summary>
/// user profile class 
/// </summary>
public class UserProfile
{
    /// <summary>
    /// User ID 
    /// </summary>
    private string id;
    public string Id {  get { return id; }  set => value = id; }
    
    /// <summary>
    /// User name 
    /// </summary>
    private string name;
    
    /// <summary>
    /// User display name 
    /// </summary>
    public string DisplayName { get { return name; } set { name = value; } }
    
    /// <summary>
    /// User icon 
    /// </summary>
    private Texture2D icon; 
    public Texture2D Icon { get { return icon; } set { icon = value; } }
    
    /// <summary>
    /// User presence
    /// </summary>
    private PresenceAvailability presence;
    public PresenceAvailability Presence { get { return presence; } set { presence = value; } } 

    /// <summary>
    /// User email 
    /// </summary>
    private string email;
    public string Email { get { return email; } set { email = value; } }

    /// <summary>
    /// Constructor 
    /// </summary>
    /// <param name="id"></param>
    /// <param name="name"></param>
    /// <param name="icon"></param>
    /// <param name="presence"></param>
    /// <param name="email"></param>
    public UserProfile(string id, string name, Texture2D icon, PresenceAvailability presence, string email)
    {
        this.id = id;
        this.name = name;
        this.icon = icon;
        this.presence = presence;
        this.email = email;
    }
}

// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Communication.Calling.Unity;
using System; 
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
/// <summary>
/// This class controls the local reference to recent users as well as the functions which control UserObject selection
/// </summary>
public class UserController : MonoBehaviour
{
    /// <summary>
    /// list of user profiles 
    /// </summary>
    public static List<UserProfile> UserProfiles = new List<UserProfile>();
    
    /// <summary>
    /// list selected user objects 
    /// </summary>
    public static List<UserObject> SelectedUserObjects = new List<UserObject>();
    
    /// <summary>
    /// selected user object
    /// </summary>
    public static UserObject SelectedUserObject = null;
    
    /// <summary>
    /// fired when user profiles is loaded  
    /// </summary>
    public static event Action LoadedStaticUserProfiles;
    
    /// <summary>
    /// main user name 
    /// </summary>
    public static string MainUserName;
    
    /// <summary>
    /// OnEnable 
    /// </summary>
    private void OnEnable()
    {
        PresenceGetter.OnProfilesFullyLoaded += OnProfilesFullyLoaded;
        PeopleGetter.SendMainUserName += SendMainUserName;
    }

    /// <summary>
    /// OnDisable 
    /// </summary>
    private void OnDisable()
    {
        PresenceGetter.OnProfilesFullyLoaded -= OnProfilesFullyLoaded;
        PeopleGetter.SendMainUserName -= SendMainUserName;
    }

    /// <summary>
    /// set user name 
    /// </summary>
    /// <param name="name"></param>
    private void SendMainUserName(string name)
    {
        MainUserName = name;
    }
    /// <summary>
    /// Fires when all the recent users have their photos, precenses and names sent to the app.
    /// </summary>
    /// <param name="users">temporary list of recent users sent to the app</param>
    private void OnProfilesFullyLoaded(List<StaticUserProfile> users)
    {
        foreach (var user in users)
        {
            UserProfiles.Add(new UserProfile(user.Id, user.DisplayName, user.Icon, user.Presence, user.Email));
        }
        Debug.Log("user profiles stored internally: "+UserProfiles.Count);
        LoadedStaticUserProfiles?.Invoke();
    }
    /// <summary>
    /// Returns a profile in Recent users via their email
    /// </summary>
    /// <param name="email">the email for the user you are searching for</param>
    /// <returns></returns>
    public UserProfile GetUserProfile(string email)
    {
        foreach (var profile in UserProfiles)
        {
            if (string.Compare(email, profile.Email, StringComparison.OrdinalIgnoreCase) == 0)
            {
                return profile;
            }
        }

        return null;
    }
    
    /// <summary>
    /// Returns a profile in Recent user matching ID 
    /// </summary>
    /// <param name="id">the ID of the user you are searching for</param>
    /// <returns></returns>
    public UserProfile GetUserProfileFromID(string id)
    {
        foreach (var profile in UserProfiles)
        {
            if (string.Compare(id, profile.Id, StringComparison.OrdinalIgnoreCase) == 0)
            {
                return profile;
            }
        }
        return null;
    }
     
    /// <summary>
    /// Clear all selected user objects
    /// </summary>
    public static void ClearSelectedUserObjects()
    {
        foreach(var userObject in SelectedUserObjects.ToList())
        {
            if(userObject != null)
            {
                userObject.DeSelect();
                userObject.OverlayHover(false);
            } 
        }
        if(SelectedUserObject != null)
        {
            SelectedUserObject.DeSelect();  
        } 
        SelectedUserObject = null;
    }

    /// <summary>
    /// add user to relevant contact
    /// </summary>
    /// <param name="user"></param>
    public static void AddToRecentContacts(UserObject user)
    { 
        if(!UserProfiles.Exists(x => x.Id == user.Id))
        {
            UserProfiles.Add(new UserProfile(user.Id, user.DisplayName, null, user.Presence, user.Email));
            LoadedStaticUserProfiles?.Invoke();
        } 
    }

    /// <summary>
    /// clear all 
    /// </summary>
    public void ClearAll()
    {
        UserProfiles.Clear();
        SelectedUserObjects.Clear();
    }
    
}
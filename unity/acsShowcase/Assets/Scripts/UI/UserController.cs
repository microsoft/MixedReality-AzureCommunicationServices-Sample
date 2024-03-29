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
    /// List of user profiles 
    /// </summary>
    public static List<UserProfile> UserProfiles = new List<UserProfile>();

    /// <summary>
    /// List selected user objects 
    /// </summary>
    public static List<UserObject> SelectedUserObjects = new List<UserObject>();
    
    /// <summary>
    /// Selected user object
    /// </summary>
    public static UserObject SelectedUserObject = null;
    
    /// <summary>
    /// Fired when user profiles is loaded  
    /// </summary>
    public static event Action LoadedStaticUserProfiles;


    [SerializeField]
    [Tooltip("The PhotoGetter object in the scene")]
    private PhotoGetter photoGetter;

    [SerializeField]
    [Tooltip("The PresenceGetter object in the scene")]
    private PresenceGetter presenceGetter;

    [SerializeField]
    [Tooltip("The ProfileGetter object in the scene")]
    private ProfileGetter profileGetter;

    [SerializeField]
    [Tooltip("The PeopleGetter object in the scene")]
    private PeopleGetter peopleGetter;

    [SerializeField]
    [Tooltip("The main user prefab")]
    private GameObject mainUserPrefab;

    private bool isOnPresenceLoadedCalled = false;
    private bool isOnPhotoLoadedCalled = false;
    private bool isOnProfileLoadedCalled = false;

    /// <summary>
    /// OnAwake
    /// </summary>
    private void OnAwake()
    {
        if (photoGetter == null)
        {
            photoGetter = GameObject.FindObjectOfType<PhotoGetter>();
        }

        if (presenceGetter == null)
        {
            presenceGetter = GameObject.FindObjectOfType<PresenceGetter>();
        }

        if (profileGetter == null)
        {
            profileGetter = GameObject.FindObjectOfType<ProfileGetter>();
        }

        if (peopleGetter == null)
        {
            peopleGetter = GameObject.FindObjectOfType<PeopleGetter>();
        }
    }

    private void OnEnable()
    {
        if (presenceGetter != null)
        {
            presenceGetter.PresenceLoaded += OnPresenceLoaded;
        }

        if (photoGetter != null)
        {
            photoGetter.PhotoLoaded += OnPhotoLoaded;
        }

        if (profileGetter != null)
        {
            profileGetter.ProfileLoaded += OnProfileLoaded;
        }

        if (peopleGetter != null)
        {
            peopleGetter.PeopleChanged += OnPeopleChanged;
        }
    }

    private void OnDisable()
    {
        if (presenceGetter != null)
        {
            presenceGetter.PresenceLoaded -= OnPresenceLoaded;
        }

        if (photoGetter != null)
        {
            photoGetter.PhotoLoaded -= OnPhotoLoaded;
        }

        if (profileGetter != null)
        {
            profileGetter.ProfileLoaded -= OnProfileLoaded;
        }

        if (peopleGetter != null)
        {
            peopleGetter.PeopleChanged -= OnPeopleChanged;
        }
    }

    /// <summary>
    /// Fired when the signed in user's presence is loaded 
    /// </summary>
    private void OnPresenceLoaded(PresenceGetter getter, PresenceLoadedEventArgs args)
    {
        isOnPresenceLoadedCalled = true;
        CheckAllElementsLoaded();
    }

    /// <summary>
    /// Fired when the signed in user's photo is loaded 
    /// </summary>
    private void OnPhotoLoaded(PhotoGetter getter, PhotoLoadedEventArgs args)
    {
        isOnPhotoLoadedCalled = true;
        CheckAllElementsLoaded();
    }

    /// <summary>
    /// Fired when the signed in user's profile is loaded 
    /// </summary>
    private void OnProfileLoaded(ProfileGetter getter, ProfileLoadedEventArgs args)
    {
        isOnProfileLoadedCalled = true;
        CheckAllElementsLoaded();
    }

    /// <summary>
    /// Wait to load the UI all at once the first time. 
    /// </summary>
    private void CheckAllElementsLoaded()
    {
        if (isOnPresenceLoadedCalled && isOnPhotoLoadedCalled && isOnProfileLoadedCalled)
        {
            SetUI();
        }
    }

    /// <summary>
    /// Set up the UI for the signed in user.
    /// </summary>
    private void SetUI()
    {
        GameObject userPrefab = mainUserPrefab;
        userPrefab.transform.SetAsFirstSibling();
        var userObject = userPrefab.GetComponent<UserObject>();
        userObject.SetVariablesAndUI(
            ProfileGetter.Profile.id, 
            ProfileGetter.Profile.mail, 
            PageType.RelevantContacts, 
            ProfileGetter.Profile.displayName, 
            photoGetter.Photo, 
            presenceGetter.
            Presence.availability);
    }

    /// <summary>
    /// Fires when the list of relevant contacts has changed.
    /// </summary>
    private async void OnPeopleChanged(PeopleGetter getter, PeopleChangedEventArgs args)
    {
        List<StaticUserProfile> users = await photoGetter.UpdateProfilesWorkerAsync(getter, args);
        List<StaticUserProfile> fullyLoadedUsers = await presenceGetter.UpdatePresenceAsyncWorker(users);
        OnProfilesFullyLoaded(fullyLoadedUsers);
    }

    /// <summary>
    /// Fires when all the recent users have their photos, presences and names sent to the app.
    /// </summary>
    /// <param name="users">temporary list of recent users sent to the app</param>
    private void OnProfilesFullyLoaded(List<StaticUserProfile> users)
    {
        ClearAll();
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
        foreach (var userObject in SelectedUserObjects.ToList())
        {
            if (userObject != null)
            {
                userObject.DeSelect();
            } 
        }

        if (SelectedUserObject != null)
        {
            SelectedUserObject.DeSelect();  
        } 

        SelectedUserObject = null;
    }

    /// <summary>
    /// Add user to relevant contact
    /// </summary>
    /// <param name="user"></param>
    public static void AddToRelevantContacts(UserObject user)
    { 
        if(!UserProfiles.Exists(x => x.Id == user.Id))
        {
            UserProfiles.Add(new UserProfile(user.Id, user.DisplayName, null, user.Presence, user.Email));
            LoadedStaticUserProfiles?.Invoke();
        } 
    }

    /// <summary>
    /// Clear all 
    /// </summary>
    public void ClearAll()
    {
        UserProfiles.Clear();
        SelectedUserObjects.Clear();
    }
    
}
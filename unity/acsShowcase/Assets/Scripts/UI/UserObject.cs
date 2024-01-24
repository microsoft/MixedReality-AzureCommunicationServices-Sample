// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Communication.Calling.Unity.Rest;
using MixedReality.Toolkit.UX;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// This class represents a User's visual representation within the unity app
/// </summary>
public class UserObject : MonoBehaviour
{
    [SerializeField] [Tooltip("The image to represent the presence")]
    private RawImage presence;
    [SerializeField] [Tooltip("The user name text display")]
    private TextMeshProUGUI name;
    [SerializeField] [Tooltip("The image of the profile icon")]
    private RawImage profileIcon;
    [SerializeField] [Tooltip("The text show the initial of the user when the icon profile is not found")]
    private TextMeshProUGUI initialsText;
    [SerializeField] [Tooltip("The texture image of the presence")]
    private List<Texture2D> presences;
    [SerializeField] [Tooltip("This object is shown when user object is selected")]
    private GameObject selectedOverlay;
    [SerializeField] [Tooltip("This object is shown when user object is hovered")]
    private GameObject hoverOverlay;
    [SerializeField] [Tooltip("The pressable button of this user object")]
    private PressableButton pressableButton;
    [SerializeField] [Tooltip("minimum duration in second when hover enter and exit")]
    private float hoverMinDuration = 0.1f;
    
    /// <summary>
    /// list of background color 
    /// </summary>
    private List<Color> backgroundColors = new List<Color>() { new Color(170 / 255f, 1, 241 / 255f, 1), new Color(1, 136 / 255f, 145 / 255f, 1), new Color(238 / 255f, 160 / 255f, 1, 1) };
    
    /// <summary>
    /// list of initial text color 
    /// </summary>
    private List<Color> initialsTextColors = new List<Color>() { new Color(63 / 255f, 118 / 255f, 192 / 255f, 1), new Color(183 / 255f, 32 / 255f, 35 / 255f, 1), new Color(149 / 255f, 32 / 255f, 183 / 255f, 1) };

    /// <summary>
    /// user id 
    /// </summary>
    private string id;
    public string Id
    {
        get { return id; }
        set => value = id;
    }

    /// <summary>
    /// user email
    /// </summary>
    private string email;
    public string Email
    {
        get { return email; }
        set { email = value; }
    }

    /// <summary>
    /// user display name 
    /// </summary>
    private string displayName;
    public string DisplayName
    {
        get { return displayName; }
        set { displayName = value; }
    }

    /// <summary>
    /// is selected?
    /// </summary>
    public bool IsSelected = false;
    
    /// <summary>
    /// user page type 
    /// </summary>
    private PageType userPageType;
    public PageType UserObjectPageType
    {
        get { return userPageType; }
        set { userPageType = value; }
    }
    
    /// <summary>
    /// user presence availability 
    /// </summary>
    private PresenceAvailability presenceAvail;
    
    
    /// <summary>
    /// default icon texture 
    /// </summary>
    private Texture defaultIconTexture;

    /// <summary>
    /// user presence 
    /// </summary>
    public PresenceAvailability Presence
    {
        get { return presenceAvail; }
        set { presenceAvail = value; }
    }
    
    /// <summary>
    /// event fired when participant selection has changed 
    /// </summary>
    public static event Action OnSelectedParticipantsChanged;
    
    /// <summary>
    /// event fired when participant is selected to add  
    /// </summary>
    public static event Action OnSelectedParticipantToAdd;
     
    /// <summary>
    /// event fired when selecting a particiant for a one to one call
    /// </summary>
    public static event Action OnSelectedParticipantCall;
    /// <summary>
    /// time when hover exit occurs 
    /// </summary>
    private float exitHoverTime = 0;
    
    /// <summary>
    /// true when it is user button is entering hover 
    /// </summary>
    private bool isEnterHover = false;
    
    /// <summary>
    /// true when it is user button is exiting hover 
    /// </summary>
    private bool isExitHover = false;

    /// <summary>
    /// Start 
    /// </summary>
    void Start()
    {
        defaultIconTexture = profileIcon.texture;
    }

    /// <summary>
    /// update 
    /// </summary>
    private void Update()
    {
        
        if (isExitHover)
        {
            // avoid flickering of overlay hover image because the hover enter and exit 
            // can occurs very frequently 
            if (Time.time - exitHoverTime > hoverMinDuration)
            {
                isEnterHover = false;
                isExitHover = false;
                OverlayHover(false);
            }
        }
    }

    /// <summary>
    /// copy user object 
    /// </summary>
    /// <param name="userObject"></param>
    public void Copy(UserObject userObject)
    {
        SetVariables(userObject.Id, userObject.Email, userObject.UserObjectPageType);
        SetName(userObject.DisplayName);
        SetProfileIcon((Texture2D)userObject.profileIcon.texture);
        SetPresenceIcon(userObject.presenceAvail);
    }

    
    /// <summary>
    /// select user object 
    /// </summary>
    public void Select()
    {
        //if the tenant id is in the user id, remove it
        if (Id != null)
        {
            var splitIds = this.Id.Split('@');
            id = splitIds[0];
        }

        //Participants page has multi-select
        if (UserObjectPageType == PageType.Participants)
        {
            if (UserController.SelectedUserObjects.Contains(this))
            {
                DeSelect();
            }
            else
            {
                UserController.SelectedUserObjects.Add(this);
                SetSelectedOverlay(true);
                UserController.SelectedUserObject = this;
                IsSelected = true;
            }

            OnSelectedParticipantsChanged?.Invoke();
        }
        //Otherwise you can only select one
        else
        {
            if (UserController.SelectedUserObject == this)
            {
                DeSelect();
            }
            else
            {
                if (UserController.SelectedUserObject != null)
                    UserController.SelectedUserObject.Select();
                UserController.SelectedUserObject = this;
                SetSelectedOverlay(true);
                IsSelected = true;
                UserController.AddToRelevantContacts(this);
            }
        }
        //Selected single participant for adding to a call
        if (userPageType == PageType.SearchParticipants)
        {
            OnSelectedParticipantToAdd?.Invoke();
        }
        //Selected single participant for 1on1 call
        else if ( userPageType == PageType.RelevantContacts || userPageType == PageType.SearchMain)
        {
            OnSelectedParticipantCall?.Invoke();
        }
    }

    
    /// <summary>
    /// Unselect user object
    /// </summary>
    public void DeSelect()
    {
        if (UserObjectPageType == PageType.Participants)
        {
            UserController.SelectedUserObjects.Remove(this);
            SetSelectedOverlay(false);
            OverlayHover(false);
            UserController.SelectedUserObject = null;
            IsSelected = false;
        }
        else
        {
            UserController.SelectedUserObject = null;
            SetSelectedOverlay(false);
            IsSelected = false;
        }
    }

    /// <summary>
    /// set interactability
    /// </summary>
    /// <param name="isInteractable"></param>
    public void SetInteractability(bool isInteractable)
    {
        pressableButton.enabled = isInteractable;
    }

    /// <summary>
    /// overlay when it is hoverd 
    /// </summary>
    /// <param name="isHovering"></param>
    public void OverlayHover(bool isHovering)
    {
        if (selectedOverlay.activeSelf)
            isHovering = false;
        hoverOverlay.SetActive(isHovering);
        SetOverlayIcon(isHovering, hoverOverlay);
    }

    /// <summary>
    /// overlay when it is selected 
    /// </summary>
    /// <param name="setActive"></param>
    private void SetSelectedOverlay(bool setActive)
    {
        if (setActive)
            hoverOverlay.SetActive(false);
        selectedOverlay.SetActive(setActive);
        SetOverlayIcon(setActive, selectedOverlay);
        if (!setActive && UserController.SelectedUserObject == this)
            OverlayHover(true);
    }

    /// <summary>
    /// Depending on the parent window type, the icon associated with the selection action will be different
    /// </summary>
    /// <param name="setActive"></param>
    /// <param name="overlay"></param>
    private void SetOverlayIcon(bool setActive, GameObject overlay)
    {
        var checkIcon = overlay.transform.GetChild(0).gameObject;
        var addIcon = overlay.transform.GetChild(1).gameObject;
        var phoneIcon = overlay.transform.GetChild(2).gameObject;

        switch (UserObjectPageType)
        {
            case PageType.Participants:
                checkIcon.SetActive(setActive);
                break;
            case PageType.RelevantContacts:
                phoneIcon.SetActive(setActive);
                break;
            case PageType.SearchMain:
                phoneIcon.SetActive(setActive);
                break;
            case PageType.SearchParticipants:
                addIcon.SetActive(setActive);
                break;
        }
    }

    /// <summary>
    /// set info 
    /// </summary>
    /// <param name="id"></param>
    /// <param name="email"></param>
    /// <param name="pageType"></param>
    public void SetVariables(string id, string email, PageType pageType)
    {
        this.id = id;
        this.email = email;
        UserObjectPageType = pageType;
    }

    
    /// <summary>
    /// set name 
    /// </summary>
    /// <param name="name"></param>
    public void SetName(string name)
    {
        if (this.name != null)
        {
            this.name.text = name;
            displayName = name;
        }
    }

    
    /// <summary>
    /// set profile icon 
    /// </summary>
    /// <param name="texture"></param>
    public void SetProfileIcon(Texture2D texture)
    {
        if (texture != null && texture.name != "Ellipse 8")
        {
            SetForIcon();
            profileIcon.texture = texture;
        }
        else
        {
            SetForNoIcon();
            SetIconAndTextColors();
        }
    }

    /// <summary>
    /// set icon 
    /// </summary>
    private void SetForIcon()
    {
        initialsText.gameObject.SetActive(false);
        profileIcon.color = Color.white;
    }

    /// <summary>
    /// set no icon 
    /// </summary>
    private void SetForNoIcon()
    {
        initialsText.gameObject.SetActive(true);
        var initials = GetInitials();
        initialsText.text = initials;
        profileIcon.texture = defaultIconTexture;
    }

    
    /// <summary>
    /// get user initial 
    /// </summary>
    /// <returns></returns>
    private string GetInitials()
    {
        var name = this.name.text;
        var firstChars = name.Where((ch, index) => ch != ' '
                                                   && (index == 0 || name[index - 1] == ' '));
        return new String(firstChars.ToArray());
    }

    
    /// <summary>
    /// set icon and text color 
    /// </summary>
    private void SetIconAndTextColors()
    {
        var randomColor = UnityEngine.Random.Range(1, 3);
        profileIcon.color = backgroundColors[randomColor];
        initialsText.color = initialsTextColors[randomColor];
    }

    /// <summary>
    /// set user presence icon
    /// </summary>
    /// <param name="presenceRecieved"></param>
    public void SetPresenceIcon(PresenceAvailability presenceRecieved)
    {
        presenceAvail = presenceRecieved;
        switch (presenceRecieved)
        {
            case PresenceAvailability.Available:
                presence.texture = presences[1];
                break;
            case PresenceAvailability.AvailableIdle:
                presence.texture = presences[0];
                break;
            case PresenceAvailability.Away:
                presence.texture = presences[2];
                break;
            case PresenceAvailability.BeRightBack:
                presence.texture = presences[2];
                break;
            case PresenceAvailability.Busy:
                presence.texture = presences[3];
                break;
            case PresenceAvailability.BusyIdle:
                presence.texture = presences[3];
                break;
            case PresenceAvailability.DoNotDisturb:
                presence.texture = presences[4];
                break;
            case PresenceAvailability.Offline:
                presence.texture = presences[5];
                break;
            default:
                presence.texture = presences[5];
                break;
        }
    }

    /// <summary>
    /// called when user button is entering hover mode 
    /// </summary>
    public void OnEnterHover()
    {
        if (!isEnterHover)
        {
            isEnterHover = true;
            OverlayHover(true);
        }
        exitHoverTime = Time.time;
        isExitHover = false;
    }

    /// <summary>
    /// called when user button is exiting hover mode 
    /// </summary>
    public void OnExitHover()
    {
        exitHoverTime = Time.time;
        isExitHover = true;
    }
}

public enum PageType
{
    RelevantContacts,
    SearchMain,
    Participants,
    SearchParticipants
}
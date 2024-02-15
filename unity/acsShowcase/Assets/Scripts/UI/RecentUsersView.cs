// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
/// <summary>
/// This class controls the recenet users view from the main panel
/// </summary>
public class RecentUsersView : MonoBehaviour
{
    [SerializeField] [Tooltip("The parent of scroll view to adjust the size")]
    private RectTransform scrollViewParent;
    [SerializeField] [Tooltip("The scroll view background for adjusting the size")]
    private RectTransform scrollviewBackground;
    [SerializeField] [Tooltip("The scroll view content")]
    private Transform scrollViewContent;
    [SerializeField] [Tooltip("The user prefab")]
    private GameObject horizontalUserPrefab;
    [SerializeField] [Tooltip("The no user recent text display")]
    private TextMeshProUGUI noRecentUsersText;
    [SerializeField] [Tooltip("The load more button")]
    private Transform loadMoreButton;
    [SerializeField] [Tooltip("The load more button text")]
    private TextMeshProUGUI loadMoreText;
    [SerializeField] [Tooltip("To refresh the content")]
    private RectTransform contentToRefresh;
    
    /// <summary>
    /// is viewport expanded?
    /// </summary>
    private bool isViewportExpanded;
    
    /// <summary>
    /// OnEnable
    /// </summary>
    private void OnEnable()
    {
        UserController.LoadedStaticUserProfiles += LoadedStaticUserProfiles;
    }
    
    /// <summary>
    /// OnDisable 
    /// </summary>
    private void OnDisable()
    {
        UserController.LoadedStaticUserProfiles -= LoadedStaticUserProfiles;
    }

    /// <summary>
    /// Load static user profile 
    /// </summary>
    private void LoadedStaticUserProfiles()
    {
        if (UserController.UserProfiles.Count > 10)
        {
            loadMoreButton.gameObject.SetActive(true);
        }
        else
        {
            loadMoreButton.gameObject.SetActive(false);
        }
        if (UserController.UserProfiles.Count <= 1)
        {
            noRecentUsersText.gameObject.SetActive(true);
        }
        else
        {
            noRecentUsersText.gameObject.SetActive(false);
        }

        
        // remove previous user contact list
        var allContact = new List<GameObject>();
        foreach (Transform child in scrollViewContent.transform) allContact.Add(child.gameObject);
        allContact.ForEach(child => Destroy(child));
        
        var count = 0;
        
        foreach (var user in UserController.UserProfiles)
        {
            GameObject userPrefab = GameObject.Instantiate(horizontalUserPrefab, scrollViewContent);
            ++count;
            var userObject = userPrefab.GetComponent<UserObject>();
            userObject.SetVariablesAndUI(user.Id, user.Email, PageType.RelevantContacts, user.DisplayName, user.Icon, user.Presence);
            if(count > 10)
                userObject.gameObject.SetActive(false);
        }
    }
    
    /// <summary>
    /// Called when load more button clicked 
    /// </summary>
    public void OnButtonClicked()
    {
        var loadMorePosition = loadMoreButton.localPosition;
        if (isViewportExpanded)
        {
            loadMoreText.text = "Load More";
            scrollViewParent.sizeDelta = new Vector2(scrollViewParent.sizeDelta.x, 203);
            scrollViewParent.localPosition = new Vector2(158.4f, -175.63f);
            scrollviewBackground.sizeDelta = new Vector2(scrollViewParent.sizeDelta.x, 228);
            scrollviewBackground.localPosition = new Vector2(158.4f, -175.63f); 
            loadMoreButton.localPosition = new Vector3(loadMorePosition.x, -86f, loadMorePosition.z);
            HideOrShowChildren(true);
        }
        else
        {
            loadMoreText.text = "Load Less";
            scrollViewParent.sizeDelta = new Vector2(scrollViewParent.sizeDelta.x, 354);
            scrollViewParent.localPosition = new Vector2(158.4f, -342.2f);
            scrollviewBackground.sizeDelta = new Vector2(scrollViewParent.sizeDelta.x, 365); 
            loadMoreButton.localPosition = new Vector3(loadMorePosition.x,-154f ,loadMorePosition.z);
            HideOrShowChildren(false);
        }
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentToRefresh);
        isViewportExpanded = !isViewportExpanded;
    }
    
    /// <summary>
    /// to hide or show users
    /// </summary>
    /// <param name="hide"></param>
    private void HideOrShowChildren(bool hide)
    {
        //Start at 11th child and continue
        for(int i = 10; i< scrollViewContent.childCount; i++)
        {
            if (hide)
                scrollViewContent.GetChild(i).gameObject.SetActive(false);
            else
                scrollViewContent.GetChild(i).gameObject.SetActive(true);
        }
    } 
}

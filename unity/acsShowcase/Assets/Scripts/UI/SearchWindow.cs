// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Communication.Calling.Unity; 
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
/// <summary>
/// This class controls the functionality of the search window
/// </summary>
public class SearchWindow : MonoBehaviour
{  
    [SerializeField] [Tooltip("The search input text ")]
    private Text searchInput;
    [SerializeField] [Tooltip("The viewport content")]
    private Transform viewportContent;
    [SerializeField] [Tooltip("The search result count text display")]
    private TextMeshProUGUI searchResultsCount;
    [SerializeField] [Tooltip("The reference of the search people script")]
    private PeopleSearcher peopleSearcher;
    [SerializeField] [Tooltip("The search field")]
    private InputField searchField; 
    [SerializeField] [Tooltip("The user prefab")]
    private GameObject horizontalUserPrefab;
    [SerializeField] [Tooltip("The search type")]
    private PageType searchPageType;
    [SerializeField] [Tooltip("The main user prefab")]
    private GameObject mainUserPrefab;


    /// <summary>
    /// Awake 
    /// </summary>
    private void Awake()
    {
        searchField.onValidateInput += ValidateInput;

    }
    
    /// <summary>
    /// OnEnable 
    /// </summary>
    private void OnEnable()
    {
        PeopleSearcher.OnSearchComplete += OnSearchComplete;
    }
     
    
    /// <summary>
    /// OnDisable 
    /// </summary>
    private void OnDisable()
    {
        PeopleSearcher.OnSearchComplete -= OnSearchComplete;
    }
    
    /// <summary>
    /// OnDestroy
    /// </summary>
    private void OnDestroy()
    {
        searchField.onValidateInput -= ValidateInput; 
    }
    
    
    /// <summary>
    /// Returns the search results from the API and creates the user objects in the search window
    /// </summary>
    /// <param name="searchResults">the returned list of searched users</param>
    private void OnSearchComplete(List<StaticUserProfile> searchResults)
    {
        foreach(Transform child in viewportContent.transform)
        {
            GameObject.Destroy(child.gameObject);
        }
        if(searchResults == null || searchResults.Count == 0)
        {
            searchResultsCount.text = "0 Search Results";
        }
        else
        {
            int mainUserSearched = 0;

            foreach (var user in searchResults)
            { 
                if (user.Email == ProfileGetter.Profile.mail)
                {
                    mainUserSearched = 1;
                }
                else
                {
                    var userPrefab = GameObject.Instantiate(horizontalUserPrefab, viewportContent);
                    var userObject = userPrefab.GetComponent<UserObject>();
                    userObject.SetVariablesAndUI(user.Id, user.Email, searchPageType, user.DisplayName, user.Icon, user.Presence);
                }
            }
            searchResultsCount.text = $"{searchResults.Count - mainUserSearched} Search Results";

        }
    }
    /// <summary>
    /// validate search input 
    /// </summary>
    /// <param name="text"></param>
    /// <param name="charIndex"></param>
    /// <param name="addedChar"></param>
    /// <returns></returns>
    private char ValidateInput(string text, int charIndex, char addedChar)
    {
        peopleSearcher.Query = text + addedChar;
        return addedChar;
    }  
    
    /// <summary>
    /// perform the search
    /// </summary>
    public void PerformSearch()
    {
        peopleSearcher.Query = searchInput.text; 
    } 
    
    /// <summary>
    /// clear the search result 
    /// </summary>
    public void ClearSearch()
    {
        searchField.Select();
        searchField.text = "";
        searchResultsCount.text = "";
        foreach (Transform child in viewportContent.transform)
        {
            GameObject.Destroy(child.gameObject);
        }
    }
    
    /// <summary>
    /// set search type 
    /// </summary>
    /// <param name="isMainWindow"></param>
    public void SetSearchPageType(bool isMainWindow)
    {
        if (isMainWindow)
            searchPageType = PageType.SearchMain;
        else
            searchPageType = PageType.SearchParticipants;

    }

}

// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Communication.Calling.Unity.Rest;
using TMPro;
using UnityEngine;

/// <summary>
/// This class represents attendee information such as name, status, email, id 
/// </summary>
public class AttendeeInfo : MonoBehaviour
{

    [SerializeField] [Tooltip("Attendee name text display")] 
    private TextMeshProUGUI name;

    [SerializeField] [Tooltip("Attendee status display")]
    private TextMeshProUGUI status;

    /// <summary>
    /// attendee email 
    /// </summary>
    private string email;
    
    public string Email
    {
        get { return email; }
        set { email = value; }
    }
    
    /// <summary>
    /// attendee id 
    /// </summary>
    private string id;

    public string ID
    {
        get { return id; }
        set { id = value; }
    }

    /// <summary>
    /// parent game object place holder 
    /// </summary>
    private GameObject parentGameObject = null; 
    public GameObject ParentGameObject 
    {
        get { return parentGameObject; }
        set { parentGameObject = value; }
    }
   
    
    /// <summary>
    /// set attendee info 
    /// </summary>
    /// <param name="eventAttendee"></param>

    public void SetInfo(IEventAttendee eventAttendee)
    {
        email = eventAttendee.emailAddress.address;
        name.text = eventAttendee.emailAddress.name;
        status.text = eventAttendee.status.response.ToString();
    }

    /// <summary>
    /// copy attendee info 
    /// </summary>
    /// <param name="info"></param>
    public void Copy(AttendeeInfo info)
    {
        email = info.email;
        name.text = info.name.text;
        status.text = info.status.text;
    }

    /// <summary>
    /// set attendee status 
    /// </summary>
    /// <param name="status"></param>
    public void SetStatus(string status)
    {
        this.status.text = status;
    }
    

}

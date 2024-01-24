// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class IncomingCallController : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI callerInitials;
    [SerializeField] private RawImage callerProfileIcon;
    [SerializeField] private Texture2D nullIconTexture;

    /// <summary>
    /// Sets the icon for the incoming call window. If the user is in relevant contacts and has a profile image, it will use that image.
    /// Otherwise, it will set it with the incoming callers initials (as the only data recieved is the name)
    /// </summary>
    /// <param name="callerNameObject">The incoming callers display name object in the incoming call window</param>
    public void GetInitials(TextMeshProUGUI callerNameObject)
    {
        var callerName = callerNameObject.text;
        //User is in relevant contacts and has a non-null profile picture
        if (UserController.UserProfiles != null && UserController.UserProfiles.Any(x => x.DisplayName == callerName && x.Icon != null && x.Icon.name != "Ellipse 8"))
        {
            callerProfileIcon.color = Color.white;
            callerInitials.text = "";
            callerProfileIcon.texture = UserController.UserProfiles.Find(x => x.DisplayName == callerName).Icon; 

        }
        else
        {
            SetInitials(callerName);
        }
        
    }
    /// <summary>
    /// Sets the initials as the logo for the incoming caller
    /// </summary>
    /// <param name="callerName">the incoming callers display name</param>
    private void SetInitials(string callerName)
    {
        callerProfileIcon.texture = nullIconTexture;
        callerProfileIcon.color = new Color(235 / 255f, 115 / 255f, 1, 1);
        var firstChars = callerName.Where((ch, index) => (ch != ' ' || ch != '.')
                                               && (index == 0 || (callerName[index - 1] == ' ' || callerName[index - 1] == '.')));
        callerInitials.text = new String(firstChars.ToArray());
    }
}

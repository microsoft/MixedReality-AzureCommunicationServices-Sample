// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;


/// <summary>
/// This class manages caption display 
/// </summary>
public class CaptionController : MonoBehaviour
{
    [SerializeField] [Tooltip("The reference to the caption game object to hide/show caption")]
    private GameObject caption;
    
    [SerializeField] [Tooltip("The caption text display")]
    private TextMeshProUGUI captionText;

    /// <summary>
    /// List of each line of caption 
    /// </summary>
    private List<string> multiLineText = new List<string>();
    
    /// <summary>
    /// Maximum number of line can be displayed 
    /// </summary>
    private const int maxLine = 5;
    
  
    /// <summary>
    /// Enable caption 
    /// </summary>
    public void Enable()
    {
        captionText.text = "";
        caption.SetActive(true);
    }
    
    /// <summary>
    /// Disable caption 
    /// </summary>
    public void Disable()
    {
        captionText.text = "";
        caption.SetActive(false);
    }

    /// <summary>
    /// Add new caption text 
    /// </summary>
    /// <param name="lineText"></param>
    public void AddCaptionText(string lineText)
    {
        if (string.IsNullOrEmpty(lineText)) return;
        if (!gameObject.activeSelf) return;
        
        if (multiLineText.Count > maxLine)
        {
            multiLineText.RemoveAt(0);
        }

        // remove previous text 
        if (multiLineText.Count > 0)
        {
            multiLineText.RemoveAt(multiLineText.Count - 1);
        }

        multiLineText.Add(lineText);

        string displayText = "";
        
        // concatenate all the lines to one string and separate each line with "\n" character 
        foreach (string oneLine in multiLineText)
        {
            displayText += oneLine + "\n";
        }
        captionText.text = displayText;
    }
    
}

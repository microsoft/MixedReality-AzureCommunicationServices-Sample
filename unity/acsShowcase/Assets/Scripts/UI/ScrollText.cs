// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using TMPro;
using UnityEngine;

/// <summary>
/// This class handle scrolling for text mesh when text height is greater than its rect size
/// It will scroll down to make the bottom text always visible
/// The text object should be child of game object with mask to make the top text invisible
/// when scrolling down.
/// </summary>
public class ScrollText : MonoBehaviour
{
    [SerializeField] [Tooltip("texmeshpro for scrolling")]
    private TextMeshProUGUI textMeshPro;
    
    [SerializeField] [Tooltip("rect transform of texmeshpro gameobject")] 
    private RectTransform rectTransform;
    
    [SerializeField] [Tooltip("duration of scrolling in second")]
    private float duration = 1f;
    
    /// <summary>
    /// Elapsed time to lerp between the current and target anchor y position when scrolling  
    /// </summary>
    private float elapsedTime = 1f;
    
    /// <summary>
    /// Start y anchor position before scrolling starts 
    /// </summary>
    private float startYPos = 0f;
    
    /// <summary>
    /// Desired y anchor position when scrolling ends
    /// </summary>
    private float targetYPos = 0f;

    void Update()
    {
        if (elapsedTime <= duration) // scrolling
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / duration);
            rectTransform.anchoredPosition = new Vector2(rectTransform.anchoredPosition.x, Mathf.Lerp(startYPos, targetYPos, t));
        }
        else
        {
            float newYPos = 0;
            // if text height is smaller than its size
            if (textMeshPro.textBounds.size.y - rectTransform.sizeDelta.y <= 0)
            {
                newYPos = 0;
            }
            else
            {
                // text height is larger than its size 
                newYPos = (textMeshPro.textBounds.size.y - rectTransform.sizeDelta.y) * rectTransform.localScale.y;
            }
            // check if the bottom text is visible 
            if (rectTransform.anchoredPosition.y != newYPos)
            {
                startYPos = rectTransform.anchoredPosition.y;
                targetYPos = newYPos;
                elapsedTime = 0;
            }
        }
    }
}
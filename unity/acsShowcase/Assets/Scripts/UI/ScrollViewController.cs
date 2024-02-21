// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using MixedReality.Toolkit.UX;
using UnityEngine;
using UnityEngine.UI;
/// <summary>
/// This class controls the logic for scrolling through an attached scrollview when the up/down buttons are clicked
/// </summary>
public class ScrollViewController : MonoBehaviour
{
    [SerializeField] [Tooltip("Scroll rectangle for scrolling")]
    private ScrollRect scrollRect;
    
    [SerializeField] [Tooltip("how fast is scrolling")]
    private float scrollIncrement = 0.2f;

    [SerializeField] [Tooltip("The button to scroll up")]
    private PressableButton buttonUp = null;

    [SerializeField] [Tooltip("The button to scroll down")]
    private PressableButton buttonDown = null;

    [SerializeField]  [Tooltip("The content rectangle to prevent horizontal scrolling")]
    private RectTransform contentRect = null;

    /// <summary>
    /// content initial position 
    /// </summary>
    private Vector2 contentInitPosition;
    
    /// <summary>
    /// is it scrolling? 
    /// </summary>
    private bool isScrolling = false;
    private float endScrollTime = -1;

    // Start is called before the first frame update
    void Start()
    {
    }

    /// <summary>
    /// Handle component enable event.
    /// </summary>
    private void OnEnable()
    {
        scrollRect.onValueChanged.AddListener(ScrollRectValueChanged);
        contentInitPosition = contentRect.anchoredPosition;
        UpdateButtonUpDownVisibility();
        UpdateButtonUpDownEnablement();
    }


    /// <summary>
    /// Handle component disable event.
    /// </summary>
    private void OnDisable()
    {
        scrollRect.onValueChanged.RemoveListener(ScrollRectValueChanged);
    }

    private void Update()
    {
        // scrollable still allows to scroll horizontally even in the scroll rect component, the horizontal scrolling is disabled
        // scrollable is still experimental
        if (isScrolling)
        {
            contentRect.anchoredPosition = new Vector2(contentInitPosition.x, contentRect.anchoredPosition.y); // prevent the horizontal scrolling
        }

        // fix horizontal scrolling after the finger is released 
        if (endScrollTime > 0 && Time.time - endScrollTime > 0.5f)
        {
            endScrollTime = -1;
            contentRect.anchoredPosition = new Vector2(contentInitPosition.x, contentRect.anchoredPosition.y); // fix the horizontal scrolling if it happened
        }        
    }

    /// <summary>
    /// called when scroll rect value has changed 
    /// </summary>
    /// <param name="arg0"></param>
    private void ScrollRectValueChanged(Vector2 arg0)
    {
        UpdateButtonUpDownEnablement();
        UpdateButtonUpDownVisibility();
    }

    /// <summary>
    /// scroll up 
    /// </summary>
    public void ScrollUp()
    {
        scrollRect.verticalNormalizedPosition += scrollIncrement;
    }

    
    /// <summary>
    /// scroll down 
    /// </summary>
    public void ScrollDown()
    {
        scrollRect.verticalNormalizedPosition -= scrollIncrement;
    }

    /// <summary>
    /// Update the up and down buttons' visibility
    /// </summary>
    private void UpdateButtonUpDownVisibility()
    {
        // check if we need to show up/down button for scrolling
        bool needVerticalScrollbar = scrollRect.content.rect.height > scrollRect.viewport.rect.height;
        if (buttonUp != null)
        {
            buttonUp.gameObject.SetActive(needVerticalScrollbar);
        }

        if (buttonDown != null)
        {
            buttonDown.gameObject.SetActive(needVerticalScrollbar);
        }

    }

    /// <summary>
    /// Enable or disable the up down buttons
    /// </summary>
    private void UpdateButtonUpDownEnablement()
    {
        if (buttonUp == null || buttonDown == null) return;
        if (scrollRect.verticalNormalizedPosition > 0.95)
        {
            buttonUp.enabled = false;
            buttonDown.enabled = true;
        }
        else if (scrollRect.verticalNormalizedPosition < 0.05f)
        {
            buttonUp.enabled = true;
            buttonDown.enabled = false;
        }
        else
        {
            buttonUp.enabled = true;
            buttonDown.enabled = true;
        }
    }

    /// <summary>
    /// called when start scrolling with finger 
    /// </summary>
    public void StartScrollable()
    {
        isScrolling = true;
    }

    /// <summary>
    /// called when end scrolling with finger 
    /// do not allow to scroll horizontally
    /// </summary>
    public void EndScrollable()
    {
        isScrolling = false;
        contentRect.anchoredPosition = new Vector2(contentInitPosition.x, contentRect.anchoredPosition.y);
        endScrollTime = Time.time;
    }
}

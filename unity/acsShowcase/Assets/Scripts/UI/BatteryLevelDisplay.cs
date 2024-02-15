// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using UnityEngine;
/// <summary>
/// This class controls the display for the HoloLens' battery level
/// </summary>
public class BatteryLevelDisplay : MonoBehaviour
{
    
    [Tooltip("The current battery level")]
    public float BatteryLevel = 0.5f;
    
    [SerializeField] [Tooltip("Reference to the battery rect to adjust battery icon basing on current battery level")]
    private RectTransform levelRect;
    
    [SerializeField] [Tooltip("How often to update battery level")]
    private float updateInterval = 1.0f;
    
    /// <summary>
    /// Keep the init battery icon position to adjust the level correctly
    /// </summary>
    private float initPosX;
    /// <summary>
    /// Keep the init battery icon width  to adjust the level correctly
    /// </summary>
    private float initWidth;
    
    /// <summary>
    /// Last updated time
    /// </summary>
    private float lastUpdatedTime = 0;
    
    // Start is called before the first frame update
    void Start()
    {
        initPosX = levelRect.anchoredPosition.x;
        initWidth = levelRect.rect.width;
    }

    // Update is called once per frame
    void Update()
    {
        if (Time.time - lastUpdatedTime > updateInterval)
        {

            lastUpdatedTime = Time.time;

            BatteryLevel = SystemInfo.batteryLevel;
            float newBatteryLevelWidth = BatteryLevel * initWidth;
            float redWidth = initWidth - newBatteryLevelWidth;
            // show the battery status according to the level
            levelRect.sizeDelta = new Vector2(newBatteryLevelWidth, levelRect.rect.height);
            levelRect.anchoredPosition = new Vector2(initPosX - redWidth / 2, levelRect.anchoredPosition.y);

        }
    }
}

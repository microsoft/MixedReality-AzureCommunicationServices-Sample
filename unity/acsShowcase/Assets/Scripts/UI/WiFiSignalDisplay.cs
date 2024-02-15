// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using UnityEngine;
using System;
#if ENABLE_WINMD_SUPPORT
using Windows.Media.Capture;
using Windows.Devices;
#endif 


/// <summary>
/// This class manages the status display of the WiFi signal strength 
/// </summary>
public class WiFiSignalDisplay : MonoBehaviour
{
  
    [SerializeField] [Tooltip("how often to update the display")]
    private float updateInterval = 1.0f;
    
    [SerializeField] [Tooltip("reference images to show different strengths")]
    private List<GameObject> imageSignalList;
    
    /// <summary>
    /// Last updated time 
    /// </summary>
    private float lastUpdatedTime = 0;
    
#if ENABLE_WINMD_SUPPORT        
    Windows.Networking.Connectivity.ConnectionProfile connectedProfile = null;  
    void Start()
    {
        GetProfile();
    }

    private async void GetProfile()
    {
        // get wifi connected signal strength 
        var adapters = await Windows.Devices.WiFi.WiFiAdapter.FindAllAdaptersAsync();
        foreach (var adapter in adapters)
        {
            connectedProfile = await adapter.NetworkAdapter.GetConnectedProfileAsync();
        }        
    }

    
    // Update is called once per frame
    void Update()
    {
        if (connectedProfile != null && Time.time - lastUpdatedTime > updateInterval)
        {
            UpdateWiFiSignalDisplay();
            lastUpdatedTime = Time.time;
        }
    }
    
    private async void UpdateWiFiSignalDisplay()
    {
            var signalStrength = (int)connectedProfile.GetSignalBars(); // value from 0 to 5

            int adjSignal = signalStrength;
            if (adjSignal >= imageSignalList.Count)
                adjSignal = imageSignalList.Count - 1;
            else if (adjSignal < 0)
                adjSignal = 0;
            for (int i = 0; i < imageSignalList.Count; i++)
            {
                if (i == adjSignal)
                {
                    imageSignalList[i].SetActive(true);
                }
                else
                {
                    imageSignalList[i].SetActive(false);
                }
            }
    }

#endif     
}

// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using TMPro;
using UnityEngine;


/// <summary>
/// This class show the day, month and year of today 
/// </summary>
public class TodayDisplay : MonoBehaviour
{
    [SerializeField] [Tooltip("today text display")]
    private TextMeshProUGUI todayString;
    // Start is called before the first frame update
    void Start()
    {
        todayString.text = DateTime.Now.ToString("dddd, MMMM dd, yyyy");
    }
}

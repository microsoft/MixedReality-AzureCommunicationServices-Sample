// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections; 
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// This script forces the layout it is attached to to rebuild when the Refresh function is called
/// </summary>
public class RefreshContent : MonoBehaviour
{
    [SerializeField] [Tooltip("Content size fitter ")]
    private ContentSizeFitter contentSizeFitter; 

    /// <summary>
    /// refresh 
    /// </summary>
    public void Refresh()
    {
        StartCoroutine(RefreshCoroutine());
    }
    
    /// <summary>
    /// refresh coroutine
    /// </summary>
    /// <returns></returns>
    private IEnumerator RefreshCoroutine()
    {
        yield return null;

        if (contentSizeFitter != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentSizeFitter.gameObject.GetComponent<RectTransform>());
        }

    }
}

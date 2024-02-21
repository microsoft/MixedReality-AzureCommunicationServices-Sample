// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections; 
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

/// <summary>
/// This script forces the layout it is attached to to rebuild when the Refresh function is called
/// </summary>
public class RefreshContent : MonoBehaviour
{
    [SerializeField] [Tooltip("Content size fitter ")]
    private ContentSizeFitter contentSizeFitter; 

    /// <summary>
    /// Avoid multiple refresh requests.
    /// </summary>
    private bool refreshPending = false;

    /// <summary>
    /// When enabled, refresh layout.
    /// </summary>
    public void OnEnable()
    {
        Refresh();
    }

    /// <summary>
    /// refresh 
    /// </summary>
    public void Refresh()
    {
        if (isActiveAndEnabled && !refreshPending)
        {
            StartCoroutine(RefreshCoroutine());
        }
    }
    
    /// <summary>
    /// refresh coroutine
    /// </summary>
    /// <returns></returns>
    private IEnumerator RefreshCoroutine()
    {
        refreshPending = true;
        yield return null;
        refreshPending = false;

        if (contentSizeFitter != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentSizeFitter.gameObject.GetComponent<RectTransform>());
        }
    }
}

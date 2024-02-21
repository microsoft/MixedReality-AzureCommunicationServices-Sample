// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using UnityEngine;


/// <summary>
/// This class controls the logic on enable/disable for the various manipulation bars within the project
/// </summary>
public class ManipulationController : MonoBehaviour
{
    [SerializeField] [Tooltip("The reference to the view to disable when manipulating to avoid the conflict")]
    private HorizontalCenterView horizontalCenterView;

    /// <summary>
    /// Remember the status of the view 
    /// </summary>
    private bool horizontalCenterViewEnabled = true;

    /// <summary>
    /// Start manipulating 
    /// </summary>
    public void StartManipulating()
    {
        if (horizontalCenterView != null)
        {
            horizontalCenterViewEnabled = horizontalCenterView.enabled;
            horizontalCenterView.enabled = false;
        }
    }

    /// <summary>
    /// End manipulating 
    /// </summary>
    public void EndManipulating()
    {
        if (horizontalCenterView != null)
        {
            horizontalCenterView.enabled = horizontalCenterViewEnabled;
        }
    }
    
}

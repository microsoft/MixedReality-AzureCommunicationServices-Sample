// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using MixedReality.Toolkit.SpatialManipulation;
using UnityEngine;
using UnityEngine.EventSystems;

public class BoundsControlUpdated : UIBehaviour
{
    [SerializeField]
    [Tooltip("The bounds control to update")]
    private BoundsControl boundsControl;

    /// <summary>
    /// The bounds control to update
    /// </summary>
    public BoundsControl BoundsControl
    {
        get
        {
            return boundsControl;
        }
        set
        {
            boundsControl = value;
            UpdateBounds();
        }
    }

    private bool boundsControlInitialized = false;

    protected override void OnEnable()
    {
        base.OnEnable();
        UpdateBounds();
    }

    private void LateUpdate()
    {
        if (boundsControl != null && boundsControlInitialized)
        {
            boundsControl.RecomputeBounds();
        }
    }

    protected override void OnRectTransformDimensionsChange()
    {
        base.OnRectTransformDimensionsChange();
        UpdateBounds();
    }

    private void UpdateBounds()
    {
        boundsControlInitialized = true;
    }
}

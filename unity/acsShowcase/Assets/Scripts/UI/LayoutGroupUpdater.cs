// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Force a LayoutGroup to update its layout when the RectTransform changes.
/// </summary>
public class LayoutGroupUpdater : UIBehaviour
{
    [SerializeField]
    [Tooltip("The LayoutGroup to update")]
    private LayoutGroup layoutGroup;

    /// <summary>
    /// The LayoutGroup to update
    /// </summary>
    public LayoutGroup LayoutGroup
    {
        get
        {
            return layoutGroup;
        }
        set
        {
            layoutGroup = value;
        }
    }


    protected override void OnDisable()
    {
        base.OnDisable();
        UpdateLayout();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        UpdateLayout();
    }

    protected override void OnRectTransformDimensionsChange()
    {
        base.OnRectTransformDimensionsChange();
        UpdateLayout();
    }

    private void UpdateLayout()
    {
        if (layoutGroup != null)
        {
            layoutGroup.CalculateLayoutInputHorizontal();
            layoutGroup.CalculateLayoutInputVertical();
            layoutGroup.SetLayoutHorizontal();
            layoutGroup.SetLayoutVertical();
        }
    }
}

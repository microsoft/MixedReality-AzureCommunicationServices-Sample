// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Unity.XR.CoreUtils.GUI;
using UnityEngine;
using UnityEngine.EventSystems;


/// <summary>
/// The dock to which the RectTransform will be anchored.
/// </summary>
[Flags]
public enum LayoutDock
{
    None = 0,
    Top = 0x1,
    Bottom = 0x2,
    Left = 0x4,
    Right = 0x8,
    Center = Top | Bottom | Left | Right
}


/// <summary>
/// This component anchors the RectTransform to a specified dock location.
/// </summary>
[ExecuteAlways]
public class LayoutDocker : UIBehaviour
{
    [FlagsProperty]
    [SerializeField]
    [Tooltip("The dock to which the RectTransform will be anchored.")]
    private LayoutDock dock = LayoutDock.Center;
    
    public LayoutDock Dock
    {
        get { return dock; }
        set
        {
            if (dock != value)
            {
                dock = value;
                UpdatePosition();
            }
        }
    }

    [NonSerialized]
    private RectTransform rectTransform;

    protected RectTransform RectTransform
    {
        get
        {
            if (rectTransform == null)
            {
                rectTransform = GetComponent<RectTransform>();
            }
            return rectTransform;
        }
    }

    override protected void OnValidate()
    {
        base.OnValidate();
        UpdatePosition();
    }

    override protected void OnEnable()
    {
        base.OnEnable();
        UpdatePosition();
    }

    protected override void OnRectTransformDimensionsChange()
    {
        base.OnRectTransformDimensionsChange();
        UpdatePosition();
    }

    private void UpdatePosition()
    {
        float anchorPositionX = RectTransform.anchoredPosition.x;
        float anchorPositionY = RectTransform.anchoredPosition.y;

        if (dock.HasFlag(LayoutDock.Left) &&
            dock.HasFlag(LayoutDock.Right))
        {
            anchorPositionX = 0;
        }
        else if (dock.HasFlag(LayoutDock.Left))
        {
            anchorPositionX = RectTransform.rect.width / 2;
        }
        else if (dock.HasFlag(LayoutDock.Right))
        {
            anchorPositionX = -RectTransform.rect.width / 2;
        }

        if (dock.HasFlag(LayoutDock.Top) && 
            dock.HasFlag(LayoutDock.Bottom))
        {
            anchorPositionY = 0;
        }
        else if (dock.HasFlag(LayoutDock.Top))
        {
            anchorPositionY = -RectTransform.rect.height / 2;
        }
        else if (dock.HasFlag(LayoutDock.Bottom))
        {
            anchorPositionY = RectTransform.rect.height / 2;
        }

        RectTransform.anchoredPosition = new Vector2(anchorPositionX, anchorPositionY);
    }
}

// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Copy the size of a RectTransform to a MeshRenderer as scale.
/// </summary>
[RequireComponent(typeof(RectTransform))]
[ExecuteAlways]
public class CopyCanvasSizeToMesh : UIBehaviour
{
    [SerializeField]
    [Tooltip("The Mesh to copy the size to as scale.")]
    private MeshRenderer mesh;

    /// <summary>
    /// The mesh to copy the size to as scale.
    /// </summary>
    public MeshRenderer Mesh
    {
        get => mesh;
        set
        {
            mesh = value;
            UpdateMeshSize();
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

    protected override void OnValidate()
    {
        base.OnValidate();
        UpdateMeshSize();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        UpdateMeshSize();
    }

    protected override void OnRectTransformDimensionsChange()
    {
        base.OnRectTransformDimensionsChange();
        UpdateMeshSize();
    }

    private void UpdateMeshSize()
    { 
        var size = RectTransform.sizeDelta;
        Mesh.transform.localScale = new Vector3(size.x, size.y, Mesh.transform.localScale.z);
    }
} 

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Copy the size of a RectTransform to a MeshRenderer as scale.
/// </summary>
[RequireComponent(typeof(RectTransform))]
[ExecuteAlways]
public class CopyCanvasSizeToMesh : MonoBehaviour
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
            OnRectTransformDimensionsChange();
        }
    }

    private void OnRectTransformDimensionsChange()
    {
        var rectTransform = transform as RectTransform;
        if (rectTransform == null || mesh == null)
        {
            return;
        }

        var size = rectTransform.sizeDelta;
        mesh.transform.localScale = new Vector3(size.x, size.y, Mesh.transform.localScale.z);
    }
} 

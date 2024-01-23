// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
//using Unity.VisualScripting;

/// <summary>
/// Extends an Unity InputField so that the InputField's text is persisted in Unity's user preferences store.
/// When the InputField is first loaded, the saved value is queried and applied to the InputField.
/// </summary>
[RequireComponent(typeof(TMP_InputField))]
public class SaveTextField : MonoBehaviour
{
    private TMP_InputField inputField;

    [SerializeField]
    [Tooltip("The text field's key name. This key is used when persisting textures in the user's preferences.")]
    public string KeyName = string.Empty;

    private void Start()
    {
        inputField = GetComponent<TMP_InputField>();
        if (inputField != null)
        {
            if (HasValue())
            {
                inputField.text = LoadValue();
            }
            else
            {
                SaveValue(inputField.text);
            }
            inputField.onValueChanged.AddListener(SaveValue);
        }
    }

    private void OnDestroy()
    {
        if (inputField != null)
        {
            inputField.onValueChanged.RemoveListener(SaveValue);
            SaveValue(inputField.text);
        }
    }

    private bool HasValue()
    {
        return !string.IsNullOrEmpty(KeyName) && PlayerPrefs.HasKey(KeyName);
    }

    private string LoadValue()
    {
        if (string.IsNullOrEmpty(KeyName))
        {
            return string.Empty;
        }
        else
        {
            return PlayerPrefs.GetString(KeyName);
        }
    }

    private void SaveValue(string value)
    {
        if (!string.IsNullOrEmpty(KeyName))
        {
            PlayerPrefs.SetString(KeyName, value ?? string.Empty);
        }
    }
}

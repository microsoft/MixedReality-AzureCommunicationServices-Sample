// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. 

using UnityEngine;


public class RepeaterItem : MonoBehaviour
{
    private object dataSource = null;

    public object DataSource
    {
        get => dataSource;
        set
        {
            if (dataSource != value)
            {
                var old = dataSource;
                dataSource = value;
                OnDataSourceChanged(old, dataSource);
            }
        }
    }

    protected virtual void OnDataSourceChanged(object oldValue, object newValue)
    {
    }
}


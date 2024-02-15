// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. 

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using UnityEngine;
using UnityEngine.UI;

public class Repeater : MonoBehaviour
{
    private object dataSource;
    private INotifyCollectionChanged notifier = null;
    private IList list = null;
    private List<RepeaterEntry> entries = new List<RepeaterEntry>();
    private bool _scrollToBottom = false;
    private int _scrollerContentSize = 0;

    private struct RepeaterEntry
    {
        public object item;
        public GameObject gameObject;
    }


    [SerializeField]
    [Tooltip("The container to place items")]
    private GameObject container = null;

    [SerializeField]
    [Tooltip("The item prefab")]
    private GameObject itemTemplate = null;

    [SerializeField]
    [Tooltip("The content scroller. If added, scrolling will happen automatically")]
    private ScrollRect scroller = null;

    public object DataSource
    {
        get => dataSource;
        set
        {
            if (dataSource != value)
            {
                RemoveDataSource();
                dataSource = value;
                AddDataSource();
            }
        }
    }

    public void UpdateLayout()
    {
        LayoutGroup layoutGroup = null;
        if (container != null)
        {
            container.TryGetComponent(out layoutGroup);
        }

        if (layoutGroup != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(layoutGroup.GetComponent<RectTransform>());
        }
    }

    private void OnEnable()
    {
        if (scroller != null)
        {
            scroller.onValueChanged.AddListener(ScrollValueChanged);
            scroller.verticalScrollbar.onValueChanged.AddListener(VerticalValueChanged);
        }
    }

    private void OnDisable()
    {
        if (scroller != null)
        {
            scroller.onValueChanged.RemoveListener(ScrollValueChanged);
            scroller.verticalScrollbar.onValueChanged.RemoveListener(VerticalValueChanged);
        }
    }

    private void AddDataSource()
    {
        if (dataSource is IList)
        {
            list = (IList)dataSource;
            AddItems(index: 0, list);

            if (dataSource is INotifyCollectionChanged)
            {
                notifier = (INotifyCollectionChanged)dataSource;
                notifier.CollectionChanged += OnCollectionChanged;
            }

            ScrollToBottom();
        }
    }

    private void RemoveDataSource()
    {
        if (notifier != null)
        {
            notifier.CollectionChanged -= OnCollectionChanged;
            notifier = null;
        }

        if (list != null)
        {
            RemoveItems();
        }
    }

    private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                AddItems(e.NewStartingIndex, e.NewItems);
                break;

            case NotifyCollectionChangedAction.Remove:
                RemoveItems(e.OldStartingIndex, e.OldItems.Count);
                break;

            case NotifyCollectionChangedAction.Move:
                MoveItems(e.OldStartingIndex, e.NewStartingIndex, e.OldItems.Count);
                break;

            case NotifyCollectionChangedAction.Replace:
                UpdateItems(e.NewStartingIndex, e.NewItems);
                break;

            case NotifyCollectionChangedAction.Reset:
                RemoveDataSource();
                AddDataSource();
                break;
        }
    }

    private void AddItems(int index, IList items)
    {
        foreach (var item in items)
        {
            Debug.Log("Repeater AddItems " + item.ToString());
            RepeaterEntry newEntry = CreateEntry(item);
            entries.Insert(index, newEntry);
            InsertChild(index, newEntry.gameObject);
            index++;
        }
    }

    private void RemoveItems()
    {
        RemoveItems(index: 0, entries.Count);
    }

    private void RemoveItems(int index, int count)
    {
        int end = Mathf.Min(entries.Count, index + count);
        for (int i = 0; i < end; i++)
        {
            RemoveChild(entries[index].gameObject);
            entries.RemoveAt(index);
        }

        for (int i = index; i < entries.Count; i++)
        {
            MoveChild(i, entries[i].gameObject);
        }
    }

    private void MoveItems(int oldIndex, int newIndex, int count)
    {
        int end = Mathf.Min(entries.Count, oldIndex + count);
        List<RepeaterEntry> move = new List<RepeaterEntry>(count);
        for (int i = oldIndex; i < end; i++)
        {
            move.Add(entries[i]);
            entries.RemoveAt(i);
        }

        for (int i = 0; i < move.Count; i++)
        {
            var entry = move[i];
            entries.Insert(newIndex, entry);
            MoveChild(newIndex, entry.gameObject);
        }
    }

    private void UpdateItems(int index, IList items)
    {
        foreach (var item in items)
        {
            if (index < 0 || index >= entries.Count)
            {
                throw new IndexOutOfRangeException();
            }

            RemoveChild(entries[index].gameObject);
            RepeaterEntry newEntry = CreateEntry(item);
            entries[index] = newEntry;
            InsertChild(index, newEntry.gameObject);
            index++;
        }
    }


    private RepeaterEntry CreateEntry(object item)
    {
        GameObject child;
        if (itemTemplate == null)
        {
            child = new GameObject("Repeater Entry");
        }
        else
        {
            child = Instantiate(itemTemplate);
        }
        child.SetActive(true);

        child.GetComponent<RepeaterItem>().DataSource = item;
            
        return new RepeaterEntry()
        {
            item = item,
            gameObject = child
        };
    }

    private void InsertChild(int index, GameObject child)
    {
        if (container == null)
        {
            return;
        }

        bool scroll = ScrolledToBottom();

        if (child.transform.parent != container.transform)
        {
            child.transform.SetParent(container.transform, worldPositionStays: false);
        }

        child.transform.SetSiblingIndex(index + GetNonRepeatedItemCount());

        if (scroll)
        {
            ScrollToBottom();
        }
    }

    private void RemoveChild(GameObject child)
    {
        if (container == null)
        {
            return;
        }


        if (child != null)
        {
            Destroy(child);
        }
    }

    private void MoveChild(int index, GameObject child)
    {
        if (container == null)
        {
            return;
        }

        if (child.transform.parent == container.transform)
        {
            child.transform.SetSiblingIndex(index + GetNonRepeatedItemCount());
        }
    }

    private bool ScrolledToBottom()
    {
        if (scroller != null)
        {
            return scroller.verticalNormalizedPosition <= float.Epsilon;
        }
        else
        {
            return false;
        }
    }

    private void ScrollToBottom()
    {
        _scrollToBottom = true;
    }

    private void ScrollValueChanged(Vector2 value)
    {
        Canvas.ForceUpdateCanvases();

        if (_scrollToBottom)
        {
            StartCoroutine(ForceScrollDown());
        }

        Canvas.ForceUpdateCanvases();
    }

    private void VerticalValueChanged(float value)
    {
        Canvas.ForceUpdateCanvases();

        int currentSize = scroller.content.transform.childCount;
        if (currentSize != _scrollerContentSize)
        {
            // Content Added
            _scrollerContentSize = currentSize;

            if (_scrollToBottom)
            {
                StartCoroutine(ForceScrollDown());
            }
        }
        else
        {
            if (scroller.normalizedPosition == Vector2.zero)
            {
                _scrollToBottom = true;
            }
            else
            {
                _scrollToBottom = false;
            }
        }

        Canvas.ForceUpdateCanvases();
    }

    private IEnumerator ForceScrollDown()
    {
        yield return new WaitForEndOfFrame();
        Canvas.ForceUpdateCanvases();
        scroller.gameObject.SetActive(true);
        scroller.verticalNormalizedPosition = 0f;
        scroller.verticalScrollbar.value = 0;
        Canvas.ForceUpdateCanvases();
    }

    private int GetNonRepeatedItemCount()
    {
        if (container == null)
        {
            return 0;
        }

        int repeatedItems = entries == null ? 0 : entries.Count;
        return Math.Max(container.transform.childCount - repeatedItems, 0);
    }
}

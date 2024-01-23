// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

public class CircularQueue<T>
{
    private T[] data = null;
    private int readIndex = -1;
    private int writeIndex = 0;
    private bool itemDisposable = typeof(T) is IDisposable;

    public int Count
    {
        get
        {
            lock (data)
            {
                if (readIndex < 0)
                {
                    return 0;
                }
                else if (writeIndex > readIndex)
                {
                    return writeIndex - readIndex;
                }
                else
                {
                    return (data.Length - readIndex) + (writeIndex + 1);
                }
            }
        }
    }

    public bool IsFull
    {
        get => readIndex == writeIndex;
    }

    public CircularQueue(int queueCapacity)
    {
        if (queueCapacity < 0)
        {
            throw new ArgumentException("The queue capacity can not be negative.");
        }

        data = new T[queueCapacity];
        readIndex = -1;
        writeIndex = 0;
    }

    public void Clear()
    {
        lock (data)
        {
            if (itemDisposable)
            {
                foreach (var item in data)
                {
                    if (item != null)
                    {
                        ((IDisposable)item)?.Dispose();
                    }
                }
            }

            readIndex = -1;
            writeIndex = 0;
        }
    }

    public void ClearAllButLast()
    {
        lock (data)
        {
            if (readIndex < 0)
            {
                return;
            }

            readIndex = Decrement(writeIndex);
            writeIndex = Increment(readIndex);

            if (itemDisposable)
            {
                for (int i = 0; i < readIndex; i++)
                {
                    if (data[i] != null)
                    {
                        ((IDisposable)data[i])?.Dispose();
                    }
                }

                for (int i = writeIndex; i < data.Length; i++)
                {
                    if (data[i] != null)
                    {
                        ((IDisposable)data[i])?.Dispose();
                    }
                }
            }
        }

    }

    public void Enqueue(T item)
    {
        if (item == null)
        {
            throw new ArgumentException("The input input can not be null.");
        }

        lock (data)
        {
            if (itemDisposable && data[writeIndex] != null)
            {
                ((IDisposable)data[writeIndex]).Dispose();
            }

            data[writeIndex] = item;

            if (writeIndex == readIndex)
            {
                readIndex = Increment(readIndex);
            }
            else if (readIndex < 0)
            {
                readIndex = writeIndex;
            }

            writeIndex = Increment(writeIndex);
        }
    }

    public bool TryDequeue(out T item)
    {
        bool success = false;
        item = default;

        lock (data)
        {
            if (readIndex >= 0)
            {
                item = data[readIndex];
                data[readIndex] = default;
                success = true;

                readIndex = Increment(readIndex);

                if (readIndex == writeIndex)
                {
                    readIndex = -1;
                }
            }
        }
        return success;
    }

    private int Increment(int index)
    {
        return (index + 1) % data.Length;
    }

    private int Decrement(int index)
    {
        return index == 0 ? data.Length - 1 : (index - 1);
    }
}
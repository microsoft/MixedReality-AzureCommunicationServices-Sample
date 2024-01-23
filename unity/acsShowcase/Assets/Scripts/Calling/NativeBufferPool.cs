// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Communication.Calling.UnityClient;
using System;
using System.Collections;
using UnityEngine;

public class NativeBufferPool
{
    private PrivateEntry[] data;
    private int bufferSize = 0;
    private int writeIndex = -1;
    private int readIndex = 0;

    public NativeBufferPool(int poolCapacity, int nativeBufferSize)
    {
        if (poolCapacity <= 0)
        {
            throw new ArgumentException("The pool capacity can not be negative or zero.");
        }

        if (nativeBufferSize <= 0)
        {
            throw new ArgumentException("The native buffer size can not be negative or zero.");
        }

        bufferSize = nativeBufferSize;
        data = new PrivateEntry[poolCapacity];
        CreatePoolEntries();
    }

    public void SetBufferSizes(int nativeBufferSize)
    {
        if (nativeBufferSize <= 0)
        {
            throw new ArgumentException("The native buffer size can not be negative or zero.");
        }

        lock (data)
        {
            if (nativeBufferSize != bufferSize)
            {
                bufferSize = nativeBufferSize;
                CreatePoolEntries();
            }
        }
    }

    public void Restore()
    {
        lock (data)
        {
            CreatePoolEntries();
        }
    }

    public bool TryGet(out Entry entry)
    {
        bool success = false;
        entry = null;

        lock (data)
        {
            if (readIndex >= 0)
            {
                PrivateEntry privateEntry = data[readIndex];
                privateEntry.CheckedOut = true;
                entry = privateEntry;
                success = true;

                if (writeIndex < 0)
                {
                    writeIndex = readIndex;
                }

                readIndex = Increment(readIndex);

                if (readIndex == writeIndex)
                {
                    readIndex = -1;
                }
            }
        }
        return success;
    }

    private void Return(PrivateEntry entry)
    {
        lock (data)
        {
            if (entry != null && entry.CheckedOut && writeIndex >= 0)
            {
                entry.CheckedOut = false;
                data[writeIndex] = entry;

                if (readIndex < 0)
                {
                    readIndex = writeIndex;
                }

                writeIndex = Increment(writeIndex);

                if (writeIndex == readIndex)
                {
                    writeIndex = -1;
                }
            }
        }
    }

    private void CreatePoolEntries()
    {
        for (int i = 0; i < data.Length; i++)
        {
            data[i] = new PrivateEntry(this, new NativeBuffer(bufferSize));
        }

        readIndex = 0;
        writeIndex = -1;
    }

    private int Increment(int index)
    {
        return (index + 1) % data.Length;
    }

    public abstract class Entry : IDisposable
    {
        protected NativeBufferPool Owner { get; }

        public NativeBuffer Buffer { get; private set; }

        public Entry(NativeBufferPool owner, NativeBuffer buffer)
        {
            Owner = owner;
            Buffer = buffer;
        }

        /// <summary>
        /// Dispose will return the entry to the pool of data.
        /// </summary>
        public abstract void Dispose();
    }

    private class PrivateEntry : Entry
    {
        public bool CheckedOut { get; set; }

        public PrivateEntry(NativeBufferPool owner, NativeBuffer buffer) : base(owner, buffer)
        {
        }

        /// <summary>
        /// Dispose will return the entry to the pool of data.
        /// </summary>
        public override void Dispose()
        {
            Owner.Return(this);
        }
    }
}

// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Communication.Calling.UnityClient;
using System;
using System.Collections.Concurrent;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public class VideoStreamPlayerVideoSizeChangedEvent : UnityEvent<Vector2> { }

/// <summary>
/// This class draws frame softwareFrames to a Unity textures
/// </summary>
public class VideoStreamPlayer : MonoBehaviour
{
    private struct PendingFrame
    {
        public RawVideoFrame frame;
        public RawVideoFrameKind kind;
    }

    private CallVideoStream stream = null;
    private RawIncomingVideoStream rawIncomingStream = null;
    private ConcurrentQueue<PendingFrame> pendingFrames = new ConcurrentQueue<PendingFrame>();
    private VideoStreamPlayerTextures textures;
    private MaterialPropertyBlock materialProperty = null;
    private bool clearTextures = false;
    private Material activeMaterial = null;
    private bool isActive = true;

    [SerializeField] 
    [Tooltip("The maximum number of frames to save in buffer")]
    private int maxFrameBuffer = 5;

    [SerializeField]
    [Tooltip("The videoRenderer that will draw the video textures to screen.")]
    private Renderer videoRenderer = null;

    [SerializeField]
    [Tooltip("The material to use for RBGA video streams.")]
    private Material rgbaMaterial = null;

    [SerializeField]
    [Tooltip("The material to use for i420 (YUV) video streams.")]
    private Material i420Material = null;

    [SerializeField]
    [Tooltip("Video size scale. The video object scale will be set to video width/sizeScale video height/sizeScale")]
    private float sizeScale = 1f;

    [SerializeField]
    [Tooltip("The video stream be auto start when video is attached.")]
    private bool autoStart = false;

    [SerializeField]
    [Tooltip("Event raised when the video size changes.")]
    private VideoStreamPlayerVideoSizeChangedEvent videoSizeChanged = new VideoStreamPlayerVideoSizeChangedEvent();
    
    /// <summary>
    /// Event raised when the video size changes.
    /// </summary>
    public VideoStreamPlayerVideoSizeChangedEvent VideoSizeChanged
    {
        get => videoSizeChanged;
    }

    /// <summary>
    /// Get the current video format.
    /// </summary>
    public VideoStreamFormat VideoFormat { get; private set; } = null;
   
    
    public CallVideoStream Stream
    {
        get => stream;

        set
        {
            if (stream != value)
            {
                // if the current stream is screen sharing, ignore other kind 
                if (stream != null && value != null && stream.SourceKind == VideoStreamSourceKind.ScreenSharing) 
                    return;
                ReleaseStream();
                stream = value;
                AttachStream();
            }
        }
    }

    public Renderer VideoRenderer
    {
        get { return videoRenderer; }
        set { videoRenderer = value; }
    }

    public void StartStreaming()
    {
        if (rawIncomingStream is not null)
        {
            ClearTextures();
            rawIncomingStream.Start();
            isActive = true;
        }
    }

    public void StopStreaming()
    {
        if (rawIncomingStream is not null)
        {
            ClearTextures();
            if (rawIncomingStream.State == VideoStreamState.Started)
            {
                rawIncomingStream.Stop();
            }
            isActive = false;
        }
    }

    private void OnDestroy()
    {
        if (isActive)
            ClearTextures();
    }


    private void Start()
    {
        materialProperty = new MaterialPropertyBlock();
        materialProperty.Clear();
    }


    private void Update()
    {
        if (clearTextures)
        {
            clearTextures = false;
            ClearTextures();
        }

        while (pendingFrames.Count > maxFrameBuffer)
        {
            pendingFrames.TryDequeue(out PendingFrame pendingFrameDiscard);
            pendingFrameDiscard.frame.Dispose();
        }

        if (pendingFrames.TryDequeue(out PendingFrame pendingFrame))
        {
            switch (pendingFrame.kind)
            {
                case RawVideoFrameKind.Buffer:
                    RenderSoftwareFrame(pendingFrame.frame);
                    break;

                case RawVideoFrameKind.Texture:
                    RenderHardwareFrame(pendingFrame.frame);
                    break;
            }
            pendingFrame.frame.Dispose();
        }
    }


    private void AttachStream()
    {
        if (stream == null)
        {
            return;
        }

        if (stream is RawIncomingVideoStream)
        {
            rawIncomingStream = (RawIncomingVideoStream)stream;
            AttachRawIncomingStream();
        }
    }

    private void AttachRawIncomingStream()
    {
        if (rawIncomingStream == null)
        {
            return;
        }

        rawIncomingStream.RawVideoFrameReceived += OnRawVideoFrameAvailable;

        if (autoStart)
        {
            StartStreaming();
        }
    }

    private void ReleaseStream()
    {
        if (stream == null)
        {
            return;
        }

        ReleaseRawIncomingStream();
        rawIncomingStream = null;
        clearTextures = true;
        pendingFrames.Clear();
    }

    private void ReleaseRawIncomingStream()
    {
        if (rawIncomingStream == null)
        {
            return;
        }

        rawIncomingStream.RawVideoFrameReceived -= OnRawVideoFrameAvailable;
        if (rawIncomingStream.State == VideoStreamState.Started)
        {
            rawIncomingStream.Stop();
        }
    }

    private void OnRawVideoFrameAvailable(object sender, RawVideoFrameReceivedEventArgs args)
    {
        pendingFrames.Enqueue(new PendingFrame()
        {
            frame = args.Frame,
            kind = args.Frame.Kind
        });
    }

    private void RenderSoftwareFrame(RawVideoFrame videoFrame)
    {
        var videoFrameBuffer = videoFrame as RawVideoFrameBuffer;
        if (videoFrameBuffer == null)
        {
            return;
        }

        VideoStreamFormat videoFormat = videoFrameBuffer.StreamFormat;

        SetVideoFormat(videoFormat);

        if (videoFormat.PixelFormat == VideoStreamPixelFormat.I420)
        {
            RenderSoftwareI420Frame(videoFrameBuffer);
        }
        else if (videoFormat.PixelFormat == VideoStreamPixelFormat.Rgba)
        {
            RenderSoftwareRGBAFrame(videoFrameBuffer);
        }
    }

    private void RenderHardwareFrame(RawVideoFrame videoFrame)
    {
        var videoFrameTexture = videoFrame as RawVideoFrameTexture;
        if (videoFrameTexture == null)
        {
            return;
        }

        VideoStreamFormat videoFormat = videoFrameTexture.StreamFormat;
        RenderHardwareFrame(videoFormat, videoFrameTexture.Buffer);
    }

    private bool RenderHardwareFrame(VideoStreamFormat videoFormat, IntPtr nativeTexture)
    {
        if (nativeTexture == IntPtr.Zero)
        {
            return false;
        }

        var textureFormat = videoFormat.PixelFormat.ToTextureFormat();
        if (textureFormat != TextureFormat.RGBA32)
        {
            Debug.LogError($"App is can't handle given hardware texture format ({videoFormat.PixelFormat})");
            return false;
        }

        var texture = Texture2D.CreateExternalTexture(
            videoFormat.Width,
            videoFormat.Height,
            videoFormat.PixelFormat.ToTextureFormat(),
            mipChain: false,
            linear: true,
            nativeTexture);

        if (texture == null)
        {
            return false;
        }

        UpdateRGBATexture(texture);
        RaiseSizeChangedEvent(videoFormat);
        AdjustMaterial(videoFormat.PixelFormat);

        return true;
    }

    private bool RenderHardwareFrame(VideoStreamFormat videoFormat, NativeBuffer frameBuffer)
    {
        if (frameBuffer == null)
        {
            return false;
        }

        var textureFormat = videoFormat.PixelFormat.ToTextureFormat();
        if (textureFormat != TextureFormat.RGBA32)
        {
            Debug.LogError($"App is can't handle given hardware texture format ({videoFormat.PixelFormat})");
            return false;
        }

        SetVideoFormat(videoFormat);
        RenderSoftwareRGBAFrame(frameBuffer);

        return true;
    }

    private void SetVideoFormat(VideoStreamFormat videoFormat)
    {
        VideoFormat = videoFormat;
        EnsureTextures(videoFormat);
        RaiseSizeChangedEvent(videoFormat);
        AdjustMaterial(videoFormat.PixelFormat);
    }

    private void RaiseSizeChangedEvent(VideoStreamFormat videoFormat)
    {
        float videoWidth = videoFormat.Width;
        float videoHeight = videoFormat.Height;
        VideoSizeChanged?.Invoke(new Vector2(videoWidth, videoHeight));
    }

    private void AdjustMaterial(VideoStreamPixelFormat pixelFormat)
    {
        if (videoRenderer == null)
        {
            return;
        }

        Material material = null;
        if (pixelFormat == VideoStreamPixelFormat.Rgba)
        {
            material = rgbaMaterial;
        }
        else if (pixelFormat == VideoStreamPixelFormat.I420)
        {
            material = i420Material;
        }

        if (activeMaterial != material)
        {
            videoRenderer.sharedMaterial = material;
            activeMaterial = material;
        }
    }

    private void RenderSoftwareI420Frame(RawVideoFrameBuffer videoFrameBuffer)
    {
        var buffers = videoFrameBuffer.Buffers;

        NativeBuffer buffer1 = buffers.Count > 0 ? buffers[0] : null;
        NativeBuffer buffer2 = buffers.Count > 1 ? buffers[1] : null;
        NativeBuffer buffer3 = buffers.Count > 2 ? buffers[2] : null;

        LoadRawTextureData(buffer1, textures.TextureY);
        LoadRawTextureData(buffer2, textures.TextureU);
        LoadRawTextureData(buffer3, textures.TextureV);

        textures.TextureY.Apply();
        textures.TextureU.Apply();
        textures.TextureV.Apply();
    }

    private void RenderSoftwareRGBAFrame(RawVideoFrameBuffer videoFrameBuffer)
    {
        var buffers = videoFrameBuffer.Buffers;
        NativeBuffer buffer1 = buffers.Count > 0 ? buffers[0] : null;
        RenderSoftwareRGBAFrame(buffer1);
    }

    private void RenderSoftwareRGBAFrame(NativeBuffer nativeBuffer)
    {
        LoadRawTextureData(nativeBuffer, textures.MainTexture);
        textures.MainTexture.Apply();
    }

    private static void LoadRawTextureData(NativeBuffer nativeBuffer, Texture2D destination)
    {
        nativeBuffer.GetData(out IntPtr bytes, out int signedSize);

        if (signedSize < 0)
        {
            throw new OverflowException();
        }

        destination.LoadRawTextureData(bytes, signedSize);
    }

    private void ClearTextures()
    {
        textures.TextureY = null;
        textures.TextureU = null;
        textures.TextureV = null;
        SetMaterialTextures(VideoStreamPixelFormat.I420);
        SetMaterialTextures(VideoStreamPixelFormat.Rgba);
    }

    private void EnsureTextures(VideoStreamFormat videoFormat)
    {
        if (videoFormat.PixelFormat == VideoStreamPixelFormat.I420)
        {
            EnsureI420Textures(videoFormat);
        }
        else if (videoFormat.PixelFormat == VideoStreamPixelFormat.Rgba)
        {
            EnsureRGBATextures(videoFormat);
        }
    }

    private void EnsureI420Textures(VideoStreamFormat videoFormat)
    {
        int lumaWidth = videoFormat.Width;
        int lumaHeight = videoFormat.Height;
        int chromaWidth = lumaWidth / 2;
        int chromaHeight = lumaHeight / 2;
        bool changed = false;

        if (textures.TextureY == null || textures.TextureY.width != lumaWidth || textures.TextureY.height != lumaHeight)
        {
            textures.TextureY = CreateI420Texture(lumaWidth, lumaHeight);
            changed = true;
        }

        if (textures.TextureU == null || textures.TextureU.width != chromaWidth || textures.TextureU.height != chromaHeight)
        {
            textures.TextureU = CreateI420Texture(chromaWidth, chromaHeight);
            changed = true;
        }

        if (textures.TextureV == null || textures.TextureV.width != chromaWidth || textures.TextureV.height != chromaHeight)
        {
            textures.TextureV = CreateI420Texture(chromaWidth, chromaHeight);
            changed = true;
        }

        if (changed)
        {
            SetMaterialTextures(VideoStreamPixelFormat.I420);
        }
    }

    private void EnsureRGBATextures(VideoStreamFormat videoFormat)
    {
        int width = videoFormat.Width;
        int height = videoFormat.Height;
        Texture2D texture = null;

        if (textures.MainTexture == null || textures.MainTexture.width != width || textures.MainTexture.height != height)
        {
            texture = CreateRGBATexture(width, height);
        }

        if (texture != null)
        {
            UpdateRGBATexture(texture);
        }
    }

    private void UpdateRGBATexture(Texture2D texture)
    {
        textures.MainTexture = texture;
        SetMaterialTextures(VideoStreamPixelFormat.Rgba);
    }

    private static Texture2D CreateI420Texture(int width, int height)
    {
        var texture = new Texture2D(width, height, TextureFormat.R8, mipChain: false);
        return texture;
    }

    private static Texture2D CreateRGBATexture(int width, int height)
    {
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, mipChain: false);
        return texture;
    }

    private void SetMaterialTextures(VideoStreamPixelFormat pixelFormat)
    {
        if (videoRenderer == null)
        {
            return;
        }

        if (textures.IsEmpty)
        {
            materialProperty.Clear();
        }
        else if (pixelFormat == VideoStreamPixelFormat.I420)
        {
            materialProperty.SetTexture("_YPlane", textures.TextureY);
            materialProperty.SetTexture("_UPlane", textures.TextureU);
            materialProperty.SetTexture("_VPlane", textures.TextureV);
        }
        else if (pixelFormat == VideoStreamPixelFormat.Rgba)
        {
            materialProperty.SetTexture("_MainTex", textures.MainTexture);
        }

        videoRenderer.SetPropertyBlock(materialProperty);
    }
}

public struct VideoStreamPlayerTextures
{
    public bool IsEmpty => TextureY == null;

    public Texture2D TextureY;
    public Texture2D TextureU;
    public Texture2D TextureV;

    public Texture2D MainTexture
    {
        get => TextureY;
        set
        {
            TextureY = value;
            TextureU = null;
            TextureV = null;
        }
    }
}

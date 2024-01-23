// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR;
using System.ComponentModel;
using System.Threading;


public class SceneVideoSource : CustomVideoSource
{
    private CommandBuffer commandBuffer = null;
    private RenderTexture destinationTexture = null;
    private SceneVideoType videoType = default;
    private CircularQueue<NativeBufferPool.Entry> frameQueue = null;
    private NativeBufferPool framePool = null;
    private BackgroundWorker backgroundWorker = null;

    [SerializeField]
    [Tooltip("The material to use when blit'ing the camera output to a render texture.")]
    private Material material = null;

    [SerializeField]
    [Tooltip("The camera user to capture the scene content, whose rendering is used as a video content.")]
    private Camera sourceCamera = null;

    [Tooltip("Camera event when to insert the scene capture at.")]
    private CameraEvent cameraEvent = CameraEvent.AfterEverything;

    [SerializeField]
    [Tooltip("The maximum number of frames to save in buffer")]
    private int maxFrameBuffer = 5;

    /// <summary>
    /// Get if source is currently capturing
    /// </summary>
    public override bool IsCapturing => backgroundWorker != null;

    /// <summary>
    /// Handle being destroyed
    /// </summary>
    protected override void OnDestoryed()
    {
        StopGenerating();
    }

    /// <summary>
    /// Handle being updated
    /// </summary>
    protected override void OnUpdate()
    {
        UpdateVideoTypeSourceSizesIfCameraSizeChanged();
    }

    /// <summary>
    /// Start the background process to copy camera texture data to native buffer.
    /// </summary>
    protected override void StartGenerating(CustomVideoSourceSettings settings)
    {
        if (backgroundWorker != null)
        {
            return;
        }

        bool success = true;
        if (commandBuffer != null)
        {
            Debug.LogError("Unknown state. command buffer shouldn't exist at this point");
            success = false;
        }

        if (success)
        {
            UpdateVideoType(settings);
        }

        if (success && videoType.SourceCamera == null)
        {
            Debug.Log("Unable to start capture yet until camera is selected");
            success = false;
        }

        if (success)
        {
            CreateCommandBuffer();
        }

        if (success && commandBuffer == null)
        {
            Debug.LogError("Unable to start capture. The command buffer hasn't been created");
            success = false;
        }

        if (success && sourceCamera == null)
        {
            Debug.LogError("Empty source camera for SceneVideoSource, and could not find MainCamera as fallback.");
            success = false;
        }

        if (success)
        {
            sourceCamera.AddCommandBuffer(cameraEvent, commandBuffer);
            Debug.Log("Command buffer applied");

            frameQueue = new CircularQueue<NativeBufferPool.Entry>(maxFrameBuffer);
            framePool = new NativeBufferPool(maxFrameBuffer, videoType.DestinationDataSize);

            backgroundWorker = new BackgroundWorker();
            backgroundWorker.WorkerReportsProgress = false;
            backgroundWorker.WorkerSupportsCancellation = true;
            backgroundWorker.DoWork += BackgroundWork;
            backgroundWorker.RunWorkerAsync(frameQueue);
        }

        if (!success && commandBuffer != null)
        {
            commandBuffer.Dispose();
            commandBuffer = null;
        }
    }

    /// <summary>
    /// Stop the background process.
    /// </summary>
    protected override void StopGenerating()
    {
        // The camera sometimes goes away before this component.
        if (sourceCamera != null && commandBuffer != null)
        {
            sourceCamera.RemoveCommandBuffer(cameraEvent, commandBuffer);
        }

        if (commandBuffer != null)
        {
            commandBuffer.Dispose();
            commandBuffer = null;
        }

        if (backgroundWorker != null)
        {
            backgroundWorker.CancelAsync();
            backgroundWorker.DoWork -= BackgroundWork;
            backgroundWorker = null;
        }

        if (frameQueue != null)
        {
            frameQueue.Clear();
        }
    }


    /// <summary>
    /// Update the video type if camera's size has changed
    /// </summary>
    private void UpdateVideoTypeSourceSizesIfCameraSizeChanged()
    {
        if (videoType.SourceCamera != null &&
            (videoType.SourceCameraScaledPixelSize.x != videoType.SourceCamera.scaledPixelWidth ||
            videoType.SourceCameraScaledPixelSize.y != videoType.SourceCamera.scaledPixelHeight))
        {
            UpdateVideoTypeSourceSizes();
        }
    }

    /// <summary>
    /// Update the video type based on camera's size.
    /// </summary>
    private void UpdateVideoType(CustomVideoSourceSettings settings)
    {
        var bestCamera = SelectBestCamera();
        if (bestCamera == null)
        {
            return;
        }

        videoType = new SceneVideoType();
        videoType.SourceCamera = bestCamera;

        // Use the requested outsize
        videoType.DestinationSize = settings.Size;

        // Use the requested format
        videoType.DestinationFormat = settings.Format;

        // Update source size, scale, and offset
        UpdateVideoTypeSourceSizes();
    }

    /// <summary>
    /// Update the given video types source size, scale and offset
    /// </summary>
    private void UpdateVideoTypeSourceSizes()
    {
        videoType.SourceCameraScaledPixelSize = new Vector2Int(
            videoType.SourceCamera.scaledPixelWidth,
            videoType.SourceCamera.scaledPixelHeight);

        // By default, use the camera's render target texture size
        videoType.SourceSize = videoType.SourceCameraScaledPixelSize;

        // Default to ARGB32
        videoType.SourceFormat = RenderTextureFormat.ARGB32;

        // Offset and scale into source render target.
        videoType.SourceScale = Vector2.one;
        videoType.SourceOffset = Vector2.zero;

        // Handle stereoscopic rendering for VR/AR.
        // See https://unity3d.com/how-to/XR-graphics-development-tips for details.
        if (sourceCamera.stereoEnabled)
        {
            // Readback size is the size of the texture for a single eye.
            // The readback will occur on the left eye (chosen arbitrarily).
            videoType.SourceSize.x = XRSettings.eyeTextureWidth;
            videoType.SourceSize.y = XRSettings.eyeTextureHeight;
            videoType.SourceFormat = XRSettings.eyeTextureDesc.colorFormat;

            if (XRSettings.stereoRenderingMode == XRSettings.StereoRenderingMode.MultiPass)
            {
                // Multi-pass is similar to non-stereo, nothing to do.

                // Ensure camera is not rendering to both eyes in multi-pass stereo, otherwise the command buffer
                // is executed twice (once per eye) and will produce twice as many frames, which leads to stuttering
                // when playing back the video stream resulting from combining those frames.
                if (sourceCamera.stereoTargetEye == StereoTargetEyeMask.Both)
                {
                    throw new InvalidOperationException("SourceCamera has stereoscopic rendering enabled to both eyes" +
                        " with multi-pass rendering (XRSettings.stereoRenderingMode = MultiPass). This is not supported" +
                        " with SceneVideoSource, as this would produce one image per eye. Either set XRSettings." +
                        "stereoRenderingMode to single-pass (instanced or not), or use multi-pass with a camera rendering" +
                        " to a single eye (Camera.stereoTargetEye != Both).");
                }
            }
            else if (XRSettings.stereoRenderingMode == XRSettings.StereoRenderingMode.SinglePass)
            {
                // Single-pass (non-instanced) stereo use "wide-buffer" packing.
                // Left eye corresponds to the left half of the buffer.
                videoType.SourceScale.x = 0.5f;
            }
            else if ((XRSettings.stereoRenderingMode == XRSettings.StereoRenderingMode.SinglePassInstanced)
                || (XRSettings.stereoRenderingMode == XRSettings.StereoRenderingMode.SinglePassMultiview)) // same as instanced (OpenGL)
            {
                // Single-pass instanced stereo use texture array packing.
                // Left eye corresponds to the first array slice.
            }
        }

        if (material != null)
        {
            material.SetFloat("_SrcAspect", (float)videoType.SourceSize.x / videoType.SourceSize.y);
            material.SetFloat("_DstAspect", (float)videoType.DestinationSize.x / videoType.DestinationSize.y);
        }
    }

    /// <summary>
    /// Select the best camera to capture from.
    /// </summary>
    private Camera SelectBestCamera()
    {
        Camera result = sourceCamera;

        if (result == null)
        {
            GameObject mainCameraGameObject = GameObject.FindGameObjectWithTag("MainCamera");
            if (mainCameraGameObject != null)
            {
                result = mainCameraGameObject.GetComponent<Camera>();
            }
        }

        return result;
    }

    /// <summary>
    /// Create the command buffer reading the scene content from the source camera back into CPU memory
    /// and delivering it via the <see cref="OnSceneFrameReady(AsyncGPUReadbackRequest)"/> callback to
    /// the underlying WebRTC track.
    /// </summary>
    private void CreateCommandBuffer()
    {
        if (!SystemInfo.supportsAsyncGPUReadback)
        {
            Debug.LogError("This platform does not support async GPU readback. Cannot use the SceneVideoSender component.");
            return;
        }        

        if (videoType.SourceCamera == null)
        {
            Debug.LogError("Unable to create command buffer. There is no selected camera.");
            return;
        }

        destinationTexture = new RenderTexture(
            videoType.DestinationSize.x,
            videoType.DestinationSize.y,
            depth: 0,
            videoType.SourceFormat,
            RenderTextureReadWrite.Linear);

        commandBuffer = new CommandBuffer();
        commandBuffer.name = "SceneVideoSource";

        // Explicitly set the render target to instruct the GPU to discard previous content.
        // https://docs.unity3d.com/ScriptReference/Rendering.CommandBuffer.Blit.html recommends this.
        //< TODO - This doesn't work
        //_commandBuffer.SetRenderTarget(_readBackTex, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);

        // Copy camera target to readback texture
        commandBuffer.BeginSample("Blit");

        if (material != null)
        {
            commandBuffer.SetGlobalTexture(
                Shader.PropertyToID("_MainTex"),
                BuiltinRenderTextureType.CameraTarget);

            Debug.Log("Adding Blit command with custom material");
            commandBuffer.Blit(
                BuiltinRenderTextureType.CameraTarget,
                destinationTexture,
                material,
                pass: 0,
                destDepthSlice: 0);
        }
        else
        {
            Debug.Log("Adding Blit command without material");
            commandBuffer.Blit(
                BuiltinRenderTextureType.CameraTarget,
                destinationTexture,
                videoType.SourceScale,
                videoType.SourceOffset,
                sourceDepthSlice: 0, // left eye
                destDepthSlice: 0);
        }

        commandBuffer.EndSample("Blit");

        // Copy readback texture to RAM asynchronously, invoking the given callback once done
        commandBuffer.BeginSample("Readback");
        commandBuffer.RequestAsyncReadback(destinationTexture, 0, videoType.DestinationFormat, OnSceneFrameReady);
        commandBuffer.EndSample("Readback");
    }

    /// <summary>
    /// Callback invoked by the command buffer when the scene frame GPU readback has completed
    /// and the frame is available in CPU memory.
    /// </summary>
    /// <param name="request">The completed and possibly failed GPU readback request.</param>
    private void OnSceneFrameReady(AsyncGPUReadbackRequest request)
    {
        // Read back the data from GPU, if available
        if (request.hasError)
        {
            return;
        }

        NativeArray<byte> rawData = request.GetData<byte>();
        Debug.Assert(rawData.Length > 0);

        IntPtr ptr = IntPtr.Zero;
        unsafe
        {
            ptr = (IntPtr)NativeArrayUnsafeUtility.GetUnsafePtr(rawData);
        }

        if (framePool.TryGet(out NativeBufferPool.Entry entry))
        {
            entry.Buffer.WriteData(ptr, 0, 0, rawData.Length);
            frameQueue.Enqueue(entry);
        }
    }

    /// <summary>
    /// Send frames to listener, which will likely encode frame into a video stream.
    /// </summary>
    private void BackgroundWork(object sender, DoWorkEventArgs e)
    {
        bool run = true;
        BackgroundWorker worker = (BackgroundWorker)sender;
        while (run && !worker.CancellationPending)
        {
            while (run && !worker.CancellationPending && frameQueue.TryDequeue(out NativeBufferPool.Entry entry))
            {
                try
                {
                    FireMediaSamplesReady(new MediaSampleArgs() { Buffer = entry.Buffer });
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to send frame. Exception: {ex}");
                    run = false;
                }

                entry.Dispose();
            }

            Thread.Sleep(0);
        }
    }

    private struct SceneVideoType
    {
        public Camera SourceCamera;

        public Vector2Int SourceCameraScaledPixelSize;

        public Vector2Int SourceSize;

        public RenderTextureFormat SourceFormat;

        public Vector2 SourceScale;

        public Vector2 SourceOffset;

        public Vector2Int DestinationSize;

        public TextureFormat DestinationFormat;

        public int DestinationDataSize => DestinationSize.x * DestinationSize.y * DestinationFormat.PixelSize();
    }
}

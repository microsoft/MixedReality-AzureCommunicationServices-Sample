using Azure.Communication.Calling.UnityClient;
using UnityEngine;

public static class TextureFormatExtensions
{
    public static VideoStreamPixelFormat ToPixelFormat(this TextureFormat value)
    {
        VideoStreamPixelFormat pixelFormat;
        switch (value)
        {
            case TextureFormat.BGRA32:
                pixelFormat = VideoStreamPixelFormat.Bgrx;
                break;

            case TextureFormat.RGBA32:
                pixelFormat = VideoStreamPixelFormat.Rgba;
                break;

            default:
                pixelFormat = VideoStreamPixelFormat.Rgba;
                Debug.LogError($"Unsupported texture format {value}");
                break;
        }

        return pixelFormat;
    }

    public static int PixelSize(this TextureFormat value)
    {
        int size = 1;
        switch (value)
        {
            case TextureFormat.RGBA32:
            case TextureFormat.BGRA32:
                size = 4;
                break;

            default:
                Debug.LogError($"Unsupported texture format {value}");
                break;
        }

        return size;
    }

    public static TextureFormat ToTextureFormat(this VideoStreamPixelFormat value)
    {
        TextureFormat textureFormat;
        switch (value)
        {
            case VideoStreamPixelFormat.Bgrx:
                textureFormat = TextureFormat.BGRA32;
                break;

            case VideoStreamPixelFormat.Rgba:
                textureFormat = TextureFormat.RGBA32;
                break;

            default:
                textureFormat = TextureFormat.RGBA32;
                Debug.LogError($"Unsupported pixel format {value}");
                break;
        }

        return textureFormat;
    }

    public static int PixelSize(this VideoStreamPixelFormat value)
    {
        int size = 1;
        switch (value)
        {
            case VideoStreamPixelFormat.Bgrx:
            case VideoStreamPixelFormat.Rgba:
                size = 4;
                break;

            default:
                Debug.LogError($"Unsupported pixel format {value}");
                break;
        }

        return size;
    }
}

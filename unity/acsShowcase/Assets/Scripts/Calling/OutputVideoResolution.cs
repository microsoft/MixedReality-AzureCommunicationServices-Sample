// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using UnityEngine;

public enum OutputVideoResolution 
{
    Unknown,
    Resolution_1920x1080,
    Resolution_1280x720,
    Resolution_960x540,
    Resolution_640x360,
    Resolution_480x270,
    Resolution_320x180,
    Resolution_640x480,
    Resolution_424x320,
    Resolution_320x240,
    Resolution_212x160
}

public static class OutputVideoResolutionsExtensions
{
    public static Vector2Int ToVector(this OutputVideoResolution value)
    {
        switch (value)
        {
            case OutputVideoResolution.Resolution_1920x1080:
                return new Vector2Int(1920, 1080);

            case OutputVideoResolution.Resolution_1280x720:
                return new Vector2Int(1280, 720);

            case OutputVideoResolution.Resolution_960x540:
                return new Vector2Int(960, 540);

            case OutputVideoResolution.Resolution_640x360:
                return new Vector2Int(640, 360);

            case OutputVideoResolution.Resolution_480x270:
                return new Vector2Int(480, 270);

            case OutputVideoResolution.Resolution_320x180:
                return new Vector2Int(320, 180);

            case OutputVideoResolution.Resolution_640x480:
                return new Vector2Int(640, 480);

            case OutputVideoResolution.Resolution_424x320:
                return new Vector2Int(424, 320);

            case OutputVideoResolution.Resolution_320x240:
                return new Vector2Int(320, 240);

            case OutputVideoResolution.Resolution_212x160:
                return new Vector2Int(212, 160);

            default:
                Debug.LogError($"Unknown resoltion enumeration {value}");
                return new Vector2Int(212, 160);
        }
    }
}

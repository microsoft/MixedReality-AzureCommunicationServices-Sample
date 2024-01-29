# Building MediaPlayback Unity Plugin

Before the Azure Communication Services (ACS) Unity app can be used, the custom **MediaPlayback** Unity plugin must be built. This plugin enables playing Azure Communications Services video streams within Unity. 

> The source code for the MediaPlayback plugin comes from the [Windows 10 Media Playback for Unity](https://github.com/vladkol/MediaPlayback) OSS project, available on GitHub. Please see the [NOTICE](./../../NOTICE) file for licensing information.

To build the MediaPlayback plugin, run `build-mediaPlayback.ps1` from a PowerShell window. This script will build all configurations for the MediaPlayback plugin and copy files to the Unity sample app. 

![Screenshot showing a PowerShell script compiling the Media Playback plugin](./images/image-300-mediaplayback-build.png)

## Why is this needed?
The ACS SDK delivers video steams via a custom protocol handler using the *skype* schema. As a result, Unity's video player cannot consume these video streams directly. Instead the developer must write custom code to capture ACS video frames and present them to a texture rendered within the Unity scene. One method of doing this is with the Windows Runtime [MediaPlayer](https://docs.microsoft.com/en-us/uwp/api/Windows.Media.Playback.MediaPlayer?view=winrt-22621) class.  

The **MediaPlayback** plugin wraps Windows' MediaPlayer class, and exposes API projections to the Unity layer. In addition, the plugin supplies Unity MonoBehaviours that make consuming the video textures easy.

![The MediaPlayback plugin's MonoBehaviour, 'Playback (Script)'](./images/image-301-mediaplayback-mono.png)

> The **MediaPlayback** plugin also provides 3D video support, which is not necessary for this ACS sample. In fact, as cool as it sounds, ACS doesn't provide 3D video support. So, the plugin's 3D functionality can safely be removed from the provided code.

## Next Step
The next step, [Configuring Sample with AAD Authentication](./unity-sample-app-setup-2.md#configuring-sample-with-aad-authentication), describes how to configure the Unity sample app with AAD authentication.
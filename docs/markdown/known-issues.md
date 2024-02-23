# Known Issues
The following are known issues, unsupported features, or pitfalls you may encounter when running the sample app:

* When joining a meeting, you will not be able to see the video of participants that have joined before you. In order to see other participants' video, they will have to toggle it off and on. There is an ACS issue where OnVideoStreamStateChanged is not fired when other participants has an active stream before the user joins meeting. 

* Outgoing audio and video does not work with holographic app remoting. In Unity editor, note that only the video of the scene will be used (not PC camera). 

* When leaving a meeting, the exception "Error HangUpCurrentCall. Exception: Unknown error" is thrown. This is an ACS issue.

* Some features, including 1:1 calling, inviting contacts to a meeting, and adding/removing participants from a meeting, are not implemented. Their UI will call a not-implemented dialogue.

![Screenshot showing a warning that the feature is not implemented.](./images/not-implemented.png)

# The End
 This is the of the sample app documentation. To learn about the Unity sample application, and how setup with
 AAD, go to the [start](./azure-function-setup-1.md#multi-tenant-accounts-and-communication-services) of this documentation

// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Xml;
using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;



/// <summary>
/// To access the wifi status of a device, we must add a permission to the Package.appxmanifest file generated into Package.appxmanifest
/// This post build will add the following line into the Capabilities section: <DeviceCapability Name="wiFiControl"/>
/// </summary>
public class UWPPostBuild {
    [PostProcessBuildAttribute(1)]
    public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject) {

        Debug.Log( "UWPPostBuild " + pathToBuiltProject + " target " + target.ToString() + "  Application.productName: " + Application.productName);

        if (target == BuildTarget.WSAPlayer)
        {
            string manifestFilePath = pathToBuiltProject + "/" + Application.productName + "/Package.appxmanifest";
            Debug.Log("manifestFilePath : " + manifestFilePath);
            if (!System.IO.File.Exists(manifestFilePath))
            {
                Debug.LogError("Manifest file: " + manifestFilePath + " does not exist!");
                return;
            }

            XmlDocument document = new XmlDocument();
            document.Load(manifestFilePath);
            XmlElement rootNode = document.DocumentElement;
            var capNode = rootNode.GetElementsByTagName("Capabilities");
                
            if (capNode.Count > 0)
            {
                XmlElement devCap = document.CreateElement("DeviceCapability", document.DocumentElement.NamespaceURI);
                devCap.SetAttribute("Name", "wifiControl");
                capNode[0].AppendChild(devCap);
                document.Save(manifestFilePath);
                Debug.Log("Successfully updated Package.appxmanifest file" );
            }
        }
        
    }
}

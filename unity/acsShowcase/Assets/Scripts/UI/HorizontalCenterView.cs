// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System; 
using UnityEngine; 

/// <summary>
/// Keep the game object the horizontal center of the camera view, it keeps the vertical (y) position of the
/// game object unchanged.  
/// </summary>
public class HorizontalCenterView : MonoBehaviour
{
    [SerializeField] [Tooltip("starting adjust when the angle between the camera and this object greater than this")]
    private float adjustAngle = 45.0f;
    
    [SerializeField] [Tooltip("how fast it follows the camera")]
    private float rotatingSpeed = 1;
    
    /// <summary>
    /// Temporary distance from the camera for calculation
    /// </summary>
    private float distFromCamera = -1000;

    /// <summary>
    /// Is it rotating (adjusting)
    /// </summary>
    private bool isRotating = false;
    
    /// <summary>
    /// Target angle to rotate to the front the camera 
    /// </summary>
    private float targetAngle = 0f;


    private void OnDisable()
    {
        distFromCamera = -1000;
    }
     
     
    // Update is called once per frame
    void Update()
    {
        Vector3 localPoint = Camera.main.transform.InverseTransformPoint(transform.position);
        double angle = Math.Atan2(localPoint.x, localPoint.z) * Mathf.Rad2Deg;
        if (!isRotating)
        {
            if (Math.Abs(angle) < adjustAngle) return;
            targetAngle = (float)angle;
        }

        float rotAngle = rotatingSpeed * Time.deltaTime;
        if (targetAngle > 0)
        {
            if (targetAngle - rotAngle > 0)
            {
                targetAngle -= rotAngle;
            }
            else
            {
                rotAngle = targetAngle;
                targetAngle = 0f;
            }

            isRotating = true;
        }
        else if (targetAngle < 0)
        {
            rotAngle = -rotAngle;
            if (targetAngle - rotAngle < 0)
            {
                targetAngle -= rotAngle;
            }
            else
            {
                rotAngle = targetAngle;
                targetAngle = 0f;
            }
            isRotating = true;
        }
        else
        {
            isRotating = false;
        }
        
        if (rotAngle != 0)
            transform.RotateAround(Camera.main.transform.position, Vector3.up, -(float)rotAngle);

        var cameraPos = Camera.main.transform.position;
        cameraPos.y = transform.position.y; // keep the same y position
        var vecFromCamera = transform.position - cameraPos;
        if (distFromCamera <= -1)
        {
            distFromCamera = vecFromCamera.magnitude;
        }
        else
        {
            var delta = vecFromCamera.magnitude - distFromCamera;
            // keep fix distance from the camera   
            transform.position = transform.position + vecFromCamera.normalized * -delta;
        }
    }
}
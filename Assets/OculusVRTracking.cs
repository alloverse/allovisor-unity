using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OculusVRTracking : MonoBehaviour
{

    public Transform controller;

    public static bool leftHanded { get; private set; }

    System.IO.StreamWriter recording;

    void Awake()
    {
#if UNITY_EDITOR
        leftHanded = false;        // (whichever you want to test here)
#else
        leftHanded = OVRInput.GetControllerPositionTracked(OVRInput.Controller.LTouch);
#endif
    }

    void Update()
    {
        OVRInput.Controller c = leftHanded ? OVRInput.Controller.LTouch : OVRInput.Controller.RTouch;
        if (OVRInput.GetControllerPositionTracked(c))
        {
            controller.localRotation = OVRInput.GetLocalControllerRotation(c);
            controller.localPosition = OVRInput.GetLocalControllerPosition(c);
        }
    }
}
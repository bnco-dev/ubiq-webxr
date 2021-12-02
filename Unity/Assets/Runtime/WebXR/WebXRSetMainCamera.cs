using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WebXR;

public class WebXRSetMainCamera : MonoBehaviour
{
    private Camera[] cameras;

    private void Awake()
    {
        cameras = GetComponentsInChildren<Camera>();
    }

    private void Update()
    {
        if (cameras == null)
        {
            return;
        }

        for (int i = 0; i < cameras.Length; i++)
        {
            if (cameras[i].enabled)
            {
                cameras[i].tag = "MainCamera";
                return;
            }
        }
    }
}

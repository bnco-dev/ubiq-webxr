#if UNITY_WEBGL && !UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SpatialTracking;
using UnityEngine.Events;
using WebXR;

namespace Ubiq.XR
{
    public class HandController : Hand, IPrimaryButtonProvider, IMenuButtonProvider
    {
        public ButtonEvent GripPress;
        public ButtonEvent TriggerPress;
        public SwipeEvent JoystickSwipe;

        [SerializeField]
        private ButtonEvent _PrimaryButtonPress;
        public ButtonEvent PrimaryButtonPress { get { return _PrimaryButtonPress; } }

        [SerializeField]
        private ButtonEvent _MenuButtonPress;
        public ButtonEvent MenuButtonPress { get { return _MenuButtonPress; } }

        public Vector2 Joystick;

        // for smooth hand animation transitions
        public float GripValue;
        public float TriggerValue;

        public bool GripState;
        public bool TriggerState;
        public bool PrimaryButtonState;

        public bool Left { get { return poseDriver.poseSource == UnityEngine.SpatialTracking.TrackedPoseDriver.TrackedPose.LeftPose; } }
        public bool Right { get { return poseDriver.poseSource == UnityEngine.SpatialTracking.TrackedPoseDriver.TrackedPose.RightPose; } }

        private string[] profiles = null;

        private int oculusLinkBugTest = 0;
        private Quaternion oculusOffsetRay = Quaternion.Euler(90f, 0, 0);
        private Quaternion oculusOffsetGrip = Quaternion.Euler(-90f, 0, 0);

        private const bool ALWAYS_USE_GRIP = false;
        public Vector3 gripPosition { get; private set; } = Vector3.zero;
        public Quaternion gripRotation { get; private set; } = Quaternion.identity;

        private TrackedPoseDriver poseDriver;

        private void Awake()
        {
            poseDriver = GetComponent<TrackedPoseDriver>();
        }

        private void OnEnable()
        {
            WebXRManager.OnControllerUpdate += OnControllerUpdate;
            // WebXRManager.OnHandUpdate += OnHandUpdateInternal;
            // SetControllerActive(false);
            // SetHandActive(false);
        }

        private void OnDisable()
        {
            WebXRManager.OnControllerUpdate -= OnControllerUpdate;
            // WebXRManager.OnHandUpdate -= OnHandUpdateInternal;
            // SetControllerActive(false);
            // SetHandActive(false);
        }

        private WebXRControllerHand GetHand()
        {
            if (Left)
            {
                return WebXRControllerHand.LEFT;
            }
            else if (Right)
            {
                return WebXRControllerHand.RIGHT;
            }
            else
            {
                return WebXRControllerHand.NONE;
            }
        }

        private void OnControllerUpdate(WebXRControllerData controllerData)
        {
            if (controllerData.hand == (int)GetHand())
            {
                if (!controllerData.enabled)
                {
                    // SetControllerActive(false);
                    return;
                }

                profiles = controllerData.profiles;

                if (oculusLinkBugTest != 1)
                {
                    gripRotation = controllerData.gripRotation;
                    gripPosition = controllerData.gripPosition;
                    if (ALWAYS_USE_GRIP)
                    {
                        transform.localRotation = controllerData.rotation * controllerData.gripRotation;
                        transform.localPosition = controllerData.rotation * (controllerData.position + controllerData.gripPosition);
                    }
                    else
                    {
                        transform.localRotation = controllerData.rotation;
                        transform.localPosition = controllerData.position;
                    }
                    // Oculus on desktop returns wrong rotation for targetRaySpace, this is an ugly hack to fix it
                    if (CheckOculusLinkBug())
                    {
                        HandleOculusLinkBug(controllerData);
                    }
                }
                else
                {
                    // Oculus on desktop returns wrong rotation for targetRaySpace, this is an ugly hack to fix it
                    HandleOculusLinkBug(controllerData);
                }

                TriggerValue = controllerData.trigger;
                GripValue = controllerData.squeeze;

                Joystick = new Vector2(controllerData.thumbstickX,controllerData.thumbstickY);

                TriggerState = TriggerValue == 1;
                GripState = GripValue == 1;
                PrimaryButtonState = controllerData.buttonA == 1;

                TriggerPress.Update(TriggerState);
                GripPress.Update(GripState);
                PrimaryButtonPress.Update(PrimaryButtonState);
                MenuButtonPress.Update(controllerData.buttonB == 1);
                JoystickSwipe.Update(Joystick.x);
            }
        }

        // Oculus on desktop returns wrong rotation for targetRaySpace, this is an ugly hack to fix it
        private void HandleOculusLinkBug(WebXRControllerData controllerData)
        {
            gripRotation = controllerData.gripRotation * oculusOffsetGrip;
            gripPosition = controllerData.gripPosition;
            if (ALWAYS_USE_GRIP)
            {
                transform.localRotation = controllerData.rotation * controllerData.gripRotation;
                transform.localPosition = controllerData.rotation * (controllerData.position + controllerData.gripPosition);
            }
            else
            {
                transform.localRotation = controllerData.rotation * oculusOffsetRay;
                transform.localPosition = controllerData.position;
            }
        }

        // Oculus on desktop returns wrong rotation for targetRaySpace, this is an ugly hack to fix it
        private bool CheckOculusLinkBug()
        {
            if (oculusLinkBugTest == 0
                && profiles != null && profiles.Length > 0)
            {
                if (profiles[0] == "oculus-touch" && gripRotation.x > 0)
                {
                    oculusLinkBugTest = 1;
                    return true;
                }
                else
                {
                    oculusLinkBugTest = 2;
                }
            }
            return false;
        }
    }
}

#endif // UNITY_WEBGL && !UNITY_EDITOR

#if UNITY_EDITOR || !UNITY_WEBGL
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.SpatialTracking;
using UnityEngine.Events;
using UnityEngine.XR.Management;
using static UnityEngine.SpatialTracking.TrackedPoseDriver;

namespace Ubiq.XR
{
    public class HandController : Hand, IPrimaryButtonProvider, IMenuButtonProvider
    {
        private TrackedPoseDriver poseDriver;
        private List<InputDevice> controllers;
        private List<XRNodeState> nodes;
        private XRNode node;

        public ButtonEvent GripPress;
        public ButtonEvent TriggerPress;
        public SwipeEvent JoystickSwipe;

        [SerializeField]
        private ButtonEvent _PrimaryButtonPress;
        public ButtonEvent PrimaryButtonPress { get { return _PrimaryButtonPress; } }

        [SerializeField]
        private ButtonEvent _MenuButtonPress;
        public ButtonEvent MenuButtonPress { get { return _MenuButtonPress; } }

        public Vector2 Joystick;

        // for smooth hand animation transitions
        public float GripValue;
        public float TriggerValue;

        public bool GripState;
        public bool TriggerState;
        public bool PrimaryButtonState;

        private bool initialised;

        private void Awake()
        {
            poseDriver = GetComponent<TrackedPoseDriver>();
            controllers = new List<InputDevice>();
            nodes = new List<XRNodeState>();

            if (Right)
            {
                node = XRNode.RightHand;
            }
            if(Left)
            {
                node = XRNode.LeftHand;
            }

            initialised = false;
        }

        private InputDeviceCharacteristics GetSideCharacteristic(TrackedPose type)
        {
            switch (type)
            {
                case TrackedPose.LeftPose:
                    return InputDeviceCharacteristics.Left;
                case TrackedPose.RightPose:
                    return InputDeviceCharacteristics.Right;
                case TrackedPose.RemotePose:
                    return 0;
                default:
                    return 0;
            }
        }

        private void InitialiseHandDevices()
        {
            controllers.Clear();
            var collection = new List<InputDevice>();
            InputDevices.GetDevices(collection);
            foreach (var item in collection)
            {
                InputDevices_deviceConnected(item);

            }
            InputDevices.deviceConnected += InputDevices_deviceConnected;
            initialised = true;
        }

        private void InputDevices_deviceConnected(InputDevice device)
        {
            if ((device.characteristics & InputDeviceCharacteristics.Controller) == 0)
            {
                return;
            }
            if ((device.characteristics & InputDeviceCharacteristics.HeldInHand) == 0)
            {
                return;
            }
            if ((device.characteristics & GetSideCharacteristic(poseDriver.poseSource)) == 0)
            {
                return;
            }
            controllers.Add(device);
        }

        // Update is called once per frame
        void Update()
        {
            if (poseDriver.enabled)
            {
                if (!initialised)
                {
                    if (XRGeneralSettings.Instance && XRGeneralSettings.Instance.Manager.isInitializationComplete)
                    {
                        InitialiseHandDevices();
                    }
                }

                foreach (var item in controllers)
                {
                    item.TryGetFeatureValue(CommonUsages.triggerButton, out TriggerState);
                    item.TryGetFeatureValue(CommonUsages.trigger, out TriggerValue);
                }

                foreach (var item in controllers)
                {
                    item.TryGetFeatureValue(CommonUsages.gripButton, out GripState);
                    item.TryGetFeatureValue(CommonUsages.grip, out GripValue);
                }

                foreach (var item in controllers)
                {
                    item.TryGetFeatureValue(CommonUsages.primaryButton, out PrimaryButtonState);
                }

                foreach (var item in controllers)
                {
                    item.TryGetFeatureValue(CommonUsages.primary2DAxis, out Joystick);
                }
            }

            TriggerPress.Update(TriggerState);
            GripPress.Update(GripState);
            PrimaryButtonPress.Update(PrimaryButtonState);
            JoystickSwipe.Update(Joystick.x);
        }

        public bool Left
        {
            get
            {
                return poseDriver.poseSource == TrackedPose.LeftPose;
            }
        }

        public bool Right
        {
            get
            {
                return poseDriver.poseSource == TrackedPose.RightPose;
            }
        }

        private void FixedUpdate()
        {
            InputTracking.GetNodeStates(nodes);
            foreach (var item in nodes)
            {
                if(item.nodeType == node)
                {
                    item.TryGetVelocity(out velocity);
                }
            }
        }


    }
}
#endif //UNITY_EDITOR || !UNITY_WEBGL
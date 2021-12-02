// #if UNITY_WEBGL && !UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

// namespace UnityEngine
// {
//     // General approach...
//     // Externally, appear identical to Unity mic class
//     // How does this work?
//     // Devices identified by name, arr of names available
//     // Start() returns an audioclip which gets continually filled with data
//     // in the background. Difficulty here - we can't run an audio thread
//     // Internally, interop with browser microphone
//     public class Microphone
//     {
//         public static string[] devices;

//         public static void End(string deviceName)
//         {

//         }

//         public static void GetDeviceCaps(string deviceName, out int minFreq, out int maxFreq)
//         {

//         }

//         public static void GetPosition(string deviceName)
//         {

//         }

//         public static void IsRecording(string deviceName)
//         {

//         }

//         public static AudioClip Start(string deviceName, bool loop, int lengthSec, int frequency)
//         {

//         }
//     }
// }

namespace UnityEngine
{
    public class Microphone
    {
        [DllImport("__Internal")]
        public static extern void Init();

        [DllImport("__Internal")]
        public static extern void QueryAudioInput();

        [DllImport("__Internal")]
        private static extern int GetNumberOfMicrophones();

        [DllImport("__Internal")]
        private static extern string GetMicrophoneDeviceName(int index);

        [DllImport("__Internal")]
        private static extern float GetMicrophoneVolume(int index);

        private static List<Action> _sActions = new List<Action>();

        public static void Update()
        {
            for (int i = 0; i < _sActions.Count; ++i)
            {
                Action action = _sActions[i];
                action.Invoke();
            }
        }

        public static string[] devices
        {
            get
            {
                List<string> list = new List<string>();
                int size = GetNumberOfMicrophones();
                for (int index = 0; index < size; ++index)
                {
                    string deviceName = GetMicrophoneDeviceName(index);
                    list.Add(deviceName);
                }
                return list.ToArray();
            }
        }

        public static float[] volumes
        {
            get
            {
                List<float> list = new List<float>();
                int size = GetNumberOfMicrophones();
                for (int index = 0; index < size; ++index)
                {
                    float volume = GetMicrophoneVolume(index);
                    list.Add(volume);
                }
                return list.ToArray();
            }
        }

        public static bool IsRecording(string deviceName)
        {
            return false;
        }

        public static void GetDeviceCaps(string deviceName, out int minFreq, out int maxFreq)
        {
            minFreq = 0;
            maxFreq = 0;
        }

        public static void End(string deviceName)
        {
        }

        public static AudioClip Start(string deviceName, bool loop, int lengthSec, int frequency)
        {
            return null;
        }

        public static int GetPosition(string deviceName)
        {
            return 0;
        }
    }
}

// #endif
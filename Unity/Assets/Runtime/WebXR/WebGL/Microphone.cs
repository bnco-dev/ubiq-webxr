#if !UNITY_EDITOR && UNITY_WEBGL
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace UnityEngine
{
    public class Microphone
    {
        [DllImport("__Internal")]
        public static extern bool JS_Microphone_InitOrResumeContext();
        [DllImport("__Internal")]
        public static extern int JS_Microphone_GetSampleRate(int deviceIndex);
        [DllImport("__Internal")]
        public static extern int JS_Microphone_GetPosition(int deviceIndex);
        [DllImport("__Internal")]
        public static extern bool JS_Microphone_IsRecording(int deviceIndex);
        [DllImport("__Internal")]
        public static extern int JS_Microphone_GetBufferInstanceOfLastAudioClip();
        [DllImport("__Internal")]
        public static extern void JS_Microphone_Start(int deviceIndex, int bufferInstance, int samplesPerUpdate);
        [DllImport("__Internal")]
        public static extern void JS_Microphone_End(int deviceIndex);

        private static string[] _devices = {
            // We could get a list of devices with MediaDevices.enumerateDevices()
            // For now just do it the easy way, default device only
            ""
        };
        public static string[] devices { get { return _devices; } }

        public static void GetDeviceCaps(string deviceName, out int minFreq, out int maxFreq)
        {
            // According to https://www.w3.org/TR/webaudio/ WebAudio implementations
            // must support 8khz to 96khz. In practice seems to be best to let the
            // browser pick the sample rate it prefers to avoid audio glitches
            JS_Microphone_InitOrResumeContext();
            minFreq = maxFreq = JS_Microphone_GetSampleRate(0);
        }

        public static int GetPosition(string deviceName)
        {
            JS_Microphone_InitOrResumeContext();
            return JS_Microphone_GetPosition(0);
        }

        public static bool IsRecording(string deviceName)
        {
            JS_Microphone_InitOrResumeContext();
            return JS_Microphone_IsRecording(0);
        }

        // This interface is used to match the Unity Microphone class, but we
        // ignore all arguments and assume the following:
        // deviceName: Only one device is supported, the browser default
        // loop: Always true
        // lengthSec: Sample count is equal to 32 * samplesPerUpdate
        // frequency: Left to the context to decide
        // Returns a clip only if a new audio recording is started. If existing
        // recording going on, returns null. Client code has responsibility to
        // destroy returned AudioClip.
        public static AudioClip Start(string deviceName, bool loop, int lengthSec, int frequency)
        {
            if (!JS_Microphone_InitOrResumeContext() || JS_Microphone_IsRecording(0))
            {
                return null;
            }

            const int SAMPLES_PER_UPDATE_HIGHFREQ = 1024;
            const int SAMPLES_PER_UPDATE_LOWFREQ = 512;
            const int LENGTH_SAMPLES_MULTIPLIER = 32;

            var sampleRate = JS_Microphone_GetSampleRate(0);
            var samplesPerUpdate = sampleRate > 32000
                ? SAMPLES_PER_UPDATE_HIGHFREQ
                : SAMPLES_PER_UPDATE_LOWFREQ;
            var clip = AudioClip.Create(
                name: "Microphone AudioClip",
                lengthSamples: samplesPerUpdate*LENGTH_SAMPLES_MULTIPLIER,
                channels: 1,
                frequency: sampleRate,
                stream: false
            );
            var clipIndex = JS_Microphone_GetBufferInstanceOfLastAudioClip();
            JS_Microphone_Start(0,clipIndex,samplesPerUpdate);

            return clip;
        }

        public static void End(string deviceName)
        {
            JS_Microphone_End(0);
        }
    }
}
#endif
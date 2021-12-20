#if UNITY_WEBGL && !UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace UnityEngine
{
//     // General approach...
//     // Externally, appear identical to Unity mic class
//     // How does this work?
//     // Devices identified by name, arr of names available
//     // Start() returns an audioclip which gets continually filled with data
//     // in the background. Difficulty here - we can't run an audio thread
//     // Internally, interop with browser microphone
    public class Microphone
    {
        [DllImport("__Internal")]
        public static extern void JS_Microphone_Start(int deviceIndex, float[] data, int sampleRate);

        private class ReadonlySegmentRingBuffer
        {
            private int nextSegment;

            private const float SEGMENT_READ_MAGIC = 1000.0f;

            public int segmentSampleCount { get; private set; }
            public int segmentCount { get; private set; }

            public float[] buffer { get; private set; }

            public ReadonlySegmentRingBuffer (uint segmentSampleCount, uint segmentCount)
            {
                this.segmentSampleCount = (int)segmentSampleCount;
                this.segmentCount = (int)segmentCount;
                buffer = new float[segmentSampleCount * segmentCount];

                for (int i = 0; i < this.segmentCount; i++)
                {
                    var bufferIdx = i * segmentSampleCount;
                    buffer[bufferIdx] = SEGMENT_READ_MAGIC;
                }
            }

            public int Advance()
            {
                var nextBufferIdx = nextSegment*segmentSampleCount;
                if (buffer[nextBufferIdx] == SEGMENT_READ_MAGIC)
                {
                    return -1;
                }

                // Mark current segment read
                var currentSegment = nextSegment == 0 ? SEGMENT_COUNT - 1 : nextSegment - 1;
                var currentBufferIdx = currentSegment*segmentSampleCount;
                buffer[currentBufferIdx] = SEGMENT_READ_MAGIC;

                nextSegment = (nextSegment + 1) % SEGMENT_COUNT;
                return nextBufferIdx;
            }

            // public bool AdvanceSegment(float[] dest, int sampleOffset)
            // {
            //     if (!HasNewSegment())
            //     {
            //         return false;
            //     }

            //     currentSegment = (currentSegment + 1) % segmentCount;
            //     currentSegment++;

            //     var si = GetCurrentSegmentStartIndex();
            //     for (var i = 0; i < segmentSampleCount; i++)
            //     {
            //         dest[sampleOffset+i] = buffer[si+i];
            //     }

            //     buffer[si] = SEGMENT_READ_MAGIC;
            //     currentSegment = (currentSegment + 1) % segmentCount;
            //     return true;
            // }
        }

        private class WriteonlyRingBuffer
        {
            public float[] buffer { get; private set; }
            private int writeHead = 0;

            public WriteonlyRingBuffer (uint length)
            {
                buffer = new float[length];
            }

            public void Write (float[] src, int offset, int count)
            {
                for (int i = 0; i < count; i++)
                {
                    buffer[writeHead] = src[i + offset];
                    writeHead = (writeHead + 1) % buffer.Length;
                }
            }
        }

        private const int SEGMENT_SAMPLE_COUNT = 512;
        private const int SEGMENT_COUNT = 8;

        private static ReadonlySegmentRingBuffer microphoneBuffer;
        private static WriteonlyRingBuffer audioClipBuffer;
        private static AudioClip clip;

        private static float[] dataBuff;


        // We could get a list of devices with MediaDevices.enumerateDevices()
        // For now just do it the easy way, default device only
        private static string[] _devices = {
            "default"
        };
        public static string[] devices { get { return _devices; } }

        public static void End(string deviceName)
        {

        }

        public static void GetDeviceCaps(string deviceName, out int minFreq, out int maxFreq)
        {
            // According to https://www.w3.org/TR/webaudio/
            // WebAudio implementations must at least support...
            minFreq = 8000;
            maxFreq = 96000;
        }

        public static int GetPosition(string deviceName)
        {
            return -1;
        }

        public static bool IsRecording(string deviceName)
        {
            return false;
        }

        // For now, requires this to be called frequently on the main thread
        // Should find a better way to do this...
        public static int Update(float time)
        {
            if (clip == null)
            {
                return 0;
            }

            // var writes = 0;
            // var offset = microphoneBuffer.Advance();
            // while (offset >= 0)
            // {
            //     audioClipBuffer.Write(microphoneBuffer.buffer,offset,
            //         microphoneBuffer.segmentSampleCount);
            //     offset = microphoneBuffer.Advance();
            //     writes++;
            // }

            // if (writes > 0)
            // {
            //     clip.SetData(audioClipBuffer.buffer,0);
            // }

            // return writes;

            // var waveFreq = (time % 5) * 500.0f;

            // for (int i = 0; i < dataBuff.Length; i++) {
            //     dataBuff[i] = Mathf.Sin(Mathf.PI * (i / (float)clip.frequency) * waveFreq);
            // }
            // clip.SetData(dataBuff,0);

            var waveFreq = (time % 5) * 500.0f;

            for (int i = 0; i < dataBuff.Length; i++) {
                dataBuff[i] = Mathf.Sin(Mathf.PI * (i / (float)clip.frequency) * waveFreq);
            }
            clip.SetData(dataBuff,0);

            return 0;
        }

        public static AudioClip Start(string deviceName, bool loop, int lengthSec, int frequency)
        {
            if (clip != null)
            {
                return clip;
            }

            clip = AudioClip.Create(
                name: "Microphone AudioClip",
                lengthSamples: 1024,//lengthSec*frequency,
                channels: 1,
                frequency: frequency,
                stream: false
            );
            microphoneBuffer = new ReadonlySegmentRingBuffer(
                SEGMENT_SAMPLE_COUNT,SEGMENT_COUNT);
            audioClipBuffer = new WriteonlyRingBuffer((uint)clip.samples);

            dataBuff = new float[clip.samples];

            JS_Microphone_Start(0,microphoneBuffer.buffer,frequency);

            return clip;
        }
    }
}
#endif

// namespace UnityEngine
// {
//     public class Microphone
//     {
//         [DllImport("__Internal")]
//         public static extern void Init();

//         [DllImport("__Internal")]
//         public static extern void QueryAudioInput();

//         [DllImport("__Internal")]
//         private static extern int GetNumberOfMicrophones();

//         [DllImport("__Internal")]
//         private static extern string GetMicrophoneDeviceName(int index);

//         [DllImport("__Internal")]
//         private static extern float GetMicrophoneVolume(int index);

//         private static List<Action> _sActions = new List<Action>();

//         public static void Update()
//         {
//             for (int i = 0; i < _sActions.Count; ++i)
//             {
//                 Action action = _sActions[i];
//                 action.Invoke();
//             }
//         }

//         public static string[] devices
//         {
//             get
//             {
//                 List<string> list = new List<string>();
//                 int size = GetNumberOfMicrophones();
//                 for (int index = 0; index < size; ++index)
//                 {
//                     string deviceName = GetMicrophoneDeviceName(index);
//                     list.Add(deviceName);
//                 }
//                 return list.ToArray();
//             }
//         }

//         public static float[] volumes
//         {
//             get
//             {
//                 List<float> list = new List<float>();
//                 int size = GetNumberOfMicrophones();
//                 for (int index = 0; index < size; ++index)
//                 {
//                     float volume = GetMicrophoneVolume(index);
//                     list.Add(volume);
//                 }
//                 return list.ToArray();
//             }
//         }

//         public static bool IsRecording(string deviceName)
//         {
//             return false;
//         }

//         public static void GetDeviceCaps(string deviceName, out int minFreq, out int maxFreq)
//         {
//             minFreq = 0;
//             maxFreq = 0;
//         }

//         public static void End(string deviceName)
//         {
//         }

//         public static AudioClip Start(string deviceName, bool loop, int lengthSec, int frequency)
//         {
//             return null;
//         }

//         public static int GetPosition(string deviceName)
//         {
//             return 0;
//         }
//     }
// }

// // #endif
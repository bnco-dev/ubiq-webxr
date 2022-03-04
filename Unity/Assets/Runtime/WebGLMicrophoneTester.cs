using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class WebGLMicrophoneTester : MonoBehaviour
{
    private AudioSource audioSource;

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.loop = true;
        audioSource.ignoreListenerPause = true;
    }

    private void StartMic()
    {
        audioSource.clip = Microphone.Start(
            deviceName:"",loop:true,lengthSec:1,frequency:16000);

        if (audioSource.clip == null) {
            return;
        }

    }

    const float latency = .1f;
    private float lastUpdateTime;
    private void Update()
    {
// #if UNITY_WEBGL && !UNITY_EDITOR

        if (audioSource.clip == null) {
            StartMic();
            return;
        }

        if (Input.GetKeyDown(KeyCode.Return))
        {
            StartMic();
            Debug.Log("startedMic");
        }

        if (audioSource && !audioSource.isPlaying
            && Microphone.GetPosition("") > audioSource.clip.frequency * latency)
        {
            Debug.Log("Playing!");
            audioSource.Play();
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            Microphone.End("");
            Debug.Log("endedMic");
        }
            // if (Microphone.GetPosition() > audios)
            // Debug.Log(Microphone.GetAverage());
            // if (Time.realtimeSinceStartup > lastUpdateTime + 5.0f)
            // {
            //     // audioSource.Pause();
            //     // var writes = Microphone.Update(Time.realtimeSinceStartup);
            //     // audioSource.UnPause();
            //     Debug.Log((Time.realtimeSinceStartup % 5) * 500.0f);
            //     if (writes > 0)
            //     {
            //         Debug.Log(Time.frameCount + " WRITES:" + writes);
            //     }
            // }
        // }
// #endif
    }
}

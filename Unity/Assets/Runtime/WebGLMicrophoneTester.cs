using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class WebGLMicrophoneTester : MonoBehaviour
{
    private AudioSource audioSource;

    private void Start()
    {
        Invoke("StartMic",20.0f);
    }

    private void StartMic()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.clip = Microphone.Start(
            deviceName:"",loop:true,lengthSec:1,frequency:16000);
        Debug.Log(audioSource.clip.samples);
        audioSource.loop = true;
        audioSource.ignoreListenerPause = true;
        audioSource.Play();

    }

    private float lastTime = -1;
    private void Update()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        if (audioSource)
        {
            // if (Input.GetKeyDown(KeyCode.Space) && Time.realtimeSinceStartup > lastTime + 2.0f)
            // {
            //     lastTime = Time.realtimeSinceStartup;
                // audioSource.Stop();
                var writes = Microphone.Update(Time.realtimeSinceStartup);
                // audioSource.Play();
                // Debug.Log((Time.realtimeSinceStartup % 5) * 500.0f);
                if (writes > 0)
                {
                    Debug.Log(Time.frameCount + " WRITES:" + writes);
                }
            }
        }
#endif
    }
}

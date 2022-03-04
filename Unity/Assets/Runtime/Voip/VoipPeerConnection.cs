using System;
using System.Net;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;
using UnityEngine;
using UnityEngine.Events;
using SIPSorcery.Net;
using Ubiq.Messaging;
using Ubiq.Voip.Implementations;

namespace Ubiq.Voip
{
    public enum IceConnectionState
    {
        closed = 0,
        failed = 1,
        disconnected = 2,
        @new = 3,
        checking = 4,
        connected = 5
    }

    public enum PeerConnectionState
    {
        closed = 0,
        failed = 1,
        disconnected = 2,
        @new = 3,
        connecting = 4,
        connected = 5
    }

    public interface IVoipAudioSink
    {
        public struct Stats
        {
            public readonly float volume;
            public readonly int samples;
        }

        Vector3 position { get; set; }
        int sampleRate { get; }

        event Action<Stats> onStats;
    }

    public interface IVoipAudioSource
    {
        Vector3 position { get; set; }
    }

    [NetworkComponentId(typeof(VoipPeerConnection), 78)]
    public class VoipPeerConnection : MonoBehaviour, INetworkComponent, INetworkObject {

        // public IVoipAudioSource audioSource { get { return impl?.audioSource; } }
        // public IVoipAudioSink audioSink { get { return impl?.audioSink; } }
        public VoipMicrophoneInput audioSource { get; private set; }
        public VoipAudioSourceOutput audioSink { get; private set; }
        public NetworkId Id { get; protected set; }
        public string peerUuid { get; protected set; }

        public IceConnectionState iceConnectionState { get; private set; } = IceConnectionState.@new;
        public PeerConnectionState peerConnectionState { get; private set; } = PeerConnectionState.@new;

        [Serializable] public class IceConnectionStateEvent : UnityEvent<IceConnectionState> { }
        [Serializable] public class PeerConnectionStateEvent : UnityEvent<PeerConnectionState> { }

        public IceConnectionStateEvent OnIceConnectionStateChanged = new IceConnectionStateEvent();
        public PeerConnectionStateEvent OnPeerConnectionStateChanged = new PeerConnectionStateEvent();

        private NetworkContext context;
        private IPeerConnectionImpl impl;

        private bool isSetup;

        private void OnDestroy()
        {
            if (impl != null)
            {
                impl.Dispose();
                impl = null;
            }
        }

        // debug setremote
        public void SetRemotePeerPosition(Vector3 worldPosition, Quaternion worldRotation)
        {
            if (context == null)
            {
                return;
            }

            var avatarManager = context.scene.GetComponentInChildren<Ubiq.Avatars.AvatarManager>();
            if (!avatarManager)
            {
                return;
            }

            var localVoipAvatar = avatarManager.LocalAvatar.GetComponent<Ubiq.Avatars.VoipAvatar>();
            if (!localVoipAvatar)
            {
                return;
            }

            var listener = localVoipAvatar.audioSourcePosition;
            var relativePosition = listener.InverseTransformPoint(worldPosition);
            var relativeRotation = Quaternion.Inverse(listener.rotation) * worldRotation;

            impl.SetRemotePeerRelativePosition(relativePosition,relativeRotation);
        }
        // debug setremote

        // todo
        public VoipAudioSourceOutput.Stats GetLastFrameStats ()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (impl != null)
            {
                return impl.GetStats();
            }
            else
            {
                return new VoipAudioSourceOutput.Stats {samples = 0, volume = 0};
            }
#else
            if (audioSink)
            {
                return audioSink.lastFrameStats;
            }
            else
            {
                return new VoipAudioSourceOutput.Stats {samples = 0, volume = 0};
            }
#endif
        }

        public void Setup (NetworkId objectId, string peerUuid,
            bool polite, List<IceServerDetails> iceServers,
            VoipMicrophoneInput source, VoipAudioSourceOutput sink)
        {
            if (isSetup)
            {
                return;
            }

            this.Id = objectId;
            this.peerUuid = peerUuid;
            this.context = NetworkScene.Register(this);
            this.audioSource = source;
            this.audioSink = sink;

#if UNITY_WEBGL && !UNITY_EDITOR
            this.impl = new Ubiq.WebXR.WebXrPeerConnectionImpl();
#else
            this.impl = new Ubiq.Voip.SipSorcery.SipSorceryPeerConnectionImpl();
#endif

            impl.signallingMessageEmitted += OnImplMessageEmitted;
            impl.iceConnectionStateChanged += OnImplIceConnectionStateChanged;
            impl.peerConnectionStateChanged += OnImplPeerConnectionStateChanged;

            impl.Setup(this,polite,iceServers);
            isSetup = true;
        }

        public void ProcessMessage(ReferenceCountedSceneGraphMessage data)
        {
            if (impl != null)
            {
                var message = data.FromJson<SignallingMessage>();
                impl.ProcessSignallingMessage(message);
            }
        }

        private void OnImplMessageEmitted (SignallingMessage message)
        {
            context.SendJson(message);
        }

        private void OnImplIceConnectionStateChanged (IceConnectionState state)
        {
            iceConnectionState = state;
            OnIceConnectionStateChanged.Invoke(state);
        }

        private void OnImplPeerConnectionStateChanged (PeerConnectionState state)
        {
            peerConnectionState = state;
            OnPeerConnectionStateChanged.Invoke(state);
        }
    }
}
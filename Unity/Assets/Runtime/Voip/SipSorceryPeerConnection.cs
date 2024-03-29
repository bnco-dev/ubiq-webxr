using System;
using System.Net;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using UnityEngine;
using SIPSorcery.Net;
using Ubiq.Voip.Implementations;

namespace Ubiq.Voip.SipSorcery
{
    public class SipSorceryPeerConnectionImpl : IPeerConnectionImpl {

        public event MessageEmittedDelegate signallingMessageEmitted;
        public event IceConnectionStateChangedDelegate iceConnectionStateChanged;
        public event PeerConnectionStateChangedDelegate peerConnectionStateChanged;

        private ConcurrentQueue<Action> mainThreadActions = new ConcurrentQueue<Action>();
        private Queue<SignallingMessage> messageQueue = new Queue<SignallingMessage>();
        private Task<RTCPeerConnection> setupTask;

        private VoipPeerConnection context;

        // SipSorcery Peer Connection
        private RTCPeerConnection rtcPeerConnection;
        private Coroutine updateCoroutine;

        public async void Dispose()
        {
            if (updateCoroutine != null)
            {
                context.StopCoroutine(updateCoroutine);
                updateCoroutine = null;
            }

            if (setupTask != null)
            {
                await setupTask.ConfigureAwait(false);
                setupTask.Result.Dispose();
            }
        }

        public void Setup(VoipPeerConnection context,
            bool polite, List<IceServerDetails> iceServers)
        {
            if (setupTask != null)
            {
                // Already setup or setup in progress
                return;
            }

            this.context = context;

            // Copy ice servers before entering multithreaded context
            var iceServersCopy = new List<IceServerDetails>();
            for (int i = 0; i < iceServers.Count; i++)
            {
                iceServersCopy.Add(new IceServerDetails(
                    uri: iceServers[i].uri,
                    username: iceServers[i].username,
                    password: iceServers[i].password));
            }

            setupTask = Task.Run(() => DoSetup(polite,iceServersCopy));
            updateCoroutine = context.StartCoroutine(Update());
        }

        public void ProcessSignallingMessage (SignallingMessage message)
        {
            // Buffer messages until the RtcPeerConnection is initialised
            if (setupTask == null || !setupTask.IsCompleted)
            {
                messageQueue.Enqueue(message);
            }
            else
            {
                DoReceiveSignallingMessage(message);
            }
        }

        private void DoReceiveSignallingMessage(SignallingMessage message)
        {
            switch(message.type)
            {
                case SignallingMessage.Type.SessionDescription: ReceiveSessionDescription(message); break;
                case SignallingMessage.Type.IceCandidate: ReceiveIceCandidate(message); break;
            }
        }

        // todo
        public VoipAudioSourceOutput.Stats GetStats ()
        {
            return new VoipAudioSourceOutput.Stats {samples = 0, volume = 0};
        }

        // debug setremote
        public void SetRemotePeerRelativePosition (Vector3 position, Quaternion rotation) {}
        // debug setremote

        private async Task<RTCPeerConnection> DoSetup(bool polite,
            List<IceServerDetails> iceServers)
        {
            // Convert to sipsorcery ice server format
            var ssIceServers = new List<RTCIceServer>();
            for (int i = 0; i < iceServers.Count; i++)
            {
                if (string.IsNullOrEmpty(iceServers[i].username) ||
                    string.IsNullOrEmpty(iceServers[i].password))
                {
                    ssIceServers.Add(new RTCIceServer {
                        urls = iceServers[i].uri
                    });
                }
                else
                {
                    ssIceServers.Add(new RTCIceServer {
                        urls = iceServers[i].uri,
                        username = iceServers[i].username,
                        credential = iceServers[i].password,
                        credentialType = RTCIceCredentialType.password
                    });
                }
            }

            var pc = new RTCPeerConnection(new RTCConfiguration
            {
                iceServers = ssIceServers,
            });

// todo
            pc.addTrack(new MediaStreamTrack(
                context.audioSource.GetAudioSourceFormats(),
                MediaStreamStatusEnum.SendRecv));
            pc.OnAudioFormatsNegotiated += (formats) =>
            {
                context.audioSource.SetAudioSourceFormat(formats[0]);
                context.audioSink.SetAudioSinkFormat(formats[0]);
            };

            context.audioSource.OnAudioSourceEncodedSample += pc.SendAudio;
// todo

            pc.onconnectionstatechange += (state) =>
            {
                mainThreadActions.Enqueue(() =>
                {
                    switch(state)
                    {
                        case RTCPeerConnectionState.closed : peerConnectionStateChanged(PeerConnectionState.closed); break;
                        case RTCPeerConnectionState.failed : peerConnectionStateChanged(PeerConnectionState.failed); break;
                        case RTCPeerConnectionState.disconnected : peerConnectionStateChanged(PeerConnectionState.disconnected); break;
                        case RTCPeerConnectionState.@new : peerConnectionStateChanged(PeerConnectionState.@new); break;
                        case RTCPeerConnectionState.connecting : peerConnectionStateChanged(PeerConnectionState.connecting); break;
                        case RTCPeerConnectionState.connected : peerConnectionStateChanged(PeerConnectionState.connected); break;
                    }
                    Debug.Log($"Peer connection state change to {state}.");
                });

                if (state == RTCPeerConnectionState.connected)
                {
                    // audioSource.OnAudioSourceEncodedSample += pc.SendAudio;
                }
                else if (state == RTCPeerConnectionState.closed || state == RTCPeerConnectionState.failed)
                {
                    // audioSource.OnAudioSourceEncodedSample -= pc.SendAudio;
                }
            };

            pc.onicecandidate += (iceCandidate) =>
            {
                if (iceCandidate != null && !string.IsNullOrEmpty(iceCandidate.candidate))
                {
                    mainThreadActions.Enqueue(() => SendIceCandidate(iceCandidate));
                }
            };

            pc.OnRtpPacketReceived += (IPEndPoint rep, SDPMediaTypesEnum media, RTPPacket rtpPkt) =>
            {
                if (media == SDPMediaTypesEnum.audio)
                {
                    // todo
                    context.audioSink.GotAudioRtp(rep, rtpPkt.Header.SyncSource, rtpPkt.Header.SequenceNumber, rtpPkt.Header.Timestamp, rtpPkt.Header.PayloadType, rtpPkt.Header.MarkerBit == 1, rtpPkt.Payload);
                }
            };

            // Diagnostics.
            pc.OnReceiveReport += (re, media, rr) => mainThreadActions.Enqueue(
                () => Debug.Log($"RTCP Receive for {media} from {re}\n{rr.GetDebugSummary()}"));
            pc.OnSendReport += (media, sr) => mainThreadActions.Enqueue(
                () => Debug.Log($"RTCP Send for {media}\n{sr.GetDebugSummary()}"));
            pc.GetRtpChannel().OnStunMessageReceived += (msg, ep, isRelay) => mainThreadActions.Enqueue(
                () => Debug.Log($"STUN {msg.Header.MessageType} received from {ep}."));
            pc.oniceconnectionstatechange += (state) => mainThreadActions.Enqueue(() =>
            {
                switch(state)
                {
                    case RTCIceConnectionState.closed : iceConnectionStateChanged(IceConnectionState.closed); break;
                    case RTCIceConnectionState.failed : iceConnectionStateChanged(IceConnectionState.failed); break;
                    case RTCIceConnectionState.disconnected : iceConnectionStateChanged(IceConnectionState.disconnected); break;
                    case RTCIceConnectionState.@new : iceConnectionStateChanged(IceConnectionState.@new); break;
                    case RTCIceConnectionState.checking : iceConnectionStateChanged(IceConnectionState.checking); break;
                    case RTCIceConnectionState.connected : iceConnectionStateChanged(IceConnectionState.connected); break;
                }
                Debug.Log($"ICE connection state change to {state}.");
            });

            if (!polite)
            {
                var offer = pc.createOffer();
                await pc.setLocalDescription(offer).ConfigureAwait(false);
                mainThreadActions.Enqueue(() => SendOffer(offer));
            }

            return pc;
        }

        private IEnumerator Update()
        {
            while(setupTask == null || !setupTask.IsCompleted)
            {
                yield return null;
            }

            rtcPeerConnection = setupTask.Result;

            // Process buffered messages now that the peer connection is initialised
            while (messageQueue.Count > 0)
            {
                DoReceiveSignallingMessage(messageQueue.Dequeue());
            }

            while(true)
            {
                while (mainThreadActions.TryDequeue(out Action action))
                {
                    action();
                }

                yield return null;
            }
        }

        private void SendOffer(RTCSessionDescriptionInit ssOffer)
        {
            var offer = new SessionDescriptionArgs();
            offer.sdp = ssOffer.sdp;
            switch(ssOffer.type)
            {
                case RTCSdpType.answer : offer.type = SessionDescriptionArgs.TYPE_ANSWER; break;
                case RTCSdpType.offer : offer.type = SessionDescriptionArgs.TYPE_OFFER; break;
                case RTCSdpType.pranswer : offer.type = SessionDescriptionArgs.TYPE_PRANSWER; break;
                case RTCSdpType.rollback : offer.type = SessionDescriptionArgs.TYPE_ROLLBACK; break;
            }
            signallingMessageEmitted(SignallingMessage.FromSessionDescription(offer));
        }

        private void SendIceCandidate(RTCIceCandidate iceCandidate)
        {
            var args = new IceCandidateArgs
            {
                candidate = iceCandidate.candidate,
                sdpMid = iceCandidate.sdpMid,
                sdpMLineIndex = iceCandidate.sdpMLineIndex,
                usernameFragment = iceCandidate.usernameFragment
            };
            signallingMessageEmitted(SignallingMessage.FromIceCandidate(args));
        }

        private void ReceiveSessionDescription(SignallingMessage message)
        {
            if (message.ParseSessionDescription(out var offer))
            {
                // Convert to sipsorcery format
                var ssOffer = new RTCSessionDescriptionInit();
                ssOffer.sdp = offer.sdp;
                switch(offer.type)
                {
                    case SessionDescriptionArgs.TYPE_ANSWER : ssOffer.type = RTCSdpType.answer; break;
                    case SessionDescriptionArgs.TYPE_OFFER : ssOffer.type = RTCSdpType.offer; break;
                    case SessionDescriptionArgs.TYPE_PRANSWER : ssOffer.type = RTCSdpType.pranswer; break;
                    case SessionDescriptionArgs.TYPE_ROLLBACK : ssOffer.type = RTCSdpType.rollback; break;
                }

                Debug.Log($"Got remote SDP, type {ssOffer.type}");
                switch(ssOffer.type)
                {
                    case RTCSdpType.answer:
                    {
                        var result = rtcPeerConnection.setRemoteDescription(ssOffer);
                        if (result != SetDescriptionResultEnum.OK)
                        {
                            Debug.Log($"Failed to set remote description, {result}.");
                            rtcPeerConnection.Close("Failed to set remote description");
                        }
                        break;
                    }
                    case RTCSdpType.offer:
                    {
                        var result = rtcPeerConnection.setRemoteDescription(ssOffer);
                        if (result != SetDescriptionResultEnum.OK)
                        {
                            Debug.Log($"Failed to set remote description, {result}.");
                            rtcPeerConnection.Close("Failed to set remote description");
                        }

                        var answerSdp = rtcPeerConnection.createAnswer();
                        rtcPeerConnection.setLocalDescription(answerSdp);
                        SendOffer(answerSdp);
                        break;
                    }
                }
            }
        }

        private void ReceiveIceCandidate(SignallingMessage message)
        {
            if (message.ParseIceCandidate(out var iceCandidate))
            {
                // Convert to sipsorcery format
                var ssIceCandidate = new RTCIceCandidateInit
                {
                    candidate = iceCandidate.candidate,
                    sdpMid = iceCandidate.sdpMid,
                    sdpMLineIndex = iceCandidate.sdpMLineIndex,
                    usernameFragment = iceCandidate.usernameFragment
                };

                Debug.Log($"Got remote Ice Candidate, uri {ssIceCandidate.candidate}");
                rtcPeerConnection.addIceCandidate(ssIceCandidate);
            }
        }
    }
}
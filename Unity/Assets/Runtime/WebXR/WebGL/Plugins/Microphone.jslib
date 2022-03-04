var Microphone = {
    // Setup the audiocontext and all required objects. Should be called before
    // any of the other js microphone interface functions in this file. If this
    // returns true, it is possible to start an audio recording with Start()
    JS_Microphone_InitOrResumeContext: function() {
        if (!WEBAudio || WEBAudio.audioWebEnabled == 0) {
            // No WEBAudio object (Unity version changed?)
            return false;
        }

        navigator.getUserMedia =
            navigator.getUserMedia || navigator.webkitGetUserMedia ||
            navigator.mozGetUserMedia || navigator.msGetUserMedia;
        if (!navigator.getUserMedia){
            return false;
        }

        var ctx = document.unityMicrophoneInteropContext;
        if (!ctx) {
            document.unityMicrophoneInteropContext = {};
            ctx = document.unityMicrophoneInteropContext;
        }

        if (!ctx.audioContext || ctx.audioContext.state == "closed"){
            ctx.audioContext = new AudioContext();
        }

        if (ctx.audioContext.state == "suspended") {
            ctx.audioContext.resume();
        }

        if (ctx.audioContext.state == "suspended") {
            return false;
        }

        return true;
    },

    // Returns the index of the most recently created audio clip so we can
    // write to it. Should be called immediately after creating the clip and
    // the value stored for indexing purposes.
    // Relies on undocumented/unexposed js code within Unity's WebGL code,
    // so may break with later versions
    JS_Microphone_GetBufferInstanceOfLastAudioClip: function() {
        if (WEBAudio && WEBAudio.audioInstanceIdCounter) {
            return WEBAudio.audioInstanceIdCounter;
        }
        return -1;
    },

    JS_Microphone_IsRecording: function(deviceIndex) {
        var ctx = document.unityMicrophoneInteropContext;
        return ctx && ctx.stream;
    },

    // Get the current index of the last recorded sample
    JS_Microphone_GetPosition: function(deviceIndex) {
        var ctx = document.unityMicrophoneInteropContext;
        if (ctx && ctx.stream) {
            return ctx.stream.currentPosition;
        }
        return -1;
    },

    // Get the sample rate for this device
    // According to https://www.w3.org/TR/webaudio/ WebAudio implementations
    // must support 8khz to 96khz. In practice seems to be best to let the
    // browser pick the sample rate it prefers to avoid audio glitches
    JS_Microphone_GetSampleRate: function(deviceIndex) {
        var ctx = document.unityMicrophoneInteropContext;
        if (ctx && ctx.audioContext.state == "running") {
            return ctx.audioContext.sampleRate;
        }
        return -1;
    },

    // Note samplesPerUpdate balances performance against latency, must be one of:
    // 256, 512, 1024, 2048, 4096, 8192, 16384
    // Note also that the clip sample count for the buffer instance should be
    // a multiple of samplesPerUpdate
    JS_Microphone_Start: function(deviceIndex,bufferInstance,samplesPerUpdate) {
        var ctx = document.unityMicrophoneInteropContext;
        if (ctx && ctx.stream) {
            // We are already recording
            return false;
        }

        var sound = WEBAudio.audioInstances[bufferInstance];
        if (!sound || !sound.buffer) {
            // No buffer for the given bufferInstance (Unity version changed?)
            return false;
        }

        var outputArray = sound.buffer.getChannelData(0);
        var outputArrayLen = outputArray.length;

        navigator.getUserMedia(
            {audio:true},
            function(userMediaStream) {
                var stream = {};
                stream.userMediaStream = userMediaStream;
                stream.microphoneSource = ctx.audioContext.createMediaStreamSource(userMediaStream);
                stream.processorNode = ctx.audioContext.createScriptProcessor(samplesPerUpdate, 1, 1);
                stream.currentPosition = 0;
                stream.processorNode.onaudioprocess = function(event) {

                    // Simple version for minimum delay
                    // Assumes clip length is a multiple of samplesPerUpdate
                    var inputArray = event.inputBuffer.getChannelData(0);
                    var pos = stream.currentPosition;
                    outputArray.set(inputArray,pos);
                    pos = (pos + samplesPerUpdate) % outputArrayLen;
                    stream.currentPosition = pos;
                }

                stream.microphoneSource.connect(stream.processorNode);

                // Add a zero gain node and connect to destination
                // Some browsers seem to ignore a solo processor node
                stream.gainNode = new GainNode(ctx.audioContext,{gain:0});
                stream.processorNode.connect(stream.gainNode);
                stream.gainNode.connect(ctx.audioContext.destination);

                ctx.stream = stream;
            },
            function(e) {
                alert('Error capturing audio.');
            }
        );
    },

    JS_Microphone_End: function(deviceIndex) {
        var ctx = document.unityMicrophoneInteropContext;
        if (ctx && ctx.stream) {
            ctx.stream.userMediaStream.getTracks().forEach(
                function(track) {
                    track.stop();
                }
            );

            ctx.stream.gainNode.disconnect();
            ctx.stream.processorNode.disconnect();
            ctx.stream.microphoneSource.disconnect();

            delete ctx.stream;
        }
    },

    // var audioInput = null,
    //     microphone_stream = null,
    //     gain_node = null,
    //     script_processor_node = null,
    //     script_processor_fft_node = null,
    //     analyserNode = null;



    // ---

    // function show_some_data(given_typed_array, num_row_to_display, label) {

    //     var size_buffer = given_typed_array.length;
    //     var index = 0;
    //     var max_index = num_row_to_display;

    //     console.log("__________ " + label);

    //     for (; index < max_index && index < size_buffer; index += 1) {

    //         console.log(given_typed_array[index]);
    //     }
    // }

    // function process_microphone_buffer(event) { // invoked by event loop

    //     var i, N, inp, microphone_output_buffer;

    //     microphone_output_buffer = event.inputBuffer.getChannelData(0); // just mono - 1 channel for now

    //     // microphone_output_buffer  <-- this buffer contains current gulp of data size BUFF_SIZE

    //     // show_some_data(microphone_output_buffer, 5, "from getChannelData");
    // }

    // function start_microphone(stream){

    // //   gain_node = audioContext.createGain();
    // //   gain_node.connect( audioContext.destination );

    //   microphone_stream = audioContext.createMediaStreamSource(stream);
    // //   microphone_stream.connect(gain_node);

    //   script_processor_node = audioContext.createScriptProcessor(BUFF_SIZE, 1, 1);
    //   script_processor_node.onaudioprocess = process_microphone_buffer;

    //   microphone_stream.connect(script_processor_node);

      // --- enable volume control for output speakers

    //   document.getElementById('volume').addEventListener('change', function() {

    //       var curr_volume = this.value;
    //       gain_node.gain.value = curr_volume;

    //       console.log("curr_volume ", curr_volume);
    //   });

      // --- setup FFT

    //   script_processor_fft_node = audioContext.createScriptProcessor(2048, 1, 1);
    //   script_processor_fft_node.connect(gain_node);

    //   analyserNode = audioContext.createAnalyser();
    //   analyserNode.smoothingTimeConstant = 0;
    //   analyserNode.fftSize = 2048;

    //   microphone_stream.connect(analyserNode);

    //   analyserNode.connect(script_processor_fft_node);

    //   script_processor_fft_node.onaudioprocess = function() {

    //     // get the average for the first channel
    //     var array = new Uint8Array(analyserNode.frequencyBinCount);
    //     analyserNode.getByteFrequencyData(array);

    //     // draw the spectrogram
    //     if (microphone_stream.playbackState == microphone_stream.PLAYING_STATE) {

    //         show_some_data(array, 5, "from fft");
    //     }
    //   };

};

mergeInto(LibraryManager.library, Microphone);
var Microphone = {

    JS_Microphone_Start: function(deviceIndex,floatBuffer,sampleRate) {
        // FloatBuffer must be 512*8 samples in size
        const FLOAT_BUFFER_SEGMENT_SAMPLE_COUNT = 512;
        const FLOAT_BUFFER_SEGMENT_COUNT = 8;
        const FLOAT_BYTE_LENGTH = 4;

        if (!document.unityMicrophoneInterop){
            document.unityMicrophoneInterop = {};
        }

        if (document.unityMicrophoneInterop.audioContext){
            document.unityMicrophoneInterop.audioContext.close();
        }

        document.unityMicrophoneInterop.audioContext =
            new AudioContext({sampleRate:sampleRate});
        //document.unityMicrophoneInterop.getUserMedia =
        //    navigator.getUserMedia || navigator.webkitGetUserMedia ||
        //    navigator.mozGetUserMedia || navigator.msGetUserMedia;
        //document.unityMicrophoneInterop.currentSegment = 0;

        navigator.getUserMedia =
            navigator.getUserMedia || navigator.webkitGetUserMedia ||
            navigator.mozGetUserMedia || navigator.msGetUserMedia;
        if (navigator.getUserMedia){
            navigator.getUserMedia(
                {audio:true},
                function(stream) {
                    console.log("onstream");
                    document.unityMicrophoneInterop.microphoneStream = document.unityMicrophoneInterop.
                        audioContext.createMediaStreamSource(stream);
                    document.unityMicrophoneInterop.processorNode = document.unityMicrophoneInterop.
                        audioContext.createScriptProcessor(FLOAT_BUFFER_SEGMENT_SAMPLE_COUNT, 1, 1);
                    document.unityMicrophoneInterop.currentSegment = 0;
                    document.unityMicrophoneInterop.processorNode.onaudioprocess = function(event) {
                        console.log("onaudioprocess");
                        var p = floatBuffer +
                            FLOAT_BYTE_LENGTH * FLOAT_BUFFER_SEGMENT_SAMPLE_COUNT *
                            document.unityMicrophoneInterop.currentSegment;
                        var outArr = new Float32Array(Module.HEAPF32.buffer,
                            p,FLOAT_BUFFER_SEGMENT_SAMPLE_COUNT);
                        outArr.set(event.inputBuffer.getChannelData(0));
                        document.unityMicrophoneInterop.currentSegment =
                            (document.unityMicrophoneInterop.currentSegment + 1)
                            % FLOAT_BUFFER_SEGMENT_COUNT;
                    }

                    document.unityMicrophoneInterop.microphoneStream.connect(document.unityMicrophoneInterop.processorNode);
                    document.unityMicrophoneInterop.gainNode = new GainNode(document.unityMicrophoneInterop.audioContext,{gain:0});
                    document.unityMicrophoneInterop.processorNode.connect(document.unityMicrophoneInterop.gainNode);
                    document.unityMicrophoneInterop.gainNode.connect(document.unityMicrophoneInterop.audioContext.destination);
                },
                function(e) {
                    alert('Error capturing audio.');
                }
            );

        } else { alert('getUserMedia not supported in this browser.'); }
    },

    JS_Microphone_Stop: function(deviceIndex) {

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
// Demo only - Only handles one websocket, not thread safe
var JsWebSocketPlugin = {

    ws: undefined,
    //sendBuffer: new Int8Array(256),
    //recvBuffer: new Int8Array(50000),
    //recvMessages: [],

    JsWebSocketPlugin_TryConnect: function(url) {
        if (Module.myws !== undefined) {
            return false;
        }

        //this.sendBuffer = new Int8Array(256);
        //this.recvBuffer = new Int8Array(50000);

        Module.myws = new WebSocket(UTF8ToString(url));
        Module.myws.binaryType = 'arraybuffer';
        Module.myws.messages = [];
        Module.myws.onmessage = function(ev) {
            Module.myws.messages.push(new Uint8Array(ev.data));
            // var buffer = new Uint8Array(ev.data);
            // try {

                // var lastMessage = this.recvMessages[recvMessages.length-1];
                // var message = {
                //     start: (lastMessage.start + lastMessage.length) % recvBuffer.length,
                //     length: buffer.length
                // };

                // if (message.start + message.length > recvBuffer.length) {
                //     // Wrap into receive buffer
                //     var beforeLength = recvBuffer.length - message.start;
                //     this.recvBuffer.set(
                //         new Int8Array(buffer,0,beforeLength),
                //         message.start
                //     );
                //     this.recvBuffer.set(
                //         new Int8Array(buffer,beforeLength,message.length - beforeLength),
                //         0
                //     );
                // } else {
                //     // No need to wrap
                //     this.recvBuffer.set(buffer,message.start,message.length);
                // }

                // recvMessages.push({
                //     start: (lastMessage.start + lastMessage.length) % recvBuffer.length,
                //     length: buffer.length
                // });

                // // this.recvBuffer.set(buffer,this.recvMessageOffsets[this.recvMessageCount]);
                // // this.recvMessageCount++;
                // // this.recvMessageOffsets[this.recvMessageCount] =
                // //     this.recvMessageOffsets[this.recvMessageCount-1] + buffer.length;
            // } catch (err) {
            //     alert("JSWebSocketPlugin receive buffer overflow");
            // }
        }
        // Module.myws.onclose = function(event) {
        //     this.state = States.Closed;
        // }
        // Module.myws.onerror = function(event) {
        //     if (state === States.Connected) {
        //         Module.myws.close();
        //         this.state = State.Closing;
        //     }
        // }
        // Module.myws.onopen = function(event) {
        //     this.state = States.Opened;
        // }
        return true;
    },

    JsWebSocketPlugin_IsConnecting: function() {
        return Module.myws !== undefined && Module.myws.readyState === 0;
    },

    JsWebSocketPlugin_IsOpen: function() {
        return Module.myws !== undefined && Module.myws.readyState === 1;
    },

    JsWebSocketPlugin_IsClosing: function() {
        return Module.myws !== undefined && Module.myws.readyState === 2;
    },

    JsWebSocketPlugin_IsClosed: function() {
        return Module.myws !== undefined && Module.myws.readyState === 3;
    },

//    JsWebSocketPlugin_Send: function(bytes, start, length) {
//        if (Module.myws === undefined || Module.myws.readyState >= 2) {
//            // Closing or closed
//            return -1;
//        } else if (Module.myws.readyState == 0)
//        {
//            // Connecting
//            return 0;
//        }

//        Module.myws.send(new Uint8Array(Module.HEAPU8.buffer,bytes+start,length));
//        return 1;
//    },

    JsWebSocketPlugin_Send: function(bytes, start, length) {
        if (Module.myws === undefined || Module.myws.readyState >= 2) {
            // Closing or closed
            return -1;
        } else if (Module.myws.readyState == 0)
        {
            // Connecting
            return 0;
        }

        Module.myws.send(new Uint8Array(Module.HEAPU8.buffer,bytes+start,length));
        return 1;
    },

    JsWebSocketPlugin_Receive: function(bytes, offset, length) {
        if (Module.myws.messages.length > 0) {
            //var data = new Uint8Array(Module.HEAPU8.buffer, Module.myws.messages[0], Module.myws.messages[0].length); // (buffer is heap)
            //for (var i = 0; i < Module.myws.messages[0].length; i++) {
            //    HEAPU8[(bytes >> 2) + i + offset] = Module.myws.messages[0][i];
            //}
            //var result = new Uint8Array(Module.HEAPU8.buffer,bytes+offset,Module.myws.messages[0].length);
            var m = Module.myws.messages[0];
            if (length > m.length) {
                length = m.length;
            }

            var outArr = new Uint8Array(Module.HEAPU8.buffer,bytes+offset,length);

            if (length < m.length) {
                outArr.set(Module.myws.messages[0].slice(0,length));
                Module.myws.messages[0] = Module.myws.messages[0].slice(length);
            } else {
                outArr.set(Module.myws.messages[0]);
                Module.myws.messages.splice(0,1);
            }

            return length;
        }

        if (Module.myws === undefined || Module.myws.readyState >= 2) {
            // Closing or closed
            return -1;
        }

        return 0;
    },

    JsWebSocketPlugin_Close: function() {
        //if (this.state !== State.Connected) {
        //    return;
        //}

        Module.myws.close();
        //state = States.Closing;
    }
};

//JsWebSocketPlugin.JsWebSocketPlugin_TryConnect("ws://nexus.cs.ucl.ac.uk:8006");
mergeInto(LibraryManager.library, JsWebSocketPlugin);

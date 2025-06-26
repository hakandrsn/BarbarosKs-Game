var WebSocketLibrary = {
    $webSocketInstances: {},
    $nextInstanceId: 1,

    // WebSocket nesnesi oluştur
    CreateInstance: function(urlPtr) {
        var url = UTF8ToString(urlPtr);
        var id = nextInstanceId++;

        console.log("[WebSocket] Creating instance #" + id + " for " + url);

        var ws = new WebSocket(url);
        webSocketInstances[id] = {
            socket: ws,
            onOpen: null,
            onMessage: null,
            onError: null,
            onClose: null,
            buffer: new Uint8Array(0),
            bufferSize: 0,
            error: ""
        };

        return id;
    },

    // Callback fonksiyonlarını ayarla
    SetCallbacks: function(instanceId, onOpenPtr, onMessagePtr, onErrorPtr, onClosePtr) {
        var instance = webSocketInstances[instanceId];
        if (!instance) return;

        instance.onOpen = onOpenPtr;
        instance.onMessage = onMessagePtr;
        instance.onError = onErrorPtr;
        instance.onClose = onClosePtr;

        instance.socket.onopen = function() {
            if (instance.onOpen) {
                dynCall_v(instance.onOpen);
            }
        };

        instance.socket.onmessage = function(e) {
            if (instance.onMessage) {
                var dataStr = e.data;
                var bufferSize = lengthBytesUTF8(dataStr) + 1;
                var buffer = _malloc(bufferSize);
                stringToUTF8(dataStr, buffer, bufferSize);
                dynCall_vi(instance.onMessage, buffer);
                _free(buffer);
            }
        };

        instance.socket.onerror = function(e) {
            if (instance.onError) {
                var errorStr = "WebSocket Error";
                var bufferSize = lengthBytesUTF8(errorStr) + 1;
                var buffer = _malloc(bufferSize);
                stringToUTF8(errorStr, buffer, bufferSize);
                dynCall_vi(instance.onError, buffer);
                _free(buffer);
            }
        };

        instance.socket.onclose = function(e) {
            if (instance.onClose) {
                var reason = e.reason || "Connection closed";
                var bufferSize = lengthBytesUTF8(reason) + 1;
                var buffer = _malloc(bufferSize);
                stringToUTF8(reason, buffer, bufferSize);
                dynCall_vi(instance.onClose, buffer);
                _free(buffer);
            }
        };
    },

    // Bağlantıyı başlat
    Connect: function(instanceId) {
        console.log("[WebSocket] Connecting instance #" + instanceId);
        // WebSocket bağlantısı zaten onopen olayını tetikleyecek şekilde ayarlandı
        // Bu nedenle bir şey yapmamıza gerek yok
    },

    // Mesaj gönder
    Send: function(instanceId, messagePtr) {
        var instance = webSocketInstances[instanceId];
        if (!instance) return;

        var message = UTF8ToString(messagePtr);
        instance.socket.send(message);
    },

    // Bağlantıyı kapat
    Close: function(instanceId) {
        var instance = webSocketInstances[instanceId];
        if (!instance) return;

        instance.socket.close();
        delete webSocketInstances[instanceId];
    }
};

autoAddDeps(WebSocketLibrary, '$webSocketInstances');
autoAddDeps(WebSocketLibrary, '$nextInstanceId');

mergeInto(LibraryManager.library, WebSocketLibrary);

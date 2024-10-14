// firebase-messaging-sw.js

// Firebase'i yükleyin
importScripts('https://www.gstatic.com/firebasejs/10.14.1/firebase-app-compat.js');
importScripts('https://www.gstatic.com/firebasejs/10.14.1/firebase-messaging-compat.js');

// Firebase yapılandırma
firebase.initializeApp({
    apiKey: "AIzaSyA_QoXR5jBgGN_5BiVF0J-LUVo9NO7p-84",
    authDomain: "bnetclone.firebaseapp.com",
    projectId: "bnetclone",
    storageBucket: "bnetclone.appspot.com",
    messagingSenderId: "337788997101",
    appId: "1:337788997101:web:4c00e9e662fa5383cf6be4"
});

const messaging = firebase.messaging();

// Arka planda gelen mesajları ele al
messaging.onBackgroundMessage(function(payload) {
    console.log('[firebase-messaging-sw.js] Gelen arka plan mesajı:', payload);
    const notificationTitle = payload.data.name;
    const notificationOptions = {
        body: payload.data.body,
        icon: payload.data.image, // İsteğe bağlı ikon
        data: {
            url: payload.data.url || '/', // Varsayılan bir URL ekleyin
            senderId: payload.data.senderId // Ek veri
        }
    };

    self.registration.showNotification(notificationTitle, notificationOptions);
});

// Bildirim tıklama olayını yönetme
self.addEventListener('notificationclick', function(event) {
    
    event.notification.close(); // Bildirimi kapat

    const targetUrl = event.notification.data.url; // Bildirimdeki URL
    const additionalData = event.notification.data.senderId; // Ek veri

    event.notification.close(); // Bildirimi kapat
    event.waitUntil(
        clients.matchAll({ type: 'window', includeUncontrolled: true }).then(function(clientList) {
            for (const client of clientList) {
                if (client.url === targetUrl && 'focus' in client) {
                    client.postMessage({ action: 'handleNotificationClick', data: additionalData });
                    return client.focus();
                }
            }
            if (clients.openWindow) {
            
                return clients.openWindow(targetUrl);
            }
        })
    );
});
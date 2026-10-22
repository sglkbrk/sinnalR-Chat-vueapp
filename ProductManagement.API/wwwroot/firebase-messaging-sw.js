// firebase-messaging-sw.js

importScripts('https://www.gstatic.com/firebasejs/10.14.1/firebase-app-compat.js');
importScripts('https://www.gstatic.com/firebasejs/10.14.1/firebase-messaging-compat.js');

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
messaging.onBackgroundMessage(function (payload) {
    console.log('[firebase-messaging-sw.js] Gelen arka plan mesajı:', payload);

    const data = payload.data || {};
    const notification = payload.notification || {};

    const senderName = data.name || notification.title || 'Yeni Mesaj';
    let iconUrl = notification.icon || data.image || '';

    // Eski randomuser.me mock resimleri veya boş resim kontrolü
    if (!iconUrl || iconUrl.includes('randomuser.me')) {
        iconUrl = `https://ui-avatars.com/api/?name=${encodeURIComponent(senderName)}&background=4f46e5&color=fff&rounded=true&size=192`;
    } else if (iconUrl.startsWith('/')) {
        // Relative path ise origin ile tam URL oluştur
        iconUrl = self.location.origin + iconUrl;
    }

    const notificationTitle = senderName;
    const notificationOptions = {
        body: notification.body || data.body || 'Bir yeni mesajınız var.',
        icon: iconUrl,
        badge: '/icons/icon-192x192.png',
        tag: notification.tag || `chat-${data.senderId || 'general'}`,
        renotify: true,
        data: {
            url: data.url || '/',
            senderId: data.senderId || null
        }
    };

    return self.registration.showNotification(notificationTitle, notificationOptions);
});

// Bildirime tıklanma olayı
self.addEventListener('notificationclick', function (event) {
    event.notification.close();

    const notifData = event.notification.data || {};
    const targetUrl = notifData.url || '/';
    const senderId = notifData.senderId;

    event.waitUntil(
        clients.matchAll({ type: 'window', includeUncontrolled: true }).then(function (clientList) {
            for (const client of clientList) {
                // Eğer uygulama sekmesi açıksa odaklan ve mesaj gönder
                if ('focus' in client) {
                    if (senderId) {
                        client.postMessage({ action: 'handleNotificationClick', data: senderId });
                    }
                    return client.focus();
                }
            }
            if (clients.openWindow) {
                return clients.openWindow(targetUrl);
            }
        })
    );
});

// PWA Service Worker yaşam döngüsü
self.addEventListener('install', function (event) {
    self.skipWaiting();
});

self.addEventListener('activate', function (event) {
    event.waitUntil(clients.claim());
});


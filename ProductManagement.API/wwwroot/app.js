// app.js - Firebase Cloud Messaging (WebPush) İstemcisi

const FIREBASE_CONFIG = {
    apiKey: "AIzaSyA_QoXR5jBgGN_5BiVF0J-LUVo9NO7p-84",
    authDomain: "bnetclone.firebaseapp.com",
    projectId: "bnetclone",
    storageBucket: "bnetclone.appspot.com",
    messagingSenderId: "337788997101",
    appId: "1:337788997101:web:4c00e9e662fa5383cf6be4"
};

const VAPID_KEY = "BK3CENDgnEDY-QgFfRF_Mn2xbZ77yk2LzhwWktpmqufCUYe8yNBP-aEIKWBegnFqOzNZzbwqZM8gH3Cy8K0eUF4";

let fbApp = null;
let fbMessaging = null;

function getMessagingInstance() {
    if (!fbMessaging) {
        if (!firebase.apps.length) {
            fbApp = firebase.initializeApp(FIREBASE_CONFIG);
        } else {
            fbApp = firebase.app();
        }
        fbMessaging = firebase.messaging(fbApp);
    }
    return fbMessaging;
}

// Backend'e token gönderme fonksiyonu
async function sendFbTokenToBackend(fcmToken) {
    const authToken = localStorage.getItem('token');
    const userId = localStorage.getItem('userId');

    if (!authToken || !userId || !fcmToken) {
        return;
    }

    try {
        const response = await fetch('/api/FbToken', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${authToken}`
            },
            body: JSON.stringify({ Token: fcmToken, UserId: parseInt(userId) })
        });
        if (response.ok) {
            console.log('[FCM] Token başarıyla backend\'e kaydedildi.');
        } else {
            console.warn('[FCM] Token backend\'e kaydedilemedi. HTTP:', response.status);
        }
    } catch (err) {
        console.error('[FCM] Token gönderim hatası:', err);
    }
}

// Ana başlatıcı fonksiyon
async function initPushNotifications() {
    if (!('serviceWorker' in navigator) || !('PushManager' in window)) {
        console.log('[FCM] Bu tarayıcı Push bildirimlerini desteklemiyor.');
        return;
    }

    try {
        const registration = await navigator.serviceWorker.register('/firebase-messaging-sw.js');
        console.log('[FCM] Service Worker kayıt başarılı:', registration);

        const messaging = getMessagingInstance();

        // Bildirim izni durumu
        let permission = Notification.permission;
        if (permission === 'default') {
            permission = await Notification.requestPermission();
        }

        if (permission === 'granted') {
            console.log('[FCM] Bildirim izni onaylı.');
            const currentToken = await messaging.getToken({
                vapidKey: VAPID_KEY,
                serviceWorkerRegistration: registration
            });

            if (currentToken) {
                console.log('[FCM] Token alındı:', currentToken);
                await sendFbTokenToBackend(currentToken);
            } else {
                console.warn('[FCM] Cihaz token\'ı üretilemedi.');
            }
        } else {
            console.log('[FCM] Bildirim izni reddedildi veya varsayılanda bırakıldı:', permission);
        }

        // Ön planda gelen bildirimleri dinle
        messaging.onMessage((payload) => {
            console.log('[FCM] Ön planda gelen mesaj:', payload);
            const title = payload.notification?.title || payload.data?.name || 'Yeni Mesaj';
            const body = payload.notification?.body || payload.data?.body || '';
            let icon = payload.notification?.icon || payload.data?.image || '';

            if (!icon || icon.includes('randomuser.me')) {
                icon = `https://ui-avatars.com/api/?name=${encodeURIComponent(title)}&background=4f46e5&color=fff&rounded=true&size=192`;
            } else if (icon.startsWith('/')) {
                icon = window.location.origin + icon;
            }

            // Sayfa gizliyse veya sekme odakta değilse sistem bildirimi göster
            if (document.hidden && Notification.permission === 'granted') {
                new Notification(title, {
                    body: body,
                    icon: icon,
                    data: payload.data
                });
            }
        });

    } catch (err) {
        console.error('[FCM] Push başlatma sırasında hata oluştu:', err);
    }
}

// Service worker mesajlarını dinle (tıklama vb.)
if ('serviceWorker' in navigator) {
    navigator.serviceWorker.addEventListener('message', function (event) {
        if (event.data && event.data.action === 'handleNotificationClick') {
            console.log('[FCM SW Message] Bildirim tıklama verisi:', event.data.data);
            const senderId = event.data.data;
            if (window.vueAppInstance && typeof window.vueAppInstance.selectUserById === 'function') {
                window.vueAppInstance.selectUserById(senderId);
            }
        }
    });
}

// Geriye dönük uyumluluk için global fonksiyon adları
window.initPushNotifications = initPushNotifications;
window.sendTokenToServer = initPushNotifications;

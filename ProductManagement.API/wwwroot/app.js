// app.js

// Firebase SDK'sını içe aktarın


function sendTokenToServer(currentToken) {
    const firebaseConfig = {
        apiKey: "AIzaSyA_QoXR5jBgGN_5BiVF0J-LUVo9NO7p-84",
        authDomain: "bnetclone.firebaseapp.com",
        projectId: "bnetclone",
        storageBucket: "bnetclone.appspot.com",
        messagingSenderId: "337788997101",
        appId: "1:337788997101:web:4c00e9e662fa5383cf6be4"
    };
    
    // Firebase'i başlat
    const app = firebase.initializeApp(firebaseConfig);
    const messaging = firebase.messaging(app);
    
    // Service Worker'ı kayıt et
    if ('serviceWorker' in navigator) {
        navigator.serviceWorker.register('/firebase-messaging-sw.js')
        .then((registration) => {
            console.log('Service Worker kayıt edildi:', registration);
            // FCM token alırken Service Worker'ı kullanmak için
            // messaging.useServiceWorker(registration);
        }).catch((err) => {
            console.error('Service Worker kaydedilemedi:', err);
        });
    }
    
    // Bildirim izni isteyin
    Notification.requestPermission().then((permission) => {
        if (permission === 'granted') {
            console.log('Bildirim izni verildi.');
            // Token al
            messaging.getToken(messaging, { vapidKey: 'BK3CENDgnEDY-QgFfRF_Mn2xbZ77yk2LzhwWktpmqufCUYe8yNBP-aEIKWBegnFqOzNZzbwqZM8gH3Cy8K0eUF4' })
            .then((currentToken) => {
                if (currentToken) {
                    console.log('FCM Token:', currentToken);
                    // Token'ı backend'e gönderin
                    sendTokenToServer(currentToken);
                } else {
                    console.log('Token alınamadı.');
                }
            }).catch((err) => {
                console.error('Token alınırken hata oluştu:', err);
            });
        } else {
            console.log('Bildirim izni reddedildi.');
        }
    });
    
    // Önyüzde gelen mesajları dinleyin
    messaging.onMessage(messaging, (payload) => {
        // console.log('Önyüzde gelen mesaj:', payload);
        // // Bildirimi göster
        // const notificationTitle = payload.notification.title;
        // const notificationOptions = {
        //     body: payload.notification.body,
        //     icon: '/firebase-logo.png' // İsteğe bağlı ikon
        // };
    
        // new Notification(notificationTitle, notificationOptions);
    });
    
    // Token'ı backend'e gönderme fonksiyonu
    function sendTokenToServer(fbtoken) {
        let token = localStorage.getItem('token');
        let userId = localStorage.getItem('userId');
        fetch('/api/FbToken', { // Backend API endpoint'iniz
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${token}`,
            },
            body: JSON.stringify({ Token: fbtoken,UserId: userId})
        })
        .then(response => response.json())
        .then(data => {
            console.log('Token sunucuya gönderildi:', data);
        })
        .catch((error) => {
            console.error('Token sunucuya gönderilemedi:', error);
        });
    }
    
    
    navigator.serviceWorker.addEventListener('message', function(event) {
        if (event.data.action === 'handleNotificationClick') {
            const data = event.data.data;
            // Burada sayfanız açıkken yapılacak işlemleri gerçekleştirin
            console.log('Notification Click Action:', data);
            // Örneğin, belirli bir fonksiyonu çağırabilirsiniz
            handleNotificationClick(data);
        }
    });
    
    // Bildirim tıklama işlemlerini gerçekleştiren fonksiyon
    function handleNotificationClick(data) {
        // Burada sayfanızda yapılacak işlemleri tanımlayın
        // Örneğin, belirli bir bölüme kaydırma, veriyi güncelleme vb.
        console.log('Handling notification click with data:', data);
        // Örneğin, bir modal açmak veya sayfayı belirli bir yere kaydırmak
    }
}
// Firebase yapılandırma

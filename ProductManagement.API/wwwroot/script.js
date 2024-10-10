const toggleButton = document.querySelector('.dark-light');
const colors = document.querySelectorAll('.color');
let token = '';
userId = 0;
let connection = null;
let reconnectAttempts = 0; // Yeniden bağlanma denemeleri sayacı
const maxReconnectAttempts = 10; // Maksimum yeniden bağlanma denemesi
const reconnectInterval = 5000; 
let writeTime=0
var onlineUsers_data = [];
var users = [];
let page = 1;
const pageSize = 50;
const msgDiv = document.getElementById('messages');
const chat = document.getElementById('chat');
let stop = false;
selectedUserId = 0;
let isScrollingEnabled = true; 

colors.forEach(color => {
  color.addEventListener('click', e => {
    colors.forEach(c => c.classList.remove('selected'));
    const theme = color.getAttribute('data-color');
    document.body.setAttribute('data-theme', theme);
    color.classList.add('selected');
  });
});

toggleButton.addEventListener('click', () => {
  document.body.classList.toggle('dark-mode');
});




if ("Notification" in window) {
    if (Notification.permission === "granted") {
        // İzin zaten verilmiş
    } else if (Notification.permission !== "denied") {
        Notification.requestPermission().then(permission => {
            if (permission === "granted") {
                console.log("Bildirim izni verildi.");
            }
        });
    }
}
document.getElementById("message").addEventListener("keydown", function(event) {
    if (event.key === "Enter") {
        sendMessage(); // Enter tuşuna basıldığında mesajı gönder
        event.preventDefault(); // Formun submit edilmesini engelle
    }
});

onload = () => {
    token = localStorage.getItem('token');
    if (token) {
        var asd = parseJwt(token);
        userId = asd.sub;
        document.getElementById('userProfile').src =  `https://randomuser.me/api/portraits/men/${asd.sub}.jpg`;
        document.getElementById('myusername').innerHTML=asd.name.toUpperCase();
        document.getElementById('login').style.display = 'none';
        document.getElementById('register').style.display = 'none';
        document.getElementById('app').style.display = '';
        startConnection();
        loadUsers();
    }else{
        document.getElementById('username').value = localStorage.getItem('username');
        document.getElementById('password').value = localStorage.getItem('password');
        document.getElementById('rememberMe').checked = localStorage.getItem('rememberMe');
        document.getElementById('app').style.display = 'none';
    }
}
function logout(){
    localStorage.removeItem('token');
    localStorage.removeItem('username');
    localStorage.removeItem('password');
    localStorage.removeItem('rememberMe');
    document.getElementById('logout').style.display = 'none';
    location.reload();  
}
async function login() {
    const username = document.getElementById('username').value;
    const password = document.getElementById('password').value;
    const rememberMe = document.getElementById('rememberMe').checked;
    
    const response = await fetch('/api/UserAuth/login', {
        method: 'POST',
        headers: {'Content-Type': 'application/json'},
        body: JSON.stringify({ username, password })
    });
    if (response.ok) {
        const data = await response.json();
        token = data.token;
        localStorage.setItem('token', token);
        var asd = parseJwt(token);
        userId = asd.sub;
        document.getElementById('userProfile').src =  `https://randomuser.me/api/portraits/men/${asd.sub}.jpg`;
        document.getElementById('myusername').innerHTML=asd.name.toUpperCase();
        document.getElementById('login').style.display = 'none';
        document.getElementById('register').style.display = 'none';
        document.getElementById('app').style.display = '';
        document.getElementById('register-message').textContent = '';
        if(rememberMe){
            localStorage.setItem('username', username);
            localStorage.setItem('password', password);
            localStorage.setItem('rememberMe', rememberMe);
        }
        startConnection();
        loadUsers();
    } else {
        const error = await response.json();
        alert('Login failed: ' + (error.message || 'Unknown error'));
    }
}
async function register() {
    const regUsername = document.getElementById('reg-username').value;
    const regEmail = document.getElementById('reg-email').value;
    const regPassword = document.getElementById('reg-password').value;
    if (regUsername === '' || regEmail === '' || regPassword === '') {
        alert('Lütfen bilgileri eksiksiz doldurun');
        return;
    }
    const response = await fetch('/api/UserAuth/register', {
        method: 'POST',
        headers: {'Content-Type': 'application/json'},
        body: JSON.stringify({ Username: regUsername, Email: regEmail, Password: regPassword })
    });
    if (response.ok) {
        document.getElementById('register-message').textContent = "Kayıt başarılı! Lütfen giriş yapın.";
        setTimeout(() => {
            document.getElementById('register-message').textContent = '';
        },5000)
        document.getElementById('reg-username').value = '';
        document.getElementById('reg-email').value = '';
        document.getElementById('reg-password').value = '';
        document.getElementById('register').style.display = 'none'; 
    } else {
        const error = await response.json();
        document.getElementById('register-message').textContent = 'Kayıt başarısız: ' + (error.errors || 'Lüfen Bilgileri doğrı girin ');
    }
}

function parseJwt(token) {
    const base64Url = token.split('.')[1]; // Payload kısmı
    const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/'); // Base64 URL kodlamasını düzelt
    const jsonPayload = decodeURIComponent(escape(atob(base64))); // Base64'ten JSON'a dönüştür
    return JSON.parse(jsonPayload); // JSON'ı objeye dönüştür
}

function startConnection() {
    // document.getElementById('logout').style.display = 'flex';
    connection = new signalR.HubConnectionBuilder()
        .withUrl('/chathub', { accessTokenFactory: () => token })
        .build();

    connection.on('ReceiveMessage', (message) => {
        if (message.senderId == selectedUserId) {
            msgDiv.innerHTML += 
            `<div class="chat-msg"> 
              <div class="chat-msg-profile">
                <img class="chat-msg-img" src="https://randomuser.me/api/portraits/men/${message.senderId}.jpg" alt="" />
                <div class="chat-msg-date">Message Time ${new Date(message.timestamp).toLocaleTimeString()}</div>
              </div>
              <div class="chat-msg-content">
                <div class="chat-msg-text">${message.content.content}</div>
              </div>
            </div>`;
            chat.scrollTop = chat.scrollHeight; // Otomatik olarak en son mesaja kaydır
        }
        if(!isPageVisible()){
                showNotification(message.content.content,message.senderId,message.name);
        }
    });

    connection.on('ReceiveWrites', (senderId) => {
        if (senderId == selectedUserId) {
            document.getElementById('ReceiveWrites').style.display = 'flex';
            setTimeout(() => {
                document.getElementById('ReceiveWrites').style.display = 'none';
            },3000)
        }
    });
    
    connection.on('UserStatusUpdated', (onlineUsers) => {
        onlineUsers_data = onlineUsers;
        const usersSelect = document.getElementById('users');
        for (let i = 0; i < usersSelect.children.length; i++) {
          if(onlineUsers_data.includes(users[i].id.toString()))usersSelect.children[i].classList.add("online");
          else usersSelect.children[i].classList.remove("online");
        }

    });
    connection.onclose(() => {
        updateConnectionStatus('Bağlantı kesildi. Yeniden bağlanma denemeleri başlatılıyor...');
        attemptReconnect();
    });

    connection.onreconnecting(() => {
        updateConnectionStatus('Bağlantı yeniden bağlanıyor...');
    });

    connection.onreconnected(() => {
        updateConnectionStatus('Bağlantı başarıyla yeniden kuruldu.');
    });
    connection.start()
        .then(() => {
            updateConnectionStatus('Bağlantı başarıyla kuruldu.',3000);
            console.log('SignalR connected')
        })
        .catch(err => console.error('SignalR connection error: ', err));
}
function showNotification(message,senderId,name) {
    if ("Notification" in window && Notification.permission === "granted") {
        const notification = new Notification("Yeni Mesaj", {
            body: `Gönderen: ${name}\nMesaj: ${message}`,
            icon: '/path/to/icon.png', // İsteğe bağlı
            data: {  senderId: senderId } 
        });

        notification.onclick = (event) => {
            window.focus();
            selectUser(senderId,name);
            notification.close();
        };
    }
}
function isPageVisible() {
    return !document.hidden;
}
async function attemptReconnect() {
    while (reconnectAttempts < maxReconnectAttempts) {
        await new Promise(resolve => setTimeout(resolve, reconnectInterval)); // Belirli bir süre bekle

        reconnectAttempts++;
        console.log(`Yeniden bağlanma denemesi ${reconnectAttempts}...`);

        try {
            await connection.start(); // Yeniden bağlanmayı dene
            console.log('Bağlantı başarıyla yeniden kuruldu.');
            updateConnectionStatus('Bağlantı başarıyla yeniden kuruldu.');
            reconnectAttempts = 0; // Başarılı olduğunda deneme sayacını sıfırla
            return; // Yeniden bağlantı başarılıysa döngüden çık
        } catch (err) {
            console.error('Yeniden bağlanma hatası: ', err);
        }
    }
    updateConnectionStatus('Maksimum yeniden bağlanma denemesi aşıldı. Lütfen internet bağlantınızı kontrol edin.',10000);
}
function updateConnectionStatus(status,time) {
    const statusDiv = document.getElementById('connectionStatus');
    statusDiv.textContent = status;
    setTimeout(() => statusDiv.textContent = '', time ? time: 5000);
}
async function loadUsers() {
    const response = await fetch('/api/UserAuth/users', {
        method: 'GET',
        headers: {
            'Authorization': `Bearer ${token}`,
            'Content-Type': 'application/json'
        }
    });
    if (response.ok) {
        users = await response.json();
        const usersSelect = document.getElementById('users');
        usersSelect.innerHTML = ''; // Önceki seçenekleri temizle
        let asd = ""
        users.forEach(user => {
            asd += 
          ` <div id="${user.id}-user" onclick="selectUser('${user.id}','${user.username}')" class="msg ${onlineUsers_data.includes(user.id.toString()) ? 'online' : ''}">
            <img class="msg-profile" src="https://randomuser.me/api/portraits/men/${user.id}.jpg"  alt="" />
            <div class="msg-detail">
            <div class="msg-username">${user.username}</div>
            <div class="msg-content">
              <span class="msg-message">Son Konuşma</span>
              <span class="msg-date">20m</span>
            </div>
            </div>
          </div>`;
        });
        usersSelect.innerHTML = asd;
    } else {
        const error = await response.json();
        alert('Failed to load users: ' + (error.message || 'Unknown error'));
    }
}

async function selectUser(receiverId,name) {
    page = 1
    stop = false
    selectedUserId = receiverId
    msgDiv.innerHTML = "";
    let chatAreaTitle = document.getElementById('chat-area-title');
    chatAreaTitle.innerHTML = name
    let usersSelect = document.getElementById('users');
    for (let i = 0; i < usersSelect.children.length; i++) {
        usersSelect.children[i].classList.remove("active")
    }
    chatAreaTitle.innerHTML = name
    document.getElementById(receiverId + '-user').classList.add("active")
    document.getElementById("chat-area-footer").style.display = ""
    isScrollingEnabled = false;
    loadMessages(userId, receiverId);
}

async function loadMessages(senderId, receiverId) {
    if(stop) return
    const response = await fetch('/api/message/GetMyMessages/' + senderId + '/' + receiverId + '/'+ page +'/' + pageSize, {
        method: 'GET',
        headers: {
            'Authorization': `Bearer ${token}`,
            'Content-Type': 'application/json'
        }
    });

    if (response.ok) {
        const messages = await response.json();
       
        let htmlContent = "";
        if(messages.length == 0 && page == 1) {
          htmlContent = `<p style="text-align:center;color:orange">Mesajlar uçtan uca şifresizdir. Bende dahil olmak üzere bu sohbetin dışında bulunan herkes mesajları okuyabilir.</p> `
          stop = true
        } 
        else if(messages.length == 0 && page > 1) {
            htmlContent = `<p style="text-align:center">Daha fazla Mesaj Yok</p>`
            stop = true
        } 
        messages.forEach(msg => {
          htmlContent += 
          `<div class="chat-msg ${msg.senderId == userId ? 'owner' : ''}"> 
            <div class="chat-msg-profile">
              <img class="chat-msg-img" src="https://randomuser.me/api/portraits/men/${msg.senderId}.jpg" alt="" />
              <div class="chat-msg-date">Message Time ${new Date(msg.timestamp).toLocaleTimeString()}</div>
            </div>
            <div class="chat-msg-content">
              <div class="chat-msg-text">${msg.content}</div>
            </div>
          </div>`;
        });
        msgDiv.innerHTML = htmlContent + msgDiv.innerHTML; 
        if(page ==1) chat.scrollTop = chat.scrollHeight; 
        else  chat.scrollTop = chat.scrollHeight - chat.scrollHeight;
        isScrollingEnabled = true;
        page++;
    } else {
        const error = await response.json();
        alert('Failed to load users: ' + (error.message || 'Unknown error'));
    }
}
chat.addEventListener('scroll', () => {
    if (chat.scrollTop === 0 && isScrollingEnabled )  {
        loadMessages(userId, selectedUserId);
    }
});
async function onChangeMsg (params) {
    if(writeTime  == 0 ){
        await connection.invoke('SendWrite', selectedUserId);
        writeTime = 3000
        setTimeout(() => {
            writeTime = 0
        },writeTime)
    }
   
}
async function sendMessage() {
    const receiverId = selectedUserId
    const message = document.getElementById('message').value.trim();

    if (connection && message !== '') {
        try {
            var msg = {
                SenderId: parseInt(userId),
                ReceiverId: parseInt(receiverId),
                Content: message,
                Timestamp: new Date()
            }
            await connection.invoke('SendMessage', msg);
            // const msgDiv = document.getElementById('messages');
            msgDiv.innerHTML += 
            `<div class="chat-msg owner"> 
              <div class="chat-msg-profile">
                <img class="chat-msg-img" src="https://randomuser.me/api/portraits/men/${msg.SenderId}.jpg" alt="" />
                <div class="chat-msg-date">Message Time ${new Date(msg.Timestamp).toLocaleTimeString()}</div>
              </div>
              <div class="chat-msg-content">
                <div class="chat-msg-text">${msg.Content}</div>
              </div>
            </div>`;
            chat.scrollTop = chat.scrollHeight; // Otomatik olarak en son mesaja kaydır
            document.getElementById('message').value = '';
        } catch (err) {
            console.error('SendMessage error: ', err);
        }
    }
}

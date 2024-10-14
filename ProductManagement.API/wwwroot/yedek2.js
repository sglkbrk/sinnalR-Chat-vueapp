const toggleButton = document.querySelector('.dark-light');
const colors = document.querySelectorAll('.color');
let reconnectAttempts = 0; // Yeniden bağlanma denemeleri sayacı
const maxReconnectAttempts = 10; // Maksimum yeniden bağlanma denemesi
const reconnectInterval = 5000; 
let writeTime=0
let page = 1;
const pageSize = 50;
let isScrollingEnabled = true; 

const { createApp } = Vue

createApp({
    setup() {

    },
    mounted() {
        this.token = localStorage.getItem('token');
        if (this.token) {
            this.myuser = parseJwt( this.token)
            this.myuser.picture =  `https://randomuser.me/api/portraits/men/${this.myuser.sub}.jpg`;
            document.getElementById('login').style.display = 'none';
            document.getElementById('register').style.display = 'none';
            document.getElementById('app').style.display = '';
            localStorage.setItem('userId',this.myuser.sub );
            this.startConnection();
            this.loadUsers();
            sendTokenToServer();
            this.loadFriendRequests();
        }else{
            document.getElementById('username').value = localStorage.getItem('username');
            document.getElementById('password').value = localStorage.getItem('password');
            document.getElementById('rememberMe').checked = localStorage.getItem('rememberMe');
            document.getElementById('app').style.display = 'none';
        }
        if ("Notification" in window) {
            if (Notification.permission === "granted") {
            } else if (Notification.permission !== "denied") {
                Notification.requestPermission().then(permission => {
                    if (permission === "granted") {
                        console.log("Bildirim izni verildi.");
                    }
                });
            }
        }
        this.$refs.msgDiv.addEventListener('scroll', () => {
            if (this.$refs.msgDiv.scrollTop === 0 && isScrollingEnabled )  {
                this.loadMessages(this.myuser.sub, this.selectedUserId);
            }
        });
    },
    methods: {
        logout(){
            this.deleteFbToken();
            localStorage.removeItem('token');
            location.reload(); 
        },
        deleteFbToken(){
            fetch('/api/FbToken/'+this.myuser.sub, { // Backend API endpoint'iniz
                method: 'DELETE',
                headers: {
                    'Content-Type': 'application/json',
                    'Authorization': `Bearer ${this.token}`,
                },
            })
            .then(response => response.json())
            .then(data => {
                console.log('Token sunucuya gönderildi:', data);
            })
            .catch((error) => {
                console.error('Token sunucuya gönderilemedi:', error);
            });
        },
        startConnection() {
            this.connection = new signalR.HubConnectionBuilder()
                .withUrl('/chathub', { accessTokenFactory: () =>  this.token })
                .build();
        
            this.connection.on('ReceiveMessage', (message) => {
                if (message.senderId == this.selectedUserId) {
                    var item  =  {
                        "senderId": message.senderId,
                        "receiverId": message.content.receiverId,
                        "content": message.content.content,
                        "timestamp": message.timestamp
                    }
                    this.messages.push(item);
                    setTimeout(() => {
                        this.$refs.msgDiv.scrollTop = this.$refs.msgDiv.scrollHeight;
                    }, );
                }
                if(!this.isPageVisible()){
                        this.showNotification(message.content.content,message.senderId,message.name,"message");
                }
                this.setLastMessage(message.senderId, message.content.content);
            });
        
            this.connection.on('ReceiveWrites', (senderId) => {
                if (senderId == this.selectedUserId) {
                    document.getElementById('ReceiveWrites').style.display = 'flex';
                    setTimeout(() => {
                        document.getElementById('ReceiveWrites').style.display = 'none';
                    },3000)
                }
            });
            this.connection.on('UserStatusUpdated', (onlineUsers) => {
                this.onlineUsers_data = onlineUsers;
        
            });
            this.connection.on('ReceiveNotification', (data) => {
                if(data.type == "refreshFriendRequests") {
                    this.loadFriendRequests();
                    // showNotification("Arkadaş İsteğini Gönderdi",data.id,data.name,"message");
                } 
                else if(data.type == "refreshUsers"){
                    showNotification("Arkadaş İsteğin kabul Etti",data.id,data.name,"message");
                    this.loadUsers();
                }
            });
            this.connection.onclose(() => {
                this.updateConnectionStatus('Bağlantı kesildi. Yeniden bağlanma denemeleri başlatılıyor...');
                this.attemptReconnect();
            });
        
            this.connection.onreconnecting(() => {
                this.updateConnectionStatus('Bağlantı yeniden bağlanıyor...');
            });
        
            this.connection.onreconnected(() => {
                this.updateConnectionStatus('Bağlantı başarıyla yeniden kuruldu.');
            });
            this.connection.start()
                .then(() => {
                    this.updateConnectionStatus('Bağlantı başarıyla kuruldu.',3000);
                    console.log('SignalR connected')
                })
                .catch(err => console.error('SignalR connection error: ', err));
        },
        showNotification(message,senderId,name,type) {
            if ("Notification" in window && Notification.permission === "granted") {
                const notification = new Notification("Yeni Mesaj", {
                    body: `Gönderen: ${name}\nMesaj: ${message}`,
                    icon: `https://randomuser.me/api/portraits/men/${senderId}.jpg`, // İsteğe bağlı
                    data: {  senderId: senderId } 
                });
                
                notification.onclick = (event) => {
                    if(type == "message"){
                        window.focus();
                        this.selectUser(senderId,name);
                        notification.close();
                    }
                };
            }
        },
        isPageVisible() {
            return !document.hidden;
        },
        attemptReconnect() {
            while (reconnectAttempts < maxReconnectAttempts) {
                 new Promise(resolve => setTimeout(resolve, reconnectInterval)); // Belirli bir süre bekle
        
                reconnectAttempts++;
                console.log(`Yeniden bağlanma denemesi ${reconnectAttempts}...`);
        
                try {
                    this.connection.start(); // Yeniden bağlanmayı dene
                    console.log('Bağlantı başarıyla yeniden kuruldu.');
                    this.updateConnectionStatus('Bağlantı başarıyla yeniden kuruldu.');
                    reconnectAttempts = 0; // Başarılı olduğunda deneme sayacını sıfırla
                    return; // Yeniden bağlantı başarılıysa döngüden çık
                } catch (err) {
                    console.error('Yeniden bağlanma hatası: ', err);
                }
            }
            this.updateConnectionStatus('Maksimum yeniden bağlanma denemesi aşıldı. Lütfen internet bağlantınızı kontrol edin.',10000);
        },
        updateConnectionStatus(status,time) {
            const statusDiv = document.getElementById('connectionStatus');
            statusDiv.textContent = status;
            setTimeout(() => statusDiv.textContent = '', time ? time: 5000);
        },
        setLastMessage(id, lastMessage) {
            // this.users.find(user => user.id === id).lastMessage = lastMessage;
        },
        async sendMessage() {
            if (this.connection && this.messageText !== '') {
                try {
                    var msg = {
                        SenderId: parseInt(this.myuser.sub),
                        ReceiverId: parseInt(this.selectedUserId),
                        Content: this.messageText,
                        Timestamp: new Date()
                    }
                    await this.connection.invoke('SendMessage', msg);
                    var item  =  {
                        "senderId": msg.SenderId,
                        "receiverId": msg.ReceiverId,
                        "content": msg.Content,
                        "timestamp": msg.Timestamp
                    }
                    this.messages.push(item);
                    setTimeout(() => {
                        this.$refs.msgDiv.scrollTop = this.$refs.msgDiv.scrollHeight; 
                    });
                    this.messageText = '';
                    this.setLastMessage(this.selectedUserId, msg.Content);
                } catch (err) {
                    console.error('SendMessage error: ', err);
                }
            }
        },
        toggleConversationArea() {
            this.$refs.conversationArea.classList.toggle('open');
        },
        sendFriendRequestBtn() {
            Swal.fire({
                title: 'Arkadaş İsteği Gönder',
                html:
                    `<input type="text" id="userName" class="swal2-input" placeholder="Kullanıcı Adı">
                     <input type="text" id="userId" class="swal2-input" placeholder="Kullanıcı ID">`,
                showCancelButton: true,
                confirmButtonText: 'Arkadaş İsteği Gönder',
                cancelButtonText: 'İptal',
                preConfirm: () => {
                    const userName = Swal.getPopup().querySelector('#userName').value;
                    const userId = Swal.getPopup().querySelector('#userId').value;
        
                    if (!userName || !userId) {
                        Swal.showValidationMessage(`Lütfen hem kullanıcı adını hem de ID'yi girin`);
                        return false;
                    }
                    
                    return { userName: userName, userId: userId };
                }
            }).then((result) => {
                if (result.isConfirmed) {
                    this.AddFriendRequest(result.value.userName, result.value.userId);
                }
            });
        },
        AddFriendRequest(userName, ReceiverId) {
            if(ReceiverId == this.myuser.sub){
                Swal.fire(
                    'Hata!',
                    'Kendinize arkadaş isteği gönderemezsiniz.',
                    'error'
                );
                return
            }
            fetch('/api/FriendRequest', { // Backend API endpoint'iniz
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'Authorization': `Bearer ${this.token}`,
                },
                body: JSON.stringify({ 
                    SenderId: parseInt(this.myuser.sub), // userId,
                    ReceiverId: parseInt(ReceiverId), // ReceiverId,
                    Status:0
                })
            })
            .then(response => response.json())
            .then(data => {
                if(data && data.id){
                    Swal.fire(
                        'Gönderildi!',
                        `${userName} (ID: ${ReceiverId}) kişisine arkadaş isteği gönderildi.`,
                        'success'
                    );
                }else{
                    Swal.fire(
                        'Hata!',
                        data.message ? data.message : 'Kullanıcı bulunamadı.',
                        'error'
                    );
                }
                
            })
            .catch((error) => {
                console.error('Arkadaş isteği gönderilemedi:', error);
            });
        },
        openFriendRequests() {
            let requestsHtml = '';
            this.friendRequests.forEach(request => {
                requestsHtml += `
                     <div class="request-item">
                        <p><i class="fas fa-user icon"></i> <strong>${request.sender.username}</strong> (ID: ${request.senderId})</p>
                        <div>
                            <button class="accept-btn" data-id="${request.id}" style="background-color: #28a745; margin-right: 5px;">
                                <i class="fas fa-check"></i> Onayla
                            </button>
                            <button class="reject-btn" data-id="${request.id}" style="background-color: #dc3545;">
                                <i class="fas fa-times"></i> Reddet
                            </button>
                        </div>
                    </div>
                `;
            });
        
            Swal.fire({
                title: 'Gelen Arkadaşlık İstekleri',
                html: requestsHtml,
                showCloseButton: true,
                showConfirmButton: false, // Onayla/Reddet butonlarını özel olarak ekliyoruz
                didOpen: () => {
                    document.querySelectorAll('.accept-btn').forEach(button => {
                        button.addEventListener('click', (event) => {
                            const requestId = event.target.getAttribute('data-id');
                            this.handleRequestAction(requestId, '1');
                        });
                    });
        
                    document.querySelectorAll('.reject-btn').forEach(button => {
                        button.addEventListener('click', (event) => {
                            const requestId = event.target.getAttribute('data-id');
                            this.handleRequestAction(requestId, '2');
                        });
                    });
                }
            });
        },
        handleRequestAction(requestId, action) {
            const request = this.friendRequests.find(req => req.id == requestId);
            fetch('/api/FriendRequest/AcceptAndRejectFriendRequest/' + request.id + "/" + action, { 
                method: 'PUT',
                headers: {
                    'Content-Type': 'application/json',
                    'Authorization': `Bearer ${this.token}`,
                },
            })
            .then(data => {
                if(data.ok){
                    if (action === '1') {
                    Swal.fire(
                        'Onaylandı!',
                        `${request.sender.username} (ID: ${request.senderId}) arkadaş olarak eklendi.`,
                        'success'
                    );
                    } else if (action === '2') {
                        Swal.fire(
                            'Reddedildi!',
                            `${request.sender.username} (ID: ${request.senderId}) arkadaşlık isteği reddedildi.`,
                            'error'
                        );
                    }
                    this.loadUsers();
                    this.loadFriendRequests();
                }        
            })
            .catch((error) => {
                console.error('Arkadaş isteği gönderilemedi:', error);
            });
        },
        openSettings() {
            var settingsBox = document.getElementById('settingsBox');
            settingsBox.style.display = settingsBox.style.display == 'none' ? 'block' : 'none';
        },
        selectUser(receiverId,name) {
            page = 1
            this.endMessageVariable = false
            this.messages = []
            this.selectedUserId = receiverId
            let chatAreaTitle = document.getElementById('chat-area-title');
            chatAreaTitle.innerHTML = name
            isScrollingEnabled = false;
            this.loadMessages(this.myuser.sub, receiverId);
            this.$refs.conversationArea.classList.remove('open');
        },
        async  loadUsers() {
            const response = await fetch('/api/Friend/GetChatFriends', {
                method: 'GET',
                headers: {
                    'Authorization': `Bearer ${this.token}`,
                    'Content-Type': 'application/json'
                }
            });
            if (response.ok) {
                users = await response.json();
                users.sort((a, b) => {
                    if (!a.lastMessage) return 1;
                    if (!b.lastMessage) return -1;
                    return new Date(b.lastMessage.timestamp) - new Date(a.lastMessage.timestamp);
                  });

                this.users  = users;
            } else {
                if(response.status == 401) {
                    logout();
                }
                const error = await response.json();
                alert('Failed to load users: ' + (error.message || 'Unknown error'));
            }
        },
        async  loadMessages(senderId, receiverId) {
            if(this.endMessageVariable) return
            const response = await fetch('/api/message/GetMyMessages/' + senderId + '/' + receiverId + '/'+ page +'/' + pageSize, {
                method: 'GET',
                headers: {
                    'Authorization': `Bearer ${this.token}`,
                    'Content-Type': 'application/json'
                }
            });
            if (response.ok) {
                const msgDiv2 = this.$refs.msgDiv;
                var sy = msgDiv2.scrollHeight;
                var messages = await response.json();
                if(messages.length == 0 && page > 1) {
                    this.endMessageVariable = true
                } 
                this.messages  =  messages.concat(this.messages) 
                setTimeout(() => {
                    if(page ==1) msgDiv2.scrollTop = msgDiv2.scrollHeight; 
                    else  msgDiv2.scrollTop = msgDiv2.scrollHeight - sy;
                    isScrollingEnabled = true;
                    page++;
                }, 100);
                
               
            } else {
                const error = await response.json();
                alert('Failed to load users: ' + (error.message || 'Unknown error'));
            }
        },
        async  onChangeMsg () {
            if(writeTime  == 0 ){
                await this.connection.invoke('SendWrite', this.selectedUserId.toString());
                writeTime = 3000
                setTimeout(() => {
                    writeTime = 0
                },writeTime)
            }
           
        },
        async  loadFriendRequests() {
            const response = await fetch('/api/FriendRequest/GetFriendRequestByReceiverId/' + this.myuser.sub, {
                method: 'GET',
                headers: {
                    'Authorization': `Bearer ${this.token}`,
                    'Content-Type': 'application/json'
                }
            });
            if (response.ok) {
                this.friendRequests = await response.json();
                if(this.friendRequests.length) {
                    var notification = document.getElementById('notifications');
                    var notificationCount = document.getElementById('notificationCount');
                    notification.style.display = 'inline-block';
                    notificationCount.innerHTML = this.friendRequests.length;
                }else{
                    var notification = document.getElementById('notifications');
                    notification.style.display = 'none';
                }
                
            } else {
                if(response.status == 401) {
                    logout();
                }
                const error = await response.json();
                alert('Failed to load users: ' + (error.message || 'Unknown error'));
            }
        },
        timeAgo(timestamp) {
            const now = new Date();
            const timeElapsed = now - new Date(timestamp.toLocaleString());
            
            const seconds = Math.floor(timeElapsed / 1000);
            const minutes = Math.floor(seconds / 60);
            const hours = Math.floor(minutes / 60);
            const days = Math.floor(hours / 24);
            const months = Math.floor(days / 30);
            const years = Math.floor(days / 365);
        
            if (seconds < 60) {
                return "Şimdi";
            } else if (minutes < 60) {
                return `${minutes} dk önce`;
            } else if (hours < 24) {
                return `${hours} saat önce`;
            } else if (days < 30) {
                return `${days} gün önce`;
            } else if (months < 12) {
                return `${months} ay önce`;
            } else {
                return `${years} yıl önce`;
            }
        }
    },
    data() {
      return {
        message: 'Hello Vue!',
        users: [],
        onlineUsers_data: [],
        selectedUserId: null,
        messages: [],
        myuser: {},
        messageText: '',
        token: '',
        connection: null,
        friendRequests: [],
        endMessageVariable: false,
      }
    }
}).mount('#app')

colors.forEach(color => {
  color.addEventListener('click', e => {
    colors.forEach(c => c.classList.remove('selected'));
    const theme = color.getAttribute('data-color');
    localStorage.setItem('theme', theme);
    document.body.setAttribute('data-theme', theme);
    color.classList.add('selected');
  });
});
toggleButton.addEventListener('click', () => {
  document.body.classList.toggle('dark-mode');
});

localStorage.getItem('theme') && document.body.setAttribute('data-theme', localStorage.getItem('theme'));






// onload = () => {
//     token = localStorage.getItem('token');
//     if (token) {
//         var tokenps = parseJwt(token);
//         userId = tokenps.sub;
//         document.getElementById('userProfile').src =  `https://randomuser.me/api/portraits/men/${tokenps.sub}.jpg`;
//         document.getElementById('myusername').innerHTML=tokenps.name;
//         document.getElementById('myuserId').innerHTML="#" + tokenps.sub;
//         document.getElementById('login').style.display = 'none';
//         document.getElementById('register').style.display = 'none';
//         document.getElementById('app').style.display = '';
//         localStorage.setItem('userId', userId);
//         startConnection();
//         loadUsers();
//         sendTokenToServer();
//         loadFriendRequests();
//     }else{
//         document.getElementById('username').value = localStorage.getItem('username');
//         document.getElementById('password').value = localStorage.getItem('password');
//         document.getElementById('rememberMe').checked = localStorage.getItem('rememberMe');
//         document.getElementById('app').style.display = 'none';
//     }
// }
// function logout(){
//     deleteFbToken();
//     localStorage.removeItem('token');
//     location.reload();  
// }

// function deleteFbToken(){
//     let token = localStorage.getItem('token');
//     let userId = localStorage.getItem('userId');
//     fetch('/api/FbToken/'+userId, { // Backend API endpoint'iniz
//         method: 'DELETE',
//         headers: {
//             'Content-Type': 'application/json',
//             'Authorization': `Bearer ${token}`,
//         },
//     })
//     .then(response => response.json())
//     .then(data => {
//         console.log('Token sunucuya gönderildi:', data);
//     })
//     .catch((error) => {
//         console.error('Token sunucuya gönderilemedi:', error);
//     });
// }
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
        var tokenps = parseJwt(token);
        userId = tokenps.sub;
        document.getElementById('userProfile').src =  `https://randomuser.me/api/portraits/men/${tokenps.sub}.jpg`;
        document.getElementById('myusername').innerHTML=tokenps.name.toUpperCase();
        document.getElementById('login').style.display = 'none';
        document.getElementById('register').style.display = 'none';
        document.getElementById('app').style.display = '';
        document.getElementById('register-message').textContent = '';
        if(rememberMe){
            localStorage.setItem('username', username);
            localStorage.setItem('password', password);
            localStorage.setItem('rememberMe', rememberMe);
            localStorage.setItem('userId', userId);
        }
        startConnection();
        loadUsers();
        sendTokenToServer();
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

// function startConnection() {
//     connection = new signalR.HubConnectionBuilder()
//         .withUrl('/chathub', { accessTokenFactory: () => token })
//         .build();

//     connection.on('ReceiveMessage', (message) => {
//         if (message.senderId == selectedUserId) {
//             msgDiv.innerHTML += 
//             `<div class="chat-msg"> 
//               <div class="chat-msg-profile">
//                 <img class="chat-msg-img" src="https://randomuser.me/api/portraits/men/${message.senderId}.jpg" alt="" />
//                 <div class="chat-msg-date">Message Time ${new Date(message.timestamp).toLocaleTimeString()}</div>
//               </div>
//               <div class="chat-msg-content">
//                 <div class="chat-msg-text">${message.content.content}</div>
//               </div>
//             </div>`;
//             msgDiv.scrollTop = msgDiv.scrollHeight; // Otomatik olarak en son mesaja kaydır
//         }
//         if(!isPageVisible()){
//                 showNotification(message.content.content,message.senderId,message.name,"message");
//         }
//         setLastMessage(message.senderId, message.content.content);
//     });

//     connection.on('ReceiveWrites', (senderId) => {
//         if (senderId == selectedUserId) {
//             document.getElementById('ReceiveWrites').style.display = 'flex';
//             setTimeout(() => {
//                 document.getElementById('ReceiveWrites').style.display = 'none';
//             },3000)
//         }
//     });
    
//     connection.on('UserStatusUpdated', (onlineUsers) => {
//         onlineUsers_data = onlineUsers;
//         const usersSelect = document.getElementById('users');
//         for (let i = 0; i < usersSelect.children.length; i++) {
//           if(onlineUsers_data.includes(users[i].id.toString()))usersSelect.children[i].classList.add("online");
//           else usersSelect.children[i].classList.remove("online");
//         }

//     });
//     connection.on('ReceiveNotification', (data) => {
//         if(data.type == "refreshFriendRequests") {
//             loadFriendRequests();
//             // showNotification("Arkadaş İsteğini Gönderdi",data.id,data.name,"message");
//         } 
//         else if(data.type == "refreshUsers"){
//             showNotification("Arkadaş İsteğin kabul Etti",data.id,data.name,"message");
//             loadUsers();
//         }
//     });
//     connection.onclose(() => {
//         updateConnectionStatus('Bağlantı kesildi. Yeniden bağlanma denemeleri başlatılıyor...');
//         attemptReconnect();
//     });

//     connection.onreconnecting(() => {
//         updateConnectionStatus('Bağlantı yeniden bağlanıyor...');
//     });

//     connection.onreconnected(() => {
//         updateConnectionStatus('Bağlantı başarıyla yeniden kuruldu.');
//     });
//     connection.start()
//         .then(() => {
//             updateConnectionStatus('Bağlantı başarıyla kuruldu.',3000);
//             console.log('SignalR connected')
//         })
//         .catch(err => console.error('SignalR connection error: ', err));
// }
// function showNotification(message,senderId,name,type) {
//     if ("Notification" in window && Notification.permission === "granted") {
//         const notification = new Notification("Yeni Mesaj", {
//             body: `Gönderen: ${name}\nMesaj: ${message}`,
//             icon: `https://randomuser.me/api/portraits/men/${senderId}.jpg`, // İsteğe bağlı
//             data: {  senderId: senderId } 
//         });
        
//         notification.onclick = (event) => {
//             if(type == "message"){
//                 window.focus();
//                 selectUser(senderId,name);
//                 notification.close();
//             }
//         };
//     }
// }
// function isPageVisible() {
//     return !document.hidden;
// }
// async function attemptReconnect() {
//     while (reconnectAttempts < maxReconnectAttempts) {
//         await new Promise(resolve => setTimeout(resolve, reconnectInterval)); // Belirli bir süre bekle

//         reconnectAttempts++;
//         console.log(`Yeniden bağlanma denemesi ${reconnectAttempts}...`);

//         try {
//             await connection.start(); // Yeniden bağlanmayı dene
//             console.log('Bağlantı başarıyla yeniden kuruldu.');
//             updateConnectionStatus('Bağlantı başarıyla yeniden kuruldu.');
//             reconnectAttempts = 0; // Başarılı olduğunda deneme sayacını sıfırla
//             return; // Yeniden bağlantı başarılıysa döngüden çık
//         } catch (err) {
//             console.error('Yeniden bağlanma hatası: ', err);
//         }
//     }
//     updateConnectionStatus('Maksimum yeniden bağlanma denemesi aşıldı. Lütfen internet bağlantınızı kontrol edin.',10000);
// }
// function updateConnectionStatus(status,time) {
//     const statusDiv = document.getElementById('connectionStatus');
//     statusDiv.textContent = status;
//     setTimeout(() => statusDiv.textContent = '', time ? time: 5000);
// }
// async function loadUsers() {
//     const response = await fetch('/api/Friend/GetChatFriends', {
//         method: 'GET',
//         headers: {
//             'Authorization': `Bearer ${token}`,
//             'Content-Type': 'application/json'
//         }
//     });
//     if (response.ok) {
//         users = await response.json();
//         const usersSelect = document.getElementById('users');
//         usersSelect.innerHTML = ''; // Önceki seçenekleri temizle
//         let htmlContent = ""
//         users.sort((a, b) => {
//             if (!a.lastMessage) return 1;
//             if (!b.lastMessage) return -1;
//             return new Date(b.lastMessage.timestamp) - new Date(a.lastMessage.timestamp);
//           });
//         users.forEach(user => {
//             htmlContent += 
//           ` <div id="${user.id}-user" @click="selectUser('${user.id}','${user.userName}')" class="msg ${onlineUsers_data.includes(user.id.toString()) ? 'online' : ''}">
//                 <div style="display: flex;align-items: center;">
//                     <img class="msg-profile" src="https://randomuser.me/api/portraits/men/${user.id}.jpg"  alt="" />
//                     <div class="msg-detail">
//                     <div class="msg-username">${user.userName}</div>
//                     <div class="msg-content">
//                     <span class="msg-message">${user.lastMessage ? user.lastMessage.content.slice(0, 20) : ''}</span>
//                     <span class="msg-date">${user.lastMessage ? timeAgo(user.lastMessage.timestamp) : ''}</span>
//                     </div>
//                 </div>
//                 <div>
//                 </div>
//             </div>
//           </div>`;
//         });
//         usersSelect.innerHTML = htmlContent;
//     } else {
//         if(response.status == 401) {
//             logout();
//         }
//         const error = await response.json();
//         alert('Failed to load users: ' + (error.message || 'Unknown error'));
//     }
// }

//  function selectUser(receiverId,name) {
//     page = 1
//     stop = false
//     selectedUserId = receiverId
//     msgDiv.innerHTML = "";
//     let chatAreaTitle = document.getElementById('chat-area-title');
//     chatAreaTitle.innerHTML = name
//     let usersSelect = document.getElementById('users');
//     for (let i = 0; i < usersSelect.children.length; i++) {
//         usersSelect.children[i].classList.remove("active")
//     }
//     document.getElementById(receiverId + '-user').classList.add("active")
//     document.getElementById("chat-area-footer").style.display = ""
//     isScrollingEnabled = false;
//     loadMessages(userId, receiverId);
//     conversationArea.classList.remove('open');
// }

// async function loadMessages(senderId, receiverId) {
//     if(stop) return
//     const response = await fetch('/api/message/GetMyMessages/' + senderId + '/' + receiverId + '/'+ page +'/' + pageSize, {
//         method: 'GET',
//         headers: {
//             'Authorization': `Bearer ${token}`,
//             'Content-Type': 'application/json'
//         }
//     });
//     if (response.ok) {
//         const messages = await response.json();
//         let htmlContent = "";
//         if(messages.length == 0 && page == 1) {
//           htmlContent = `<p style="text-align:center;color:orange">Mesajlar uçtan uca şifresizdir. Bende dahil olmak üzere bu sohbetin dışında bulunan herkes mesajları okuyabilir.</p> `
//           stop = true
//         } 
//         else if(messages.length == 0 && page > 1) {
//             htmlContent = `<p style="text-align:center">Daha fazla Mesaj Yok</p>`
//             stop = true
//         } 
//         messages.forEach(msg => {
//           htmlContent += 
//           `<div class="chat-msg ${msg.senderId == userId ? 'owner' : ''}"> 
//             <div class="chat-msg-profile">
//               <img class="chat-msg-img" src="https://randomuser.me/api/portraits/men/${msg.senderId}.jpg" alt="" />
//               <div class="chat-msg-date">Message Time ${new Date(msg.timestamp).toLocaleTimeString()}</div>
//             </div>
//             <div class="chat-msg-content">
//               <div class="chat-msg-text">${msg.content}</div>
//             </div>
//           </div>`;
//         });
//         var sy = msgDiv.scrollHeight;
//         msgDiv.innerHTML = htmlContent + msgDiv.innerHTML; 
//         if(page ==1) msgDiv.scrollTop = msgDiv.scrollHeight; 
//         else  msgDiv.scrollTop = msgDiv.scrollHeight - sy;
//         isScrollingEnabled = true;
//         page++;
//     } else {
//         const error = await response.json();
//         alert('Failed to load users: ' + (error.message || 'Unknown error'));
//     }
// }
// msgDiv.addEventListener('scroll', () => {
//     if (msgDiv.scrollTop === 0 && isScrollingEnabled )  {
//         loadMessages(userId, selectedUserId);
//     }
// });
// async function onChangeMsg (params) {
//     if(writeTime  == 0 ){
//         await connection.invoke('SendWrite', selectedUserId);
//         writeTime = 3000
//         setTimeout(() => {
//             writeTime = 0
//         },writeTime)
//     }
   
// }

// function setLastMessage(id, lastMessage) {
//     var htmltag = document.getElementById(id + '-user');
//     htmltag.getElementsByClassName("msg-message")[0].innerHTML = lastMessage
//     htmltag.getElementsByClassName("msg-date")[0].innerHTML = timeAgo(new Date());
// }
// async function sendMessage() {
//     const receiverId = selectedUserId
//     const message = document.getElementById('message').value.trim();

//     if (connection && message !== '') {
//         try {
//             var msg = {
//                 SenderId: parseInt(userId),
//                 ReceiverId: parseInt(receiverId),
//                 Content: message,
//                 Timestamp: new Date()
//             }
//             await connection.invoke('SendMessage', msg);
//             // const msgDiv = document.getElementById('messages');
//             msgDiv.innerHTML += 
//             `<div class="chat-msg owner"> 
//               <div class="chat-msg-profile">
//                 <img class="chat-msg-img" src="https://randomuser.me/api/portraits/men/${msg.SenderId}.jpg" alt="" />
//                 <div class="chat-msg-date">Message Time ${new Date(msg.Timestamp).toLocaleTimeString()}</div>
//               </div>
//               <div class="chat-msg-content">
//                 <div class="chat-msg-text">${msg.Content}</div>
//               </div>
//             </div>`;
//             msgDiv.scrollTop = msgDiv.scrollHeight; // Otomatik olarak en son mesaja kaydır
//             document.getElementById('message').value = '';
//             setLastMessage(receiverId, msg.Content);
//         } catch (err) {
//             console.error('SendMessage error: ', err);
//         }
//     }
// }

// function toggleConversationArea() {
//     conversationArea.classList.toggle('open');
// }
// function sendFriendRequestBtn() {
//     Swal.fire({
//         title: 'Arkadaş İsteği Gönder',
//         html:
//             `<input type="text" id="userName" class="swal2-input" placeholder="Kullanıcı Adı">
//              <input type="text" id="userId" class="swal2-input" placeholder="Kullanıcı ID">`,
//         showCancelButton: true,
//         confirmButtonText: 'Arkadaş İsteği Gönder',
//         cancelButtonText: 'İptal',
//         preConfirm: () => {
//             const userName = Swal.getPopup().querySelector('#userName').value;
//             const userId = Swal.getPopup().querySelector('#userId').value;

//             if (!userName || !userId) {
//                 Swal.showValidationMessage(`Lütfen hem kullanıcı adını hem de ID'yi girin`);
//                 return false;
//             }
            
//             return { userName: userName, userId: userId };
//         }
//     }).then((result) => {
//         if (result.isConfirmed) {
//             AddFriendRequest(result.value.userName, result.value.userId);
//         }
//     });
// }

// function AddFriendRequest(userName, ReceiverId) {
//     if(ReceiverId == userId){
//         Swal.fire(
//             'Hata!',
//             'Kendinize arkadaş isteği gönderemezsiniz.',
//             'error'
//         );
//         return
//     }
//     fetch('/api/FriendRequest', { // Backend API endpoint'iniz
//         method: 'POST',
//         headers: {
//             'Content-Type': 'application/json',
//             'Authorization': `Bearer ${token}`,
//         },
//         body: JSON.stringify({ 
//             SenderId: parseInt(userId), // userId,
//             ReceiverId: parseInt(ReceiverId), // ReceiverId,
//             Status:0
//         })
//     })
//     .then(response => response.json())
//     .then(data => {
//         if(data && data.id){
//             Swal.fire(
//                 'Gönderildi!',
//                 `${userName} (ID: ${ReceiverId}) kişisine arkadaş isteği gönderildi.`,
//                 'success'
//             );
//         }else{
//             Swal.fire(
//                 'Hata!',
//                 data.message ? data.message : 'Kullanıcı bulunamadı.',
//                 'error'
//             );
//         }
        
//     })
//     .catch((error) => {
//         console.error('Arkadaş isteği gönderilemedi:', error);
//     });
// }

// async function loadFriendRequests() {
//     const response = await fetch('/api/FriendRequest/GetFriendRequestByReceiverId/' + userId, {
//         method: 'GET',
//         headers: {
//             'Authorization': `Bearer ${token}`,
//             'Content-Type': 'application/json'
//         }
//     });
//     if (response.ok) {
//         friendRequests = await response.json();
//         if(friendRequests.length) {
//             var notification = document.getElementById('notifications');
//             var notificationCount = document.getElementById('notificationCount');
//             notification.style.display = 'inline-block';
//             notificationCount.innerHTML = friendRequests.length;
//         }else{
//             var notification = document.getElementById('notifications');
//             notification.style.display = 'none';
//         }
        
//     } else {
//         if(response.status == 401) {
//             logout();
//         }
//         const error = await response.json();
//         alert('Failed to load users: ' + (error.message || 'Unknown error'));
//     }
// }

// function openFriendRequests() {
//     let requestsHtml = '';
//     friendRequests.forEach(request => {
//         requestsHtml += `
//              <div class="request-item">
//                 <p><i class="fas fa-user icon"></i> <strong>${request.sender.username}</strong> (ID: ${request.senderId})</p>
//                 <div>
//                     <button class="accept-btn" data-id="${request.id}" style="background-color: #28a745; margin-right: 5px;">
//                         <i class="fas fa-check"></i> Onayla
//                     </button>
//                     <button class="reject-btn" data-id="${request.id}" style="background-color: #dc3545;">
//                         <i class="fas fa-times"></i> Reddet
//                     </button>
//                 </div>
//             </div>
//         `;
//     });

//     Swal.fire({
//         title: 'Gelen Arkadaşlık İstekleri',
//         html: requestsHtml,
//         showCloseButton: true,
//         showConfirmButton: false, // Onayla/Reddet butonlarını özel olarak ekliyoruz
//         didOpen: () => {
//             document.querySelectorAll('.accept-btn').forEach(button => {
//                 button.addEventListener('click', (event) => {
//                     const requestId = event.target.getAttribute('data-id');
//                     handleRequestAction(requestId, '1');
//                 });
//             });

//             document.querySelectorAll('.reject-btn').forEach(button => {
//                 button.addEventListener('click', (event) => {
//                     const requestId = event.target.getAttribute('data-id');
//                     handleRequestAction(requestId, '2');
//                 });
//             });
//         }
//     });
// }



// function handleRequestAction(requestId, action) {
//     const request = friendRequests.find(req => req.id == requestId);
//     fetch('/api/FriendRequest/AcceptAndRejectFriendRequest/' + request.id + "/" + action, { 
//         method: 'PUT',
//         headers: {
//             'Content-Type': 'application/json',
//             'Authorization': `Bearer ${token}`,
//         },
//     })
//     .then(data => {
//         if(data.ok){
//             if (action === '1') {
//             Swal.fire(
//                 'Onaylandı!',
//                 `${request.sender.username} (ID: ${request.senderId}) arkadaş olarak eklendi.`,
//                 'success'
//             );
//             } else if (action === '2') {
//                 Swal.fire(
//                     'Reddedildi!',
//                     `${request.sender.username} (ID: ${request.senderId}) arkadaşlık isteği reddedildi.`,
//                     'error'
//                 );
//             }
//             loadUsers();
//             loadFriendRequests();
//         }        
//     })
//     .catch((error) => {
//         console.error('Arkadaş isteği gönderilemedi:', error);
//     });
// }

// function openSettings() {
//     var settingsBox = document.getElementById('settingsBox');
//     settingsBox.style.display = settingsBox.style.display == 'none' ? 'block' : 'none';
// }
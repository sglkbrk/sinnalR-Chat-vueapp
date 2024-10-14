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
        localStorage.getItem('theme') && document.body.setAttribute('data-theme', localStorage.getItem('theme'));
        this.token = localStorage.getItem('token');
        if (this.token) {
            this.myuser = parseJwt( this.token)
            this.myuser.picture =  `https://randomuser.me/api/portraits/men/${this.myuser.sub}.jpg`;
            localStorage.setItem('userId',this.myuser.sub );
            this.startConnection();
            this.loadUsers();
            sendTokenToServer();
            this.loadFriendRequests();
        }else{
            this.username = localStorage.getItem('username');
            this.password = localStorage.getItem('password');
            this.rememberMe = localStorage.getItem('rememberMe');
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
        if(this.$refs.msgDiv){
            this.$refs.msgDiv.addEventListener('scroll', () => {
                if (this.$refs.msgDiv.scrollTop === 0 && isScrollingEnabled )  {
                    this.loadMessages(this.myuser.sub, this.selectedUserId);
                }
            });
        }  
    },
    computed: {
        sortedUsers() {
            return this.sortUsersByLastMessage(this.users);
        }
    },
    methods: {
        changeTheme(){
            document.body.classList.toggle('dark-mode');
        },
        changeColor(color){
            localStorage.setItem('theme', color);
            document.body.setAttribute('data-theme', color);
        },
        async  login() {
            var json = {
                username:this.username,
                password:this.password
            }
            
            const response = await fetch('/api/UserAuth/login', {
                method: 'POST',
                headers: {'Content-Type': 'application/json'},
                body: JSON.stringify(json)
            });
            if (response.ok) {
                const data = await response.json();
                this.token = data.token;
                localStorage.setItem('token',  this.token);
                this.myuser = parseJwt( this.token);
                this.myuser.picture =  `https://randomuser.me/api/portraits/men/${this.myuser.sub}.jpg`;
                this.registerMessage = '';
                if(this.rememberMe){
                    localStorage.setItem('username', this.username);
                    localStorage.setItem('password', this.password);
                    localStorage.setItem('rememberMe', this.rememberMe);
                    localStorage.setItem('userId', this.myuser.sub );
                }else{
                    localStorage.removeItem('username');
                    localStorage.removeItem('password');
                    localStorage.removeItem('rememberMe');
                }
                this.startConnection();
                this.loadUsers();
                sendTokenToServer();
            } else {
                const error = await response.json();
                alert('Login failed: ' + (error.message || 'Unknown error'));
            }
        },
        async  register() {
            if (this.register.username === '' || this.register.email === '' || this.register.password === '') {
                alert('Lütfen bilgileri eksiksiz doldurun');
                return;
            }
            const response = await fetch('/api/UserAuth/register', {
                method: 'POST',
                headers: {'Content-Type': 'application/json'},
                body: JSON.stringify({ Username: this.register.username, Email: this.register.email, Password: this.register.password })
            });
            if (response.ok) {
                this.registerMessage = "Kayıt başarılı! Lütfen giriş yapın.";
                setTimeout(() => {
                    this.registerMessage = '';
                },5000)
                this.register = {
                    username: '',
                    email: '',
                    password: ''
                }
            } else {
                const error = await response.json();
                this.registerMessage = 'Kayıt başarısız: ' + (error.errors || 'Lüfen Bilgileri doğrı girin ');
            }
        },
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
                if(!this.isPageVisible())
                    this.showNotification(message.content.content,message.senderId,message.name,"message");
                
                this.setLastMessage(message.senderId, message.content.content);
            });
        
            this.connection.on('ReceiveWrites', (senderId) => {
                if (senderId == this.selectedUserId) {
                    this.$refs.ReceiveWrites.style.display = 'flex';
                    setTimeout(() => {
                        this.$refs.ReceiveWrites.style.display  = 'none';
                    },3000)
                }
            });
            this.connection.on('UserStatusUpdated', (onlineUsers) => {
                this.onlineUsers_data = onlineUsers;
            });
            this.connection.on('ReceiveNotification', (data) => {
                if(data.type == "refreshFriendRequests")  this.loadFriendRequests();
                else if(data.type == "refreshUsers"){
                    this.showNotification("Arkadaş İsteğin kabul Etti",data.id,data.name,"message");
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
            this.statusText = status;
            setTimeout(() => this.statusText = '', time ? time: 5000);
        },
        setLastMessage(id, lastMessage) {
            const index = this.users.findIndex(user => user.id.toString() === id.toString());
            const updatedUsers = [...this.users]; // Derin kopya oluşturuyoruz
            updatedUsers[index] = {
                ...updatedUsers[index],
                lastMessage: {
                timestamp: new Date(),
                content: lastMessage
                }
            };
            this.users = this.sortUsersByLastMessage(updatedUsers);
        },
        sortUsersByLastMessage(updatedUsers) {
            updatedUsers.sort((a, b) => {
                return new Date(b.lastMessage.timestamp) - new Date(a.lastMessage.timestamp);
            });
            return updatedUsers;
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
            this.settingsBoxVis = !this.settingsBoxVis
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
                this.users  = this.sortUsersByLastMessage( await response.json());
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
            const timeElapsed = now - new Date(timestamp);
            
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
        username: '',
        password: '',
        rememberMe: false,
        statusText: '',
        registerItem:{
            username: '',
            email: '',
            password: ''
        },
        registerMessage: '',
        settingsBoxVis: false,
      }
    }
}).mount('#deneme')
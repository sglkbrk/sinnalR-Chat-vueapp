let reconnectAttempts = 0; // Yeniden bağlanma denemeleri sayacı
const maxReconnectAttempts = 20; // Maksimum yeniden bağlanma denemesi
const reconnectInterval = 5000; 
let writeTime=0
let page = 1;
const pageSize = 30;
let isScrollingEnabled = true; 
const { createApp } = Vue

createApp({
    data() {
        return {
            items: [{ src: 'https://via.placeholder.com/600', w: 600, h: 400 }],
            users: [],
            onlineUsers_data: [],
            selectedUserId: null,
            messages: [],
            cachesMessages: {},
            myuser: {},
            messageText: '',
            token: '',
            connection: null,
            friendRequests: [],
            endMessageVariable: false,
            loginItem: {
                username: '',
                password: '',
                rememberMe: false,
            },       
            statusText: '',
            registerItem: {
                username: '',
                email: '',
                password: ''
            },
            registerMessage: '',
            authError: '',
            isAuthLoading: false,
            settingsBoxVis: false,
            showEmojiPicker: false,
            darkMode: false,
            acceptType: 'image/*',
            selectedUserName: null,
            showLogin: true,
            // Phase 2: Modals & Profile Management
            showSettingsModal: false,
            showSearchModal: false,
            searchActiveTab: 'search',
            searchQuery: '',
            searchResults: [],
            isSearching: false,
            searchDebounceTimer: null,
            newStatusText: '',
            isSavingStatus: false,
            isUploadingAvatar: false,
            // Phase 3: Modern UI & PWA State
            chatFilterQuery: '',
            isPeerTyping: false,
            peerTypingTimeout: null,
            showScrollBottomBtn: false,
            canInstallPWA: false,
            deferredPrompt: null,
            // Phase 4: Core Chat Features
            replyingToMessage: null,
            editingMessage: null,
            quickReactionEmojis: ['👍', '❤️', '😂', '😮', '😢', '🙏'],
            showInChatSearch: false,
            inChatSearchQuery: '',
            searchMatches: [],
            searchMatchIndex: 0,
            isRecordingAudio: false,
            audioRecordDuration: 0,
            audioRecordTimer: null,
            mediaRecorder: null,
            audioChunks: [],
            // Phase 5: Fun Features State
            sentFriendRequests: [],
            buzzCooldown: 0,
            buzzCooldownTimer: null,
            showSelfDestructMenu: false,
            selectedSelfDestructSeconds: 0,
            expireCheckInterval: null,
            showSlashMenu: false,
            slashSelectedIndex: 0,
            slashCommands: [
                { command: '/zar', icon: '🎲', description: '1-6 arası rastgele zar atar', immediate: true },
                { command: '/yazitura', icon: '🪙', description: 'Havaya para atar (Yazı / Tura)', immediate: true },
                { command: '/rastgele', icon: '🎯', description: 'Rastgele sayı üretir (örn: /rastgele 1-100)', immediate: false, template: '/rastgele 1-100' },
                { command: '/xox', icon: '❌', description: 'Gerçek zamanlı XOX (Tic-Tac-Toe) oyunu başlatır', immediate: true },
                { command: '/tkm', icon: '✂️', description: 'Gerçek zamanlı Taş-Kağıt-Makas düellosu başlatır', immediate: true },
                { command: '/durt', icon: '⚡', description: 'Karşı tarafın ekranını sallar ve ses çalar', immediate: true },
                { command: '/spoiler', icon: '🕵️', description: 'Dokununca açılan gizli mesaj gönderir', immediate: false, template: '/spoiler ' },
                { command: '/yardim', icon: 'ℹ️', description: 'Tüm kullanılabilir komutları gösterir', immediate: true }
            ]
        }
    },
    setup() {

    },
    mounted() {
        window.vueAppInstance = this;
        localStorage.removeItem('password'); // Eski güvenlik açığını temizle
        localStorage.getItem('theme') && document.body.setAttribute('data-theme', localStorage.getItem('theme'));
        const savedDarkMode = localStorage.getItem('darkMode');
        if (savedDarkMode === 'true') {
            this.darkMode = true;
            document.body.classList.add('dark-mode');
        } else {
            this.darkMode = false;
            document.body.classList.remove('dark-mode');
        }
        this.token = localStorage.getItem('token');
        if (this.token) {
            this.myuser = this.parseJwt(this.token);
            this.myuser.id = parseInt(this.myuser.sub);
            localStorage.setItem('userId', this.myuser.sub);
            this.startConnection();
            this.loadMyProfile();
            this.loadUsers();
            if (typeof window.sendTokenToServer === 'function') {
                window.sendTokenToServer();
            }
            this.loadFriendRequests();
        } else {
            this.loginItem.username = localStorage.getItem('username') || '';
            this.loginItem.rememberMe = localStorage.getItem('rememberMe') === 'true';
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
        // PWA Kurulum dinleyicisi
        window.addEventListener('beforeinstallprompt', (e) => {
            e.preventDefault();
            this.deferredPrompt = e;
            this.canInstallPWA = true;
        });

        document.addEventListener('visibilitychange', this.handleVisibilityChange);
        window.addEventListener('keydown', this.handleKeydown);
        document.addEventListener('click', (event) => {
            if (this.showEmojiPicker && this.$refs.emojiPicker && !this.$refs.emojiPicker.contains(event.target) && (!this.$refs.emojiIcon || event.target !== this.$refs.emojiIcon)) {
                this.showEmojiPicker = false;
            }
        });

        // Phase 5: Süresi dolan mesajları her saniye kontrol et
        this.expireCheckInterval = setInterval(() => {
            this.checkExpiredMessages();
        }, 1000);
    },
    beforeDestroy() {
        document.removeEventListener('visibilitychange', this.handleVisibilityChange);
        window.removeEventListener('keydown', this.handleKeydown);
        if (this.expireCheckInterval) clearInterval(this.expireCheckInterval);
        if (this.buzzCooldownTimer) clearInterval(this.buzzCooldownTimer);
    },
    watch: {
        messageText(newVal) {
            if (newVal && newVal.startsWith('/') && !newVal.includes(' ')) {
                this.showSlashMenu = true;
                this.slashSelectedIndex = 0;
            } else {
                this.showSlashMenu = false;
            }
        }
    },
    computed: {
        filteredSlashCommands() {
            const text = (this.messageText || '');
            if (!text.startsWith('/')) return [];
            const parts = text.split(' ');
            if (parts.length > 1) return [];
            const query = parts[0].toLowerCase();
            return this.slashCommands.filter(c => c.command.toLowerCase().startsWith(query));
        },
        sortedUsers() {
            return this.sortUsersByLastMessage(this.users);
        },
        filteredUsers() {
            const query = (this.chatFilterQuery || '').trim().toLowerCase();
            const list = this.sortUsersByLastMessage(this.users);
            if (!query) return list;
            return list.filter(u =>
                (u.userName && u.userName.toLowerCase().includes(query)) ||
                (u.lastMessage && u.lastMessage.content && u.lastMessage.content.toLowerCase().includes(query)) ||
                (u.statusMessage && u.statusMessage.toLowerCase().includes(query))
            );
        },
        selectedUser() {
            return this.users.find(u => u.id === this.selectedUserId) || null;
        },
        selectedUserSharedImages() {
            if (!this.messages || !this.messages.length) return [];
            const list = [];
            for (const group of this.messages) {
                if (group && group.messages) {
                    for (const msg of group.messages) {
                        if (msg.type == 1 && msg.fileUrl && !msg.isDeleted) {
                            list.push(msg.fileUrl);
                        }
                    }
                }
            }
            return list;
        }
    },
    methods: {
        changeTheme(){
            this.darkMode = !this.darkMode;
            document.body.classList.toggle('dark-mode', this.darkMode);
            localStorage.setItem('darkMode', this.darkMode ? 'true' : 'false');
        },
        changeColor(color){
            localStorage.setItem('theme', color);
            document.body.setAttribute('data-theme', color);
        },
        handleVisibilityChange: function () {
            if(this.selectedUserId)this.sendSeen(this.selectedUserId,2);
        },
        handleKeydown: function (event) {
            if (event.key === 'Escape') {
                if (this.showSettingsModal) {
                    this.closeSettingsModal();
                } else if (this.showSearchModal) {
                    this.closeSearchModal();
                } else if (this.showSlashMenu) {
                    this.showSlashMenu = false;
                } else if (this.showEmojiPicker) {
                    this.showEmojiPicker = false;
                } else if (this.showSelfDestructMenu) {
                    this.showSelfDestructMenu = false;
                } else if (this.showInChatSearch) {
                    this.closeInChatSearch();
                } else if (this.replyingToMessage) {
                    this.cancelReply();
                } else if (this.editingMessage) {
                    this.cancelEdit();
                } else if (this.settingsBoxVis) {
                    this.settingsBoxVis = false;
                }
            }
        },
        toggleForm() {
            this.showLogin = !this.showLogin; // Toggle form between login and register
          },
        async login() {
            this.authError = '';
            if (!this.loginItem.username || !this.loginItem.password) {
                this.authError = 'Lütfen kullanıcı adı ve şifrenizi girin.';
                return;
            }
            this.isAuthLoading = true;
            try {
                const json = {
                    username: this.loginItem.username,
                    password: this.loginItem.password
                };
                
                const response = await fetch('/api/UserAuth/login', {
                    method: 'POST',
                    headers: {'Content-Type': 'application/json'},
                    body: JSON.stringify(json)
                });
                const data = await response.json();
                if (response.ok && (data.token || (data.data && data.data.token))) {
                    this.token = data.token || data.data.token;
                    localStorage.setItem('token', this.token);
                    this.myuser = this.parseJwt(this.token);
                    this.myuser.id = parseInt(this.myuser.sub);
                    localStorage.setItem('userId', this.myuser.sub);
                    this.registerMessage = '';
                    if (this.loginItem.rememberMe) {
                        localStorage.setItem('username', this.loginItem.username);
                        localStorage.setItem('rememberMe', 'true');
                    } else {
                        localStorage.removeItem('username');
                        localStorage.removeItem('rememberMe');
                    }
                    localStorage.removeItem('password');
                    this.loadMyProfile();
                    this.startConnection();
                    if (typeof window.sendTokenToServer === 'function') {
                        window.sendTokenToServer();
                    }
                    this.loadUsers();
                    this.loadFriendRequests();
                } else {
                    const msg = data.message || (data.error && data.error.message) || 'Giriş başarısız. Bilgilerinizi kontrol edin.';
                    this.authError = msg;
                }
            } catch (err) {
                this.authError = 'Sunucuya bağlanırken bir hata oluştu.';
                console.error('Login error:', err);
            } finally {
                this.isAuthLoading = false;
            }
        },
        parseJwt(token) {
            try {
                const base64Url = token.split('.')[1];
                const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
                const jsonPayload = decodeURIComponent(escape(atob(base64)));
                return JSON.parse(jsonPayload);
            } catch (e) {
                console.error('JWT parse hatası:', e);
                return {};
            }
        },
        async register() {
            this.authError = '';
            const u = this.registerItem.username ? this.registerItem.username.trim() : '';
            const e = this.registerItem.email ? this.registerItem.email.trim() : '';
            const p = this.registerItem.password || '';

            if (!u || !e || !p) {
                this.authError = 'Lütfen tüm alanları eksiksiz doldurun.';
                return;
            }
            if (p.length < 6) {
                this.authError = 'Şifre en az 6 karakter olmalıdır.';
                return;
            }

            this.isAuthLoading = true;
            try {
                const response = await fetch('/api/UserAuth/register', {
                    method: 'POST',
                    headers: {'Content-Type': 'application/json'},
                    body: JSON.stringify({ Username: u, Email: e, Password: p })
                });
                const data = await response.json();
                if (response.ok && data.success !== false) {
                    this.registerMessage = "Kayıt başarılı! Lütfen giriş yapın.";
                    this.loginItem.username = u;
                    this.registerItem = {
                        username: '',
                        email: '',
                        password: ''
                    };
                    this.showLogin = true;
                    setTimeout(() => {
                        this.registerMessage = '';
                    }, 5000);
                } else {
                    const msg = data.message || (data.error && data.error.message) || 'Kayıt başarısız.';
                    this.authError = msg;
                }
            } catch (err) {
                this.authError = 'Sunucuya bağlanırken bir hata oluştu.';
                console.error('Register error:', err);
            } finally {
                this.isAuthLoading = false;
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
                .withUrl('/chathub', { accessTokenFactory: () => this.token })
                .withAutomaticReconnect([0, 2000, 5000, 10000, 20000])
                .build();

            this.connection.on('ReceiveMessage', (message) => {
                const rawContent = message.Content || message.content;
                const senderId = message.SenderId || message.senderId;
                const timestamp = message.Timestamp || message.timestamp || new Date();
                const msgId = message.Id || message.id || (rawContent && rawContent.id);
                const replyToMessage = message.ReplyToMessage || message.replyToMessage || (rawContent && rawContent.replyToMessage);
                const replyToMessageId = message.ReplyToMessageId || message.replyToMessageId || (rawContent && rawContent.replyToMessageId);
                const metadataJson = message.MetadataJson || message.metadataJson || (rawContent && (rawContent.metadataJson || rawContent.MetadataJson)) || null;
                const expiresAt = message.ExpiresAt || message.expiresAt || (rawContent && (rawContent.expiresAt || rawContent.ExpiresAt)) || null;

                if (senderId == this.selectedUserId) {
                    var item = {
                        "id": msgId,
                        "senderId": senderId,
                        "receiverId": rawContent ? rawContent.receiverId : message.receiverId,
                        "content": rawContent ? (rawContent.content || '') : (message.content || ''),
                        "timestamp": timestamp,
                        "status": rawContent ? rawContent.status : (message.status || 0),
                        "type": rawContent ? rawContent.type : (message.type || 0),
                        "fileUrl": rawContent ? rawContent.fileUrl : message.fileUrl,
                        "metadataJson": metadataJson,
                        "expiresAt": expiresAt,
                        "replyToMessageId": replyToMessageId,
                        "replyToMessage": replyToMessage,
                        "reactions": rawContent && rawContent.reactions ? rawContent.reactions : {},
                        "editedAt": rawContent ? rawContent.editedAt : null,
                        "isPlaying": false,
                        "audioProgress": 0,
                        "audioCurrentTime": 0,
                        "audioDuration": 0,
                        "showReactionPicker": false,
                        "isHovered": false
                    };

                    if (this.messages.length && 
                        this.messages[this.messages.length - 1].senderId == item.senderId &&
                        item.type != 6 &&
                        !this.messages[this.messages.length - 1].messages.some(m => m.type == 6)) {
                        this.messages[this.messages.length - 1].messages.push(item);
                    } else {
                        this.messages.push({
                            senderId: item.senderId,
                            messages: [item]
                        });
                    }
                    this.sendSeen(item.senderId, 2);
                    this.scrollToBottom(true);
                } else {
                    const contentStr = rawContent ? (rawContent.content || '') : (message.content || '');
                    if (!this.isPageVisible()) {
                        this.showNotification(contentStr, senderId, message.name || message.Name, "message");
                    }
                    this.sendSeen(senderId, 1);
                    this.setNotMessageCount(senderId, true);
                }   
                this.setLastMessage(senderId, rawContent ? rawContent.content : message.content);
            });

            this.connection.on('MessageSentAck', (ack) => {
                if (this.messages && this.messages.length) {
                    const lastGroup = this.messages[this.messages.length - 1];
                    if (lastGroup && lastGroup.messages && lastGroup.messages.length) {
                        const lastMsg = lastGroup.messages[lastGroup.messages.length - 1];
                        if (lastMsg) {
                            if (!lastMsg.id || lastMsg.id === 0) {
                                lastMsg.id = ack.id || ack.Id;
                            }
                            if (ack.content || ack.Content) {
                                lastMsg.content = ack.content || ack.Content;
                            }
                            if (ack.type !== undefined || ack.Type !== undefined) {
                                lastMsg.type = ack.type !== undefined ? ack.type : ack.Type;
                            }
                            if (ack.metadataJson || ack.MetadataJson) {
                                lastMsg.metadataJson = ack.metadataJson || ack.MetadataJson;
                            }
                            if (ack.replyToMessage || ack.ReplyToMessage) {
                                lastMsg.replyToMessage = ack.replyToMessage || ack.ReplyToMessage;
                            }
                        }
                    }
                }
            });

            // Phase 4: Mesaj Düzenleme Dinleyicisi
            this.connection.on('ReceiveEditMessage', (data) => {
                const msgId = data.id || data.Id;
                const newContent = data.content || data.Content;
                const editedAt = data.editedAt || data.EditedAt;
                this.messages.forEach(group => {
                    if (group && group.messages) {
                        group.messages.forEach(m => {
                            if (m.id == msgId) {
                                m.content = newContent;
                                m.editedAt = editedAt;
                            }
                        });
                    }
                });
            });

            // Phase 4: Tepki (Reaction) Dinleyicisi
            this.connection.on('ReceiveReaction', (data) => {
                const msgId = data.messageId || data.MessageId;
                const reactions = data.reactions || data.Reactions || {};
                this.messages.forEach(group => {
                    if (group && group.messages) {
                        group.messages.forEach(m => {
                            if (m.id == msgId) {
                                m.reactions = { ...reactions };
                            }
                        });
                    }
                });
            });

            // Phase 4: Sohbet Temizleme Dinleyicisi
            this.connection.on('ReceiveClearChat', (friendId) => {
                const target = this.users.find(u => u.id == friendId);
                if (target) {
                    target.lastMessage = null;
                    target.notSeenMessagesCount = 0;
                    this.users = this.sortUsersByLastMessage(this.users);
                }
                if (this.selectedUserId == friendId) {
                    this.messages = [];
                    if (this.cachesMessages) {
                        this.cachesMessages[friendId] = [];
                    }
                }
            });

            // Phase 5: Dürtme (Buzz) Dinleyicisi
            this.connection.on('ReceiveBuzz', (data) => {
                this.playBuzzEffect(data);
                if (data.senderId == this.selectedUserId) {
                    this.messages.push({
                        senderId: data.senderId,
                        messages: [{
                            id: data.id || Date.now(),
                            senderId: data.senderId,
                            receiverId: data.receiverId,
                            content: data.content || "⚡ Dürtüldünüz!",
                            timestamp: data.timestamp || new Date(),
                            type: 5,
                            reactions: {},
                            status: 2
                        }]
                    });
                }
                this.setLastMessage(data.senderId, data.content || "⚡ Dürtüldünüz!");
            });

            this.connection.on('BuzzSentAck', (data) => {
                if (this.selectedUserId == data.receiverId) {
                    this.messages.push({
                        senderId: parseInt(this.myuser.sub),
                        messages: [{
                            id: data.id || Date.now(),
                            senderId: parseInt(this.myuser.sub),
                            receiverId: data.receiverId,
                            content: data.content || "⚡ Dürtme gönderildi!",
                            timestamp: data.timestamp || new Date(),
                            type: 5,
                            reactions: {},
                            status: 0
                        }]
                    });
                }
                this.setLastMessage(this.selectedUserId, data.content || "⚡ Dürtme gönderildi!");
            });

            // Phase 5: Yeni Oyun Başlatıldığında Anlık Gösterim (XOX & Taş-Kağıt-Makas)
            this.connection.on('ReceiveGameStarted', (data) => {
                const msgId = data.id || data.Id;
                const senderId = data.senderId || data.SenderId;
                const receiverId = data.receiverId || data.ReceiverId;
                const content = data.content || data.Content || '🎮 Oyun Başladı!';
                const metadataJson = data.metadataJson || data.MetadataJson;
                const timestamp = data.timestamp || data.Timestamp || new Date();
                const peerId = senderId == this.myuser.sub ? receiverId : senderId;

                if (peerId == this.selectedUserId) {
                    // Aynı id'li oyun kartı zaten eklendiyse tekrar ekleme
                    const alreadyExists = this.messages.some(g => g && g.messages && g.messages.some(m => m.id == msgId));
                    if (!alreadyExists) {
                        const gameItem = {
                            id: msgId,
                            senderId: senderId,
                            receiverId: receiverId,
                            content: content,
                            timestamp: timestamp,
                            status: senderId == this.myuser.sub ? 0 : 2,
                            type: 6,
                            metadataJson: metadataJson,
                            reactions: {},
                            showReactionPicker: false,
                            isHovered: false
                        };
                        this.messages.push({
                            senderId: senderId,
                            messages: [gameItem]
                        });
                    }
                    if (senderId != this.myuser.sub) {
                        this.sendSeen(senderId, 2);
                    }
                    this.scrollToBottom(true);
                } else if (senderId != this.myuser.sub) {
                    this.sendSeen(senderId, 1);
                    this.setNotMessageCount(senderId, true);
                }
                this.setLastMessage(peerId, content);
            });

            // Phase 5: XOX ve Taş-Kağıt-Makas Oyun Güncellemesi
            this.connection.on('ReceiveGameUpdate', (data) => {
                const msgId = data.messageId || data.MessageId;
                const state = data.state || data.State;
                const content = data.content || data.Content;
                this.messages.forEach(group => {
                    if (group && group.messages) {
                        group.messages.forEach(m => {
                            if (m.id == msgId) {
                                m.content = content;
                                m.metadataJson = typeof state === 'string' ? state : JSON.stringify(state);
                            }
                        });
                    }
                });
                const winner = state && (state.winner || state.Winner);
                if (winner && winner !== 'Draw') {
                    this.triggerConfetti();
                }
                this.scrollToBottom(true);
            });

            // Phase 5: Süresi Dolan Mesaj Bildirimi
            this.connection.on('ReceiveMessageExpired', (data) => {
                const msgId = data.messageId || data.MessageId;
                this.messages.forEach(group => {
                    if (group && group.messages) {
                        group.messages.forEach(m => {
                            if (m.id == msgId) {
                                m.isDeleted = true;
                                m.content = data.content || "🔥 Bu mesajın süresi doldu.";
                            }
                        });
                    }
                });
            });
        
            this.connection.on('ReceiveWrites', (senderId) => {
                if (senderId == this.selectedUserId) {
                    this.isPeerTyping = true;
                    if (this.peerTypingTimeout) clearTimeout(this.peerTypingTimeout);
                    this.peerTypingTimeout = setTimeout(() => {
                        this.isPeerTyping = false;
                    }, 3000);

                    if (this.$refs.ReceiveWrites) {
                        this.$refs.ReceiveWrites.style.display = 'flex';
                        setTimeout(() => {
                            if (this.$refs.ReceiveWrites) {
                                this.$refs.ReceiveWrites.style.display = 'none';
                            }
                        }, 3000);
                    }
                }
            });

            this.connection.on('UserStatusUpdated', (onlineUsers) => {
                this.onlineUsers_data = onlineUsers;
            });

            this.connection.on('ReceiveSeen', (userid, messageStatus) => {
                if (this.selectedUserId == userid) {
                    this.messages.forEach(group => {
                        if (group && group.messages) {
                            group.messages.forEach(m => {
                                if (m.senderId == this.myuser.sub && m.status != 2) {
                                    m.status = messageStatus;
                                }
                            });
                        }
                    });    
                }
            });

            this.connection.on('ReceiveNotification', (data) => {
                if (data.type == "refreshFriendRequests") this.loadFriendRequests();
                else if (data.type == "refreshUsers") {
                    this.showNotification("Arkadaş isteğin kabul edildi", data.id, data.name, "message");
                    this.loadUsers();
                }
            });

            this.connection.on('ReceiveDeleteMessage', (userId, msgid, forEveryone) => {
                const isEveryone = forEveryone === undefined || forEveryone === true;
                if (isEveryone) {
                    this.messages.forEach(msgGroup => {
                        if (msgGroup && msgGroup.messages) {
                            msgGroup.messages.forEach(x => {
                                if (x.id == msgid) {
                                    x.isDeleted = true;
                                    x.content = "Bu Mesaj Silindi";
                                }
                            });
                        }
                    });
                } else {
                    // Yalnızca benden silindiğinde mesajı listeden tamamen kaldır
                    this.messages.forEach(msgGroup => {
                        if (msgGroup && msgGroup.messages) {
                            msgGroup.messages = msgGroup.messages.filter(x => x.id != msgid);
                        }
                    });
                    this.messages = this.messages.filter(g => g.messages && g.messages.length > 0);
                }
            });

            this.connection.onreconnecting((error) => {
                this.updateConnectionStatus('Bağlantı koptu, yeniden bağlanılıyor...');
                console.warn('SignalR reconnecting:', error);
            });

            this.connection.onreconnected((connectionId) => {
                this.updateConnectionStatus('Bağlantı başarıyla yeniden kuruldu.', 3000);
                console.log('SignalR reconnected:', connectionId);
            });

            this.connection.onclose((error) => {
                this.updateConnectionStatus('Bağlantı tamamen kesildi. Lütfen sayfayı yenileyin.', 10000);
                console.error('SignalR closed:', error);
            });

            this.connection.start()
                .then(() => {
                    this.updateConnectionStatus('Bağlantı başarıyla kuruldu.', 3000);
                    console.log('SignalR connected');
                })
                .catch(err => {
                    console.error('SignalR connection error: ', err);
                    this.updateConnectionStatus('Bağlantı kurulamadı.', 5000);
                });
        },
        selectUserById(id) {
            if (!id) return;
            const targetUser = this.users.find(u => u.id.toString() === id.toString());
            if (targetUser) {
                this.selectUser(targetUser.id, targetUser.userName);
            }
        },
        showNotification(message, senderId, name, type) {
            if ("Notification" in window && Notification.permission === "granted") {
                let iconUrl = this.getSenderAvatar(senderId);
                const senderDisplayName = name || "Bilinmeyen";
                if (!iconUrl || iconUrl.includes('randomuser.me') || iconUrl.startsWith('data:image/svg')) {
                    iconUrl = `https://ui-avatars.com/api/?name=${encodeURIComponent(senderDisplayName)}&background=4f46e5&color=fff&rounded=true&size=192`;
                }
                const notification = new Notification("Yeni Mesaj", {
                    body: "Gönderen: " + senderDisplayName + "\nMesaj: " + (message || ""),
                    icon: iconUrl,
                    data: { senderId: senderId }
                });
                notification.onclick = (event) => {
                    if (type === "message") {
                        window.focus();
                        this.selectUser(senderId, name);
                        notification.close();
                    }
                };
            }
        },
        isPageVisible() {
            return !document.hidden;
        },
        attemptReconnect() {
            if (this.connection && this.connection.state === signalR.HubConnectionState.Disconnected) {
                this.connection.start()
                    .then(() => this.updateConnectionStatus('Bağlantı yeniden kuruldu.', 3000))
                    .catch(err => console.error('Manuel reconnect hatası:', err));
            }
        },
        updateConnectionStatus(status, time) {
            this.statusText = status;
            setTimeout(() => this.statusText = '', time ? time : 5000);
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
        setNotMessageCount(id, add) {
            const index = this.users.findIndex(user => user.id.toString() === id.toString());
            const updatedUsers = [...this.users]; // Derin kopya oluşturuyoruz
            updatedUsers[index] = {
                ...updatedUsers[index],
                notSeenMessagesCount: add ? updatedUsers[index].notSeenMessagesCount + 1 : 0
            };
            this.users = this.sortUsersByLastMessage(updatedUsers);
        },
        sortUsersByLastMessage(updatedUsers) {
            if (!updatedUsers || !Array.isArray(updatedUsers)) return [];
            return [...updatedUsers].sort((a, b) => {
                // 1. Sabitlenen sohbetler en üstte
                if (a.isPinned && !b.isPinned) return -1;
                if (!a.isPinned && b.isPinned) return 1;

                // 2. Son mesaj zamanına göre azalan sıralama
                const aTimestamp = a.lastMessage ? new Date(a.lastMessage.timestamp).getTime() : 0;
                const bTimestamp = b.lastMessage ? new Date(b.lastMessage.timestamp).getTime() : 0;
            
                return bTimestamp - aTimestamp;
            });
        },
        onMessageInputKeydown(event) {
            if (this.showSlashMenu && this.filteredSlashCommands.length > 0) {
                if (event.key === 'ArrowDown') {
                    event.preventDefault();
                    this.slashSelectedIndex = (this.slashSelectedIndex + 1) % this.filteredSlashCommands.length;
                    return;
                }
                if (event.key === 'ArrowUp') {
                    event.preventDefault();
                    this.slashSelectedIndex = (this.slashSelectedIndex - 1 + this.filteredSlashCommands.length) % this.filteredSlashCommands.length;
                    return;
                }
                if (event.key === 'Tab') {
                    event.preventDefault();
                    const selected = this.filteredSlashCommands[this.slashSelectedIndex];
                    if (selected) this.selectSlashCommand(selected, false);
                    return;
                }
                if (event.key === 'Enter') {
                    const selected = this.filteredSlashCommands[this.slashSelectedIndex];
                    const trimmed = (this.messageText || '').trim();
                    // Eğer kullanıcı tam komutu yazmadıysa (ör. "/z" veya sadece "/") listedeki seçili komutu uygula
                    if (selected && trimmed.toLowerCase() !== selected.cmd.toLowerCase() && !trimmed.includes(' ')) {
                        event.preventDefault();
                        this.selectSlashCommand(selected, !selected.needsArg);
                        return;
                    }
                }
                if (event.key === 'Escape') {
                    event.preventDefault();
                    this.showSlashMenu = false;
                    return;
                }
            }
            if (event.key === 'Enter') {
                event.preventDefault();
                this.handleFooterSend();
            }
        },
        selectSlashCommand(cmdObj, executeNow = false) {
            if (!cmdObj) return;
            this.showSlashMenu = false;
            if (cmdObj.needsArg && !executeNow) {
                this.messageText = cmdObj.cmd + ' ';
                this.$nextTick(() => {
                    const input = document.getElementById('message');
                    if (input) input.focus();
                });
            } else {
                this.messageText = cmdObj.cmd;
                this.sendMessage();
            }
        },
        async handleFooterSend() {
            if (this.editingMessage) {
                await this.commitEdit();
            } else {
                await this.sendMessage();
            }
        },
        async sendMessage() {
            if (!this.messageText || !this.messageText.trim()) return;
            const text = this.messageText.trim();
            const lower = text.toLowerCase();
            this.showSlashMenu = false;

            // Özel aksiyon komutlarını doğrudan ilgili Hub metoduna yönlendir
            if (lower === '/xox') {
                this.messageText = '';
                await this.startXoxGame();
                return;
            }
            if (lower === '/tkm' || lower === '/taskagitmakas') {
                this.messageText = '';
                await this.startRpsGame();
                return;
            }
            if (lower === '/durt' || lower === '/dürt') {
                this.messageText = '';
                await this.sendBuzz();
                return;
            }

            this.messageText = '';
            await this.addMessage(text);
        },
        async addMessage(messageText, type = 0, fileUrl) {
            if (this.connection && messageText) {
                try {
                    const replyId = this.replyingToMessage ? this.replyingToMessage.id : null;
                    const replySummary = this.replyingToMessage ? { ...this.replyingToMessage } : null;
                    this.replyingToMessage = null; // Yanıt durumunu temizle

                    let expiresAt = null;
                    if (this.selectedSelfDestructSeconds > 0) {
                        expiresAt = new Date(Date.now() + this.selectedSelfDestructSeconds * 1000).toISOString();
                    }

                    // Kutlama kelimelerinde konfeti tetikle
                    const celebrationWords = ['tebrik', 'kutlu', 'helal', 'bravo', 'şampiyon', '🎉', 'seviyorum', 'harika'];
                    if (celebrationWords.some(w => messageText.toLowerCase().includes(w))) {
                        this.triggerConfetti();
                    }

                    var msg = {
                        SenderId: parseInt(this.myuser.sub),
                        ReceiverId: parseInt(this.selectedUserId),
                        Content: messageText,
                        Timestamp: new Date(),
                        Status: 0,
                        Type: type,
                        FileUrl: fileUrl ? fileUrl : null,
                        ReplyToMessageId: replyId,
                        ExpiresAt: expiresAt
                    };
                    var item = {
                        "id": 0,
                        "senderId": msg.SenderId,
                        "receiverId": msg.ReceiverId,
                        "content": msg.Content,
                        "timestamp": msg.Timestamp,
                        "status": msg.Status,
                        "type": msg.Type,
                        "fileUrl": msg.FileUrl,
                        "metadataJson": null,
                        "replyToMessageId": replyId,
                        "replyToMessage": replySummary,
                        "reactions": {},
                        "editedAt": null,
                        "expiresAt": expiresAt,
                        "isPlaying": false,
                        "audioProgress": 0,
                        "audioCurrentTime": 0,
                        "audioDuration": 0,
                        "showReactionPicker": false,
                        "isHovered": false
                    };
                    let reactiveItem = null;
                    if (this.messages && this.messages.length && 
                        this.messages[this.messages.length - 1].senderId == msg.SenderId &&
                        msg.Type != 6 &&
                        !this.messages[this.messages.length - 1].messages.some(m => m.type == 6)) {
                        const groupMsgs = this.messages[this.messages.length - 1].messages;
                        groupMsgs.push(item);
                        reactiveItem = groupMsgs[groupMsgs.length - 1];
                    } else {
                        this.messages.push({
                            senderId: msg.SenderId,
                            messages: [item]
                        });
                        const lastGrp = this.messages[this.messages.length - 1];
                        reactiveItem = lastGrp.messages[lastGrp.messages.length - 1];
                    }
                    this.setLastMessage(this.selectedUserId, msg.Content);
                    this.scrollToBottom(true);

                    const savedMsg = await this.connection.invoke('SendMessage', msg);
                    if (savedMsg && reactiveItem) {
                        if (savedMsg.id || savedMsg.Id) {
                            reactiveItem.id = savedMsg.id || savedMsg.Id;
                        }
                        if (savedMsg.content || savedMsg.Content) {
                            reactiveItem.content = savedMsg.content || savedMsg.Content;
                            this.setLastMessage(this.selectedUserId, reactiveItem.content);
                        }
                        if (savedMsg.type !== undefined || savedMsg.Type !== undefined) {
                            reactiveItem.type = savedMsg.type !== undefined ? savedMsg.type : savedMsg.Type;
                        }
                        if (savedMsg.metadataJson || savedMsg.MetadataJson) {
                            reactiveItem.metadataJson = savedMsg.metadataJson || savedMsg.MetadataJson;
                        }
                    }
                    this.scrollToBottom(true);
                } catch (err) {
                    console.error('SendMessage error: ', err);
                }
            } else {
                this.updateConnectionStatus('Bağlantı kurulmadı. Lütfen internet bağlantınızı kontrol edin.', 5000);
            }
        },
        formatMessage(text) {
            if (!text) return '';
            // 1. XSS'e karşı HTML escape
            const map = {
                '&': '&amp;',
                '<': '&lt;',
                '>': '&gt;',
                '"': '&quot;',
                "'": '&#039;'
            };
            let safe = String(text).replace(/[&<>"']/g, m => map[m]);

            // 2. Metin emoji kısayollarını UTF-8 emojilere dönüştür
            const emojiShortcodes = [
                { code: ':-)', emoji: '😊' },
                { code: ':)', emoji: '😊' },
                { code: ':-D', emoji: '😃' },
                { code: ':D', emoji: '😃' },
                { code: ';-)', emoji: '😉' },
                { code: ';)', emoji: '😉' },
                { code: ':-(', emoji: '😞' },
                { code: ':(', emoji: '😞' },
                { code: ':-P', emoji: '😛' },
                { code: ':P', emoji: '😛' },
                { code: ':p', emoji: '😛' },
                { code: '<3', emoji: '❤️' },
                { code: ':fire:', emoji: '🔥' },
                { code: ':thumbsup:', emoji: '👍' },
                { code: ':clap:', emoji: '👏' },
                { code: ':smile:', emoji: '😄' },
                { code: ':heart:', emoji: '❤️' }
            ];
            for (const item of emojiShortcodes) {
                safe = safe.split(item.code).join(item.emoji);
            }

            // 3. Tıklanabilir güvenli linklere dönüştür (sadece http:// ve https://)
            const urlRegex = /(https?:\/\/[^\s<]+)/gi;
            safe = safe.replace(urlRegex, (url) => {
                return `<a href="${url}" target="_blank" rel="noopener noreferrer">${url}</a>`;
            });

            // 4. Phase 5: Spoiler / Dokun-Aç Metni (||gizli metin||)
            safe = safe.replace(/\|\|([\s\S]*?)\|\|/g, '<span class="spoiler-text" onclick="this.classList.toggle(\'revealed\')">$1</span>');

            return safe;
        },
        toggleConversationArea() {
            if (this.$refs.conversationArea) {
                this.$refs.conversationArea.classList.toggle('open');
            }
        },
        // =====================================================================
        // Phase 2: Avatar Helpers & Profile Management
        // =====================================================================
        getInitialsAvatar(nameOrId) {
            const str = String(nameOrId || '?').trim();
            const initial = (str.charAt(0) || '?').toUpperCase();
            let hash = 0;
            for (let i = 0; i < str.length; i++) {
                hash = str.charCodeAt(i) + ((hash << 5) - hash);
            }
            const colors = ['#4f46e5', '#0891b2', '#059669', '#d97706', '#dc2626', '#7c3aed', '#db2777', '#2563eb'];
            const color = colors[Math.abs(hash) % colors.length];
            const svg = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100">
                <rect width="100" height="100" fill="${color}" rx="50"/>
                <text x="50" y="63" font-family="-apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif" font-size="44" font-weight="700" fill="#ffffff" text-anchor="middle">${initial}</text>
            </svg>`;
            return `data:image/svg+xml;utf8,${encodeURIComponent(svg)}`;
        },
        getAvatar(user, fallbackName) {
            if (!user) return this.getInitialsAvatar(fallbackName || '?');
            const url = typeof user === 'string' ? user : (user.avatarUrl || user.picture);
            if (url && url.length > 0 && !url.includes('randomuser.me')) {
                if (url.startsWith('http://') || url.startsWith('https://') || url.startsWith('data:')) {
                    return url;
                }
                return '/api/MinioFile/download/' + url;
            }
            const name = (typeof user === 'object' ? (user.userName || user.username || user.name) : null) || fallbackName || (typeof user === 'object' ? user.id : null);
            return this.getInitialsAvatar(name);
        },
        getSenderAvatar(senderId) {
            if (!senderId) return this.getInitialsAvatar('?');
            if (this.myuser && (this.myuser.sub == senderId || this.myuser.id == senderId)) {
                return this.getAvatar(this.myuser);
            }
            const user = this.users.find(u => u.id == senderId);
            if (user) return this.getAvatar(user);
            return this.getInitialsAvatar(senderId);
        },
        async loadMyProfile() {
            try {
                const response = await fetch('/api/Users/me', {
                    headers: {
                        'Authorization': `Bearer ${this.token}`
                    }
                });
                if (response.ok) {
                    const result = await response.json();
                    if (result && result.data) {
                        this.myuser = {
                            ...this.myuser,
                            ...result.data,
                            sub: result.data.id.toString(),
                            name: result.data.username
                        };
                        this.newStatusText = result.data.statusMessage || '';
                    }
                }
            } catch (err) {
                console.error('Profil yükleme hatası:', err);
            }
        },
        openSettingsModal() {
            this.newStatusText = this.myuser.statusMessage || '';
            this.showSettingsModal = true;
        },
        closeSettingsModal() {
            this.showSettingsModal = false;
        },
        triggerAvatarUpload() {
            if (this.$refs.avatarFileInput) {
                this.$refs.avatarFileInput.click();
            }
        },
        cropAndResizeImage(file, targetWidth = 512, targetHeight = 512) {
            return new Promise((resolve, reject) => {
                const reader = new FileReader();
                reader.onload = (e) => {
                    const img = new Image();
                    img.onload = () => {
                        const canvas = document.createElement('canvas');
                        canvas.width = targetWidth;
                        canvas.height = targetHeight;
                        const ctx = canvas.getContext('2d');

                        // Ortalanmış kare kırpma
                        const minSide = Math.min(img.width, img.height);
                        const sx = (img.width - minSide) / 2;
                        const sy = (img.height - minSide) / 2;

                        ctx.drawImage(img, sx, sy, minSide, minSide, 0, 0, targetWidth, targetHeight);

                        canvas.toBlob((blob) => {
                            if (blob) {
                                resolve(blob);
                            } else {
                                reject(new Error('Canvas toBlob başarısız oldu'));
                            }
                        }, 'image/webp', 0.85);
                    };
                    img.onerror = reject;
                    img.src = e.target.result;
                };
                reader.onerror = reject;
                reader.readAsDataURL(file);
            });
        },
        async handleAvatarFile(event) {
            const file = event.target.files && event.target.files[0];
            if (!file) return;

            if (!file.type.startsWith('image/')) {
                Swal.fire('Hata', 'Lütfen geçerli bir resim dosyası seçin.', 'error');
                return;
            }

            if (file.size > 5 * 1024 * 1024) {
                Swal.fire('Hata', 'Profil resmi 5MB\'dan büyük olamaz.', 'error');
                return;
            }

            this.isUploadingAvatar = true;
            try {
                const croppedBlob = await this.cropAndResizeImage(file, 512, 512);
                const formData = new FormData();
                formData.append('file', croppedBlob, 'avatar.webp');

                const response = await fetch('/api/Users/me/avatar', {
                    method: 'POST',
                    headers: {
                        'Authorization': `Bearer ${this.token}`
                    },
                    body: formData
                });

                const resData = await response.json();
                if (response.ok && resData.data && resData.data.avatarUrl) {
                    this.myuser.avatarUrl = resData.data.avatarUrl;
                    Swal.fire({
                        toast: true,
                        position: 'top-end',
                        icon: 'success',
                        title: 'Profil resmi güncellendi',
                        showConfirmButton: false,
                        timer: 2000
                    });
                    this.loadUsers();
                } else {
                    Swal.fire('Hata', (resData.message || 'Resim yüklenemedi.'), 'error');
                }
            } catch (err) {
                console.error('Avatar yükleme hatası:', err);
                Swal.fire('Hata', 'Profil resmi yüklenirken bir sorun oluştu.', 'error');
            } finally {
                this.isUploadingAvatar = false;
                if (this.$refs.avatarFileInput) {
                    this.$refs.avatarFileInput.value = '';
                }
            }
        },
        async removeAvatar() {
            const result = await Swal.fire({
                title: 'Profil Resmini Kaldır',
                text: 'Mevcut profil resminizi silmek istediğinizden emin misiniz?',
                icon: 'warning',
                showCancelButton: true,
                confirmButtonText: 'Evet, Kaldır',
                cancelButtonText: 'Vazgeç'
            });

            if (!result.isConfirmed) return;

            try {
                const response = await fetch('/api/Users/me/avatar', {
                    method: 'DELETE',
                    headers: {
                        'Authorization': `Bearer ${this.token}`
                    }
                });
                if (response.ok) {
                    this.myuser.avatarUrl = null;
                    Swal.fire({
                        toast: true,
                        position: 'top-end',
                        icon: 'success',
                        title: 'Profil resmi kaldırıldı',
                        showConfirmButton: false,
                        timer: 2000
                    });
                    this.loadUsers();
                } else {
                    Swal.fire('Hata', 'Profil resmi kaldırılamadı.', 'error');
                }
            } catch (err) {
                console.error('Avatar silme hatası:', err);
            }
        },
        async saveStatusMessage() {
            this.isSavingStatus = true;
            try {
                const response = await fetch('/api/Users/me', {
                    method: 'PUT',
                    headers: {
                        'Authorization': `Bearer ${this.token}`,
                        'Content-Type': 'application/json'
                    },
                    body: JSON.stringify({ statusMessage: this.newStatusText })
                });
                if (response.ok) {
                    this.myuser.statusMessage = this.newStatusText;
                    Swal.fire({
                        toast: true,
                        position: 'top-end',
                        icon: 'success',
                        title: 'Durum mesajı kaydedildi',
                        showConfirmButton: false,
                        timer: 2000
                    });
                } else {
                    Swal.fire('Hata', 'Durum güncellenemedi.', 'error');
                }
            } catch (err) {
                console.error('Durum kaydetme hatası:', err);
            } finally {
                this.isSavingStatus = false;
            }
        },

        // =====================================================================
        // Phase 2: Friend Search & Requests Management
        // =====================================================================
        openSearchModal() {
            this.showSearchModal = true;
            this.searchActiveTab = 'search';
            this.searchQuery = '';
            this.searchResults = [];
            this.loadFriendRequests();
            this.$nextTick(() => {
                if (this.$refs.searchInput) {
                    this.$refs.searchInput.focus();
                }
            });
        },
        openSearchModalWithTab(tab) {
            this.openSearchModal();
            this.searchActiveTab = tab;
            if (tab === 'requests') {
                this.loadFriendRequests();
            }
        },
        switchToRequestsTab() {
            this.searchActiveTab = 'requests';
            this.loadFriendRequests();
        },
        closeSearchModal() {
            this.showSearchModal = false;
        },
        onSearchInput() {
            if (this.searchDebounceTimer) {
                clearTimeout(this.searchDebounceTimer);
            }
            this.searchDebounceTimer = setTimeout(() => {
                this.doSearch();
            }, 300);
        },
        async doSearch() {
            const q = (this.searchQuery || '').trim();
            if (!q) {
                this.searchResults = [];
                return;
            }
            this.isSearching = true;
            try {
                const response = await fetch('/api/Users/search?q=' + encodeURIComponent(q), {
                    headers: {
                        'Authorization': `Bearer ${this.token}`
                    }
                });
                if (response.ok) {
                    const res = await response.json();
                    this.searchResults = res.data || [];
                }
            } catch (err) {
                console.error('Arama hatası:', err);
            } finally {
                this.isSearching = false;
            }
        },
        async sendFriendRequest(targetUserId, username) {
            try {
                const response = await fetch('/api/FriendRequest', {
                    method: 'POST',
                    headers: {
                        'Authorization': `Bearer ${this.token}`,
                        'Content-Type': 'application/json'
                    },
                    body: JSON.stringify({
                        receiverId: targetUserId
                    })
                });
                const data = await response.json();
                if (response.ok && data && (data.id || data.Id)) {
                    const found = this.searchResults.find(u => u.id === targetUserId);
                    if (found) {
                        found.hasPendingOutgoing = true;
                        found.pendingRequestId = data.id || data.Id;
                    }
                    this.loadFriendRequests();
                    Swal.fire({
                        toast: true,
                        position: 'top-end',
                        icon: 'success',
                        title: `${username || 'Kullanıcı'} kişisine arkadaşlık isteği gönderildi`,
                        showConfirmButton: false,
                        timer: 2000
                    });
                } else {
                    Swal.fire('Bilgi', (data.message || 'İstek gönderilemedi.'), 'info');
                }
            } catch (err) {
                console.error('Arkadaşlık isteği hatası:', err);
            }
        },
        async cancelFriendRequest(requestId, targetUser) {
            if (!requestId) return;
            try {
                const response = await fetch('/api/FriendRequest/cancel/' + requestId, {
                    method: 'POST',
                    headers: {
                        'Authorization': `Bearer ${this.token}`
                    }
                });
                if (response.ok) {
                    if (targetUser) {
                        targetUser.hasPendingOutgoing = false;
                        targetUser.pendingRequestId = null;
                    } else {
                        const found = this.searchResults.find(u => u.pendingRequestId === requestId);
                        if (found) {
                            found.hasPendingOutgoing = false;
                            found.pendingRequestId = null;
                        }
                    }
                    Swal.fire({
                        toast: true,
                        position: 'top-end',
                        icon: 'info',
                        title: 'İstek iptal edildi',
                        showConfirmButton: false,
                        timer: 2000
                    });
                    this.loadFriendRequests();
                }
            } catch (err) {
                console.error('İstek iptal hatası:', err);
            }
        },
        async acceptFriendRequest(requestId) {
            if (!requestId) return;
            try {
                const response = await fetch('/api/FriendRequest/AcceptAndRejectFriendRequest/' + requestId + '/1', {
                    method: 'PUT',
                    headers: {
                        'Authorization': `Bearer ${this.token}`
                    }
                });
                if (response.ok) {
                    Swal.fire({
                        toast: true,
                        position: 'top-end',
                        icon: 'success',
                        title: 'Arkadaşlık isteği kabul edildi!',
                        showConfirmButton: false,
                        timer: 2000
                    });
                    const found = this.searchResults.find(u => u.pendingRequestId === requestId);
                    if (found) {
                        found.isFriend = true;
                        found.hasPendingIncoming = false;
                    }
                    this.loadUsers();
                    this.loadFriendRequests();
                }
            } catch (err) {
                console.error('İstek kabul hatası:', err);
            }
        },
        async rejectFriendRequest(requestId) {
            if (!requestId) return;
            try {
                const response = await fetch('/api/FriendRequest/AcceptAndRejectFriendRequest/' + requestId + '/2', {
                    method: 'PUT',
                    headers: {
                        'Authorization': `Bearer ${this.token}`
                    }
                });
                if (response.ok) {
                    const found = this.searchResults.find(u => u.pendingRequestId === requestId);
                    if (found) {
                        found.hasPendingIncoming = false;
                    }
                    this.loadFriendRequests();
                }
            } catch (err) {
                console.error('İstek red hatası:', err);
            }
        },
        async togglePin(user) {
            if (!user) return;
            const newPinned = !user.isPinned;
            try {
                const response = await fetch('/api/Friend/settings/' + user.id, {
                    method: 'PATCH',
                    headers: {
                        'Authorization': `Bearer ${this.token}`,
                        'Content-Type': 'application/json'
                    },
                    body: JSON.stringify({ isPinned: newPinned })
                });
                if (response.ok) {
                    user.isPinned = newPinned;
                    this.users = this.sortUsersByLastMessage(this.users);
                    Swal.fire({
                        toast: true,
                        position: 'top-end',
                        icon: 'info',
                        title: newPinned ? 'Sohbet başa sabitlendi' : 'Sabitleme kaldırıldı',
                        showConfirmButton: false,
                        timer: 1500
                    });
                }
            } catch (err) {
                console.error('Sabitleme hatası:', err);
            }
        },
        sendFriendRequestBtn() {
            this.openSearchModal();
        },
        openFriendRequests() {
            this.openSearchModalWithTab('requests');
        },
        openSettings() {
            this.openSettingsModal();
        },
        // =====================================================================
        // Phase 3: Modern UI & PWA Helpers
        // =====================================================================
        closeChatMobile() {
            this.selectedUserId = null;
            this.selectedUserName = null;
            this.isPeerTyping = false;
        },
        isUserOnline(userId) {
            if (!userId) return false;
            return this.onlineUsers_data.includes(userId.toString());
        },
        shouldShowDateBadge(groupIndex) {
            if (groupIndex === 0) return true;
            const currGroup = this.messages[groupIndex];
            const prevGroup = this.messages[groupIndex - 1];
            if (!currGroup || !currGroup.messages || !currGroup.messages.length) return false;
            if (!prevGroup || !prevGroup.messages || !prevGroup.messages.length) return true;
            return new Date(currGroup.messages[0].timestamp).toDateString() !== new Date(prevGroup.messages[0].timestamp).toDateString();
        },
        getDateBadge(isoDateString) {
            if (!isoDateString) return '';
            const date = new Date(isoDateString);
            const today = new Date();
            const yesterday = new Date();
            yesterday.setDate(today.getDate() - 1);

            if (date.toDateString() === today.toDateString()) return 'Bugün';
            if (date.toDateString() === yesterday.toDateString()) return 'Dün';

            const turkishMonths = ['Ocak', 'Şubat', 'Mart', 'Nisan', 'Mayıs', 'Haziran', 'Temmuz', 'Ağustos', 'Eylül', 'Ekim', 'Kasım', 'Aralık'];
            return `${date.getDate()} ${turkishMonths[date.getMonth()]} ${date.getFullYear()}`;
        },
        formatTime(isoDateString) {
            if (!isoDateString) return '';
            const date = new Date(isoDateString);
            return date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
        },
        scrollToBottom(smooth = true) {
            const doScroll = () => {
                if (this.$refs.msgDiv) {
                    this.$refs.msgDiv.scrollTo({
                        top: this.$refs.msgDiv.scrollHeight,
                        behavior: smooth ? 'smooth' : 'auto'
                    });
                    this.showScrollBottomBtn = false;
                }
            };
            this.$nextTick(() => {
                doScroll();
                setTimeout(doScroll, 80);
                setTimeout(doScroll, 220);
            });
        },
        installPWA() {
            if (this.deferredPrompt) {
                this.deferredPrompt.prompt();
                this.deferredPrompt.userChoice.then((choiceResult) => {
                    if (choiceResult.outcome === 'accepted') {
                        console.log('PWA başarıyla kuruldu');
                    }
                    this.deferredPrompt = null;
                    this.canInstallPWA = false;
                });
            }
        },
        openMediaPreview(imgUrl) {
            Swal.fire({
                imageUrl: '/api/MinioFile/download/' + imgUrl,
                imageAlt: 'Medya Önizleme',
                showConfirmButton: false,
                showCloseButton: true,
                background: this.darkMode ? '#1e293b' : '#ffffff'
            });
        },
        selectUser(receiverId,name) {
            if(receiverId == this.selectedUserId ) return;
            page = 1;
            this.endMessageVariable = false;
            this.messages = [];
            this.selectedUserId = receiverId;
            this.selectedUserName = name;
            this.isPeerTyping = false;
            isScrollingEnabled = false;
            this.loadMessages(this.myuser.sub, receiverId);
            if (this.$refs.conversationArea) {
                this.$refs.conversationArea.classList.remove('open');
            }
            this.sendSeen(receiverId,2);
            this.setNotMessageCount(receiverId, false);
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
                this.users.forEach(user => {
                    if(user.notSeenMessagesCount > 0)this.sendSeen(user.id,1);
                })
            } else {
                if(response.status == 401) {
                    this.logout();
                }
                const error = await response.json();
                alert('Failed to load users: ' + (error.message || 'Unknown error'));
            }
        },
        async  loadMessages(senderId, receiverId) {
            // if(this.cachesMessages[receiverId]) {
            //     this.renderMessage(this.cachesMessages[receiverId],receiverId);
            //     return
            // }
            if(this.endMessageVariable) return
            const response = await fetch('/api/message/GetMyMessages/' + receiverId + '/'+ page +'/' + pageSize, {
                method: 'GET',
                headers: {
                    'Authorization': `Bearer ${this.token}`,
                    'Content-Type': 'application/json'
                }
            });
            if (response.ok) {
                var messages = await response.json()
                var arr = [];
                messages.forEach(msg => {
                    msg.reactions = msg.reactions || {};
                    msg.isPlaying = false;
                    msg.audioProgress = 0;
                    msg.audioCurrentTime = 0;
                    msg.audioDuration = 0;
                    msg.showReactionPicker = false;
                    msg.isHovered = false;
                    if (msg.type == 6 || 
                        arr.length === 0 || 
                        msg.senderId != arr[arr.length - 1]?.senderId || 
                        arr[arr.length - 1].messages.some(m => m.type == 6)) {
                        arr.push({
                            senderId: msg.senderId,
                            messages:[msg]
                        });
                    } else {
                        arr[arr.length - 1].messages.push(msg);
                    }
                })
                this.renderMessage(arr,receiverId);
            } else {
                const error = await response.json();
                alert('Failed to load users: ' + (error.message || 'Unknown error'));
            }
        },
        renderMessage(messages,receiverId) {
            const msgDiv2 = this.$refs.msgDiv;
            var sy = msgDiv2 ? msgDiv2.scrollHeight : 0;
            if(messages.length == 0 && page > 1) {
                this.endMessageVariable = true
            } 
            this.messages  =  messages.concat(this.messages)
            this.cachesMessages[receiverId] = this.messages;
            setTimeout(() => {
                const targetDiv = this.$refs.msgDiv;
                if (!targetDiv) return;
                if(page == 1) targetDiv.scrollTop = targetDiv.scrollHeight; 
                else  targetDiv.scrollTop = targetDiv.scrollHeight - sy;
                isScrollingEnabled = true;
                page++;
            }, 100);
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
        async  sendSeen (userId,messageStatus) {
            if(this.connection){
                if(!this.isPageVisible()) messageStatus = 1
                await this.connection.invoke('SendSeen', userId.toString(), messageStatus);
            }
        },
        async loadFriendRequests() {
            const myId = this.myuser && (this.myuser.sub || this.myuser.id) ? (this.myuser.sub || this.myuser.id) : 0;
            const headers = {
                'Authorization': `Bearer ${this.token}`,
                'Content-Type': 'application/json'
            };
            try {
                const [incomingRes, outgoingRes] = await Promise.all([
                    fetch('/api/FriendRequest/GetFriendRequestByReceiverId/' + myId, { method: 'GET', headers }),
                    fetch('/api/FriendRequest/GetFriendRequestBySenderId/' + myId, { method: 'GET', headers })
                ]);

                if (incomingRes.status === 401 || outgoingRes.status === 401) {
                    this.logout();
                    return;
                }

                if (incomingRes.ok) {
                    const incData = await incomingRes.json();
                    this.friendRequests = Array.isArray(incData) ? incData : [];
                }
                if (outgoingRes.ok) {
                    const outData = await outgoingRes.json();
                    this.sentFriendRequests = Array.isArray(outData) ? outData : [];
                }
            } catch (err) {
                console.error('Arkadaşlık istekleri yüklenemedi:', err);
            }
        },
        timeAgo(isoDateString) {
            const turkishMonths = [
                'Oca', // Ocak
                'Şub', // Şubat
                'Mar', // Mart
                'Nis', // Nisan
                'May', // Mayıs
                'Haz', // Haziran
                'Tem', // Temmuz
                'Ağu', // Ağustos
                'Eyl', // Eylül
                'Eki', // Ekim
                'Kas', // Kasım
                'Ara'  // Aralık
            ];
        
            // ISO 8601 tarih string'ini Date objesine dönüştürme
            const date = new Date(isoDateString);
        
            // Gün bilgisini alma
            const day = date.getDate();
        
            // Ay bilgisini Türkçe kısaltmalarla alma
            const month = turkishMonths[date.getMonth()];
        
            // Saat bilgisini alma ve iki haneli yapma
            const hours = date.getHours().toString().padStart(2, '0');
        
            // Dakika bilgisini alma ve iki haneli yapma
            const minutes = date.getMinutes().toString().padStart(2, '0');
        
            // İstenen formatta birleştirme
            return `${day} ${month} ${hours}:${minutes}`;
        },
        onChatScroll() {
            if (!this.$refs.msgDiv) return;
            if (this.$refs.msgDiv.scrollTop === 0 && isScrollingEnabled && this.messages.length > 0) {
                this.loadMessages(this.myuser.sub, this.selectedUserId);
            }
            const distFromBottom = this.$refs.msgDiv.scrollHeight - this.$refs.msgDiv.scrollTop - this.$refs.msgDiv.clientHeight;
            this.showScrollBottomBtn = distFromBottom > 150;
        },
        onEmojiClick(event) {
            if (event && event.detail && event.detail.unicode) {
                this.messageText += event.detail.unicode;
            }
        },
        toogleEmojiBox() {
            this.showEmojiPicker = !this.showEmojiPicker;
        },
        triggerFileSelect() {
            this.acceptType = '*';
            setTimeout(() => {
                if (this.$refs.fileInput) this.$refs.fileInput.click();
            }, 100);
        },
        triggerImageSelect() {
            this.acceptType = 'image/*'; 
            setTimeout(() => {
                if (this.$refs.fileInput) this.$refs.fileInput.click();
            }, 100);
        },
        handlePaste(event) {
            // Yapıştırılan içerikleri al
            const items = event.clipboardData.items;
            for (let i = 0; i < items.length; i++) {
              const item = items[i];
              // Eğer yapıştırılan bir resim dosyasıysa
              if (item.kind === 'file' && item.type.startsWith('image/')) {
                const file = item.getAsFile();
                // Dosyayı doğrudan handleFileChange'e gönder
                this.handleFileChange({ target: { files: [file] } });
              }
            }
        },
        onImageLoad() {
            const targetDiv = this.$refs.msgDiv;
            if (!targetDiv) return;
            const distFromBottom = targetDiv.scrollHeight - targetDiv.scrollTop - targetDiv.clientHeight;
            if (page <= 2 || distFromBottom < 450) {
                targetDiv.scrollTop = targetDiv.scrollHeight;
            }
        },
        downloadFile(fileUrl) {
            const link = document.createElement('a');
            link.href = '/api/MinioFile/download/' + fileUrl;
            link.download = fileUrl.split('/').pop();
            document.body.appendChild(link);
            link.click();
            document.body.removeChild(link);
        },
        async handleFileChange(event) {
            const file = event.target.files[0];
            if(file.size > 1024 * 1024 * 5) {
                alert('Dosya boyutu 5MB den uzun olamaz!');
                return
            }
            const formData = new FormData();
            formData.append('file', file);
            fetch('/api/MinioFile/upload', {
                method: 'POST',
                headers: {
                    'Authorization': `Bearer ${this.token}`,
                },
                body: formData,
            })
            .then((response) => {
                if (!response.ok) {
                    throw new Error('Dosya yükleme başarısız!');
                }
                return response.json();
            })
            .then((data) => {
                var type = this.acceptType == 'image/*' ? 1 : 2;
                this.addMessage(file.name, type, data.key);
                console.log('Dosya başarıyla yüklendi:', data);
            })
            .catch((error) => {

                console.error('Hata:', error);
            });
        },
        // =====================================================================
        // Phase 4: Core Chat Features
        // =====================================================================

        // 1. Reply / Quote
        startReply(msg) {
            this.replyingToMessage = {
                id: msg.id,
                senderId: msg.senderId,
                senderName: msg.senderId == this.myuser.sub ? 'Siz' : (this.selectedUserName || 'Kullanıcı'),
                content: msg.isDeleted ? 'Bu mesaj silindi' : (msg.content || (msg.type == 1 ? 'Fotoğraf' : (msg.type == 3 ? 'Sesli Mesaj' : 'Dosya'))),
                type: msg.type
            };
            this.editingMessage = null;
            this.$nextTick(() => {
                const input = document.getElementById('message');
                if (input) input.focus();
            });
        },
        cancelReply() {
            this.replyingToMessage = null;
        },
        scrollToMessage(targetId) {
            if (!targetId) return;
            const el = document.getElementById('msg-bubble-' + targetId);
            if (el) {
                el.scrollIntoView({ behavior: 'smooth', block: 'center' });
                el.classList.add('msg-highlight-flash');
                setTimeout(() => {
                    el.classList.remove('msg-highlight-flash');
                }, 2000);
            } else {
                Swal.fire({
                    toast: true,
                    position: 'top-end',
                    icon: 'info',
                    title: 'Alıntılanan mesaj eski iletiler arasında yer alıyor.',
                    showConfirmButton: false,
                    timer: 2000
                });
            }
        },

        // 2. Edit Message
        startEdit(msg) {
            if (msg.senderId != this.myuser.sub || msg.isDeleted || msg.type != 0) return;
            this.editingMessage = msg;
            this.replyingToMessage = null;
            this.messageText = msg.content;
            this.$nextTick(() => {
                const input = document.getElementById('message');
                if (input) input.focus();
            });
        },
        cancelEdit() {
            this.editingMessage = null;
            this.messageText = '';
        },
        async commitEdit() {
            if (!this.editingMessage || !this.messageText || !this.messageText.trim()) return;
            const targetId = this.editingMessage.id;
            const newContent = this.messageText.trim();
            try {
                if (this.connection) {
                    await this.connection.invoke('EditMessage', this.selectedUserId.toString(), targetId, newContent);
                }
                // Optimistic local update
                this.messages.forEach(group => {
                    if (group && group.messages) {
                        group.messages.forEach(m => {
                            if (m.id == targetId) {
                                m.content = newContent;
                                m.editedAt = new Date().toISOString();
                            }
                        });
                    }
                });
                this.cancelEdit();
            } catch (err) {
                console.error('Mesaj düzenleme hatası:', err);
                Swal.fire('Hata', 'Mesaj düzenlenemedi.', 'error');
            }
        },

        // 3. Reactions & Hover Bridge
        onMessageMouseEnter(msg) {
            if (!msg) return;
            if (msg._hoverTimer) {
                clearTimeout(msg._hoverTimer);
                msg._hoverTimer = null;
            }
            msg.isHovered = true;
        },
        onMessageMouseLeave(msg) {
            if (!msg) return;
            if (msg._hoverTimer) clearTimeout(msg._hoverTimer);
            msg._hoverTimer = setTimeout(() => {
                msg.isHovered = false;
                msg.showReactionPicker = false;
                msg._hoverTimer = null;
            }, 180);
        },
        toggleQuickReactPicker(msg) {
            if (msg._hoverTimer) {
                clearTimeout(msg._hoverTimer);
                msg._hoverTimer = null;
            }
            msg.isHovered = true;
            msg.showReactionPicker = !msg.showReactionPicker;
        },
        async toggleReaction(msg, emoji) {
            msg.showReactionPicker = false;
            if (!this.connection || !msg.id) return;

            if (!msg.reactions) msg.reactions = {};
            const mySub = this.myuser.sub.toString();
            if (msg.reactions[mySub] === emoji) {
                delete msg.reactions[mySub];
            } else {
                msg.reactions[mySub] = emoji;
            }
            msg.reactions = { ...msg.reactions };

            try {
                await this.connection.invoke('SendReaction', this.selectedUserId.toString(), msg.id, emoji);
            } catch (err) {
                console.error('Tepki gönderme hatası:', err);
            }
        },
        getReactionsList(msg) {
            if (!msg || !msg.reactions) return [];
            const mySub = this.myuser.sub ? this.myuser.sub.toString() : '';
            const map = {};
            for (const [userId, emoji] of Object.entries(msg.reactions)) {
                if (!emoji) continue;
                if (!map[emoji]) {
                    map[emoji] = { count: 0, hasMine: false, userIds: [] };
                }
                map[emoji].count++;
                map[emoji].userIds.push(userId);
                if (userId === mySub) {
                    map[emoji].hasMine = true;
                }
            }
            return Object.entries(map).map(([emoji, data]) => ({
                emoji,
                count: data.count,
                hasMine: data.hasMine,
                tooltip: (data.hasMine ? 'Siz' : '') + (data.count > 1 ? ` ve ${data.count - (data.hasMine ? 1 : 0)} kişi` : (data.hasMine ? '' : '1 kişi'))
            }));
        },

        // 4. Delete: "Benden Sil" vs "Herkesten Sil"
        promptDeleteMessage(msg) {
            const isSender = msg.senderId == this.myuser.sub;
            if (isSender) {
                Swal.fire({
                    title: 'Mesajı Sil',
                    text: 'Bu mesajı nasıl silmek istersiniz?',
                    icon: 'question',
                    showCancelButton: true,
                    showDenyButton: true,
                    confirmButtonText: 'Herkesten Sil',
                    denyButtonText: 'Benden Sil',
                    cancelButtonText: 'Vazgeç',
                    confirmButtonColor: '#ef4444',
                    denyButtonColor: '#64748b'
                }).then((result) => {
                    if (result.isConfirmed) {
                        this.deleteMessage(msg, true);
                    } else if (result.isDenied) {
                        this.deleteMessage(msg, false);
                    }
                });
            } else {
                Swal.fire({
                    title: 'Mesajı Sil',
                    text: 'Bu mesaj yalnızca sizin sohbetinizden silinecek.',
                    icon: 'question',
                    showCancelButton: true,
                    confirmButtonText: 'Benden Sil',
                    cancelButtonText: 'Vazgeç',
                    confirmButtonColor: '#ef4444'
                }).then((result) => {
                    if (result.isConfirmed) {
                        this.deleteMessage(msg, false);
                    }
                });
            }
        },
        async deleteMessage(msg, forEveryone = true) {
            if (!this.connection || !msg.id) return;
            try {
                await this.connection.invoke('DeleteMessage', this.selectedUserId.toString(), msg.id, forEveryone);
                if (forEveryone) {
                    msg.isDeleted = true;
                    msg.content = "Bu Mesaj Silindi";
                } else {
                    this.messages.forEach(group => {
                        if (group && group.messages) {
                            group.messages = group.messages.filter(m => m.id != msg.id);
                        }
                    });
                    this.messages = this.messages.filter(g => g.messages && g.messages.length > 0);
                }
            } catch (err) {
                console.error('Silme hatası:', err);
            }
        },

        // 5. Delete Chat / Clear Chat History
        async confirmDeleteChat(targetUser) {
            targetUser = targetUser || this.selectedUser;
            if (!targetUser) return;

            const res = await Swal.fire({
                title: 'Sohbeti Sil',
                html: `
                    <div style="font-size: 14px; margin-bottom: 12px; color: var(--body-color); line-height: 1.5;">
                        <b>${targetUser.userName}</b> ile olan tüm sohbet geçmişi ve iletiler silinecektir.
                    </div>
                    <div style="margin-top: 14px; text-align: left; padding: 10px 14px; background: var(--input-bg); border-radius: 10px; font-size: 13px; border: 1px solid var(--border-color);">
                        <label style="display: flex; align-items: center; gap: 8px; cursor: pointer; color: var(--body-color); user-select: none;">
                            <input type="checkbox" id="swal-remove-friend" style="width: 16px; height: 16px; cursor: pointer;">
                            <span>Bu kişiyi arkadaş listemden de kaldır</span>
                        </label>
                    </div>
                `,
                icon: 'warning',
                showCancelButton: true,
                confirmButtonColor: '#ef4444',
                cancelButtonColor: '#64748b',
                confirmButtonText: '<i class="fas fa-trash-alt"></i> Evet, Sohbeti Sil',
                cancelButtonText: 'Vazgeç',
                preConfirm: () => {
                    const cb = document.getElementById('swal-remove-friend');
                    return {
                        removeFriend: cb ? cb.checked : false
                    };
                }
            });

            if (res.isConfirmed) {
                const removeFriend = res.value?.removeFriend || false;
                try {
                    // 1. Clear chat messages on server
                    if (this.connection) {
                        await this.connection.invoke('ClearChat', targetUser.id.toString());
                    } else {
                        await fetch(`/api/Message/clear/${targetUser.id}`, {
                            method: 'DELETE',
                            headers: { 'Authorization': `Bearer ${this.token}` }
                        });
                    }

                    // 2. Clear local message cache
                    if (this.cachesMessages) {
                        this.cachesMessages[targetUser.id] = [];
                    }

                    // 3. If currently selected, clear active chat and close mobile view
                    if (this.selectedUserId === targetUser.id) {
                        this.messages = [];
                        this.selectedUserId = null;
                        this.selectedUserName = null;
                        this.closeChatMobile();
                    }

                    // 4. If removeFriend checked, delete friendship
                    if (removeFriend) {
                        await fetch(`/api/Friend/remove/${targetUser.id}`, {
                            method: 'DELETE',
                            headers: { 'Authorization': `Bearer ${this.token}` }
                        });
                        this.users = this.users.filter(u => u.id !== targetUser.id);
                    } else {
                        targetUser.lastMessage = null;
                        targetUser.notSeenMessagesCount = 0;
                        this.users = this.sortUsersByLastMessage(this.users);
                    }

                    Swal.fire({
                        toast: true,
                        position: 'top-end',
                        icon: 'success',
                        title: removeFriend ? 'Sohbet ve arkadaş silindi' : 'Sohbet başarıyla silindi',
                        showConfirmButton: false,
                        timer: 2000
                    });
                } catch (err) {
                    console.error('Sohbet silme hatası:', err);
                    Swal.fire({
                        icon: 'error',
                        title: 'Hata',
                        text: 'Sohbet silinirken bir sorun oluştu.'
                    });
                }
            }
        },

        async confirmClearChat() {
            await this.confirmDeleteChat(this.selectedUser);
        },

        // 6. In-Chat Message Search
        toggleInChatSearch() {
            this.showInChatSearch = !this.showInChatSearch;
            if (this.showInChatSearch) {
                this.$nextTick(() => {
                    if (this.$refs.inChatSearchInput) {
                        this.$refs.inChatSearchInput.focus();
                    }
                });
            } else {
                this.closeInChatSearch();
            }
        },
        closeInChatSearch() {
            this.showInChatSearch = false;
            this.inChatSearchQuery = '';
            this.searchMatches = [];
            this.searchMatchIndex = 0;
        },
        onInChatSearch() {
            const q = (this.inChatSearchQuery || '').trim().toLowerCase();
            if (!q) {
                this.searchMatches = [];
                this.searchMatchIndex = 0;
                return;
            }
            const matches = [];
            this.messages.forEach(group => {
                if (group && group.messages) {
                    group.messages.forEach(m => {
                        if (!m.isDeleted && m.content && m.content.toLowerCase().includes(q)) {
                            matches.push(m.id);
                        }
                    });
                }
            });
            this.searchMatches = matches;
            this.searchMatchIndex = matches.length > 0 ? 0 : -1;
            if (matches.length > 0) {
                this.scrollToMessage(matches[0]);
            }
        },
        goToPrevSearchMatch() {
            if (!this.searchMatches.length) return;
            this.searchMatchIndex = (this.searchMatchIndex - 1 + this.searchMatches.length) % this.searchMatches.length;
            this.scrollToMessage(this.searchMatches[this.searchMatchIndex]);
        },
        goToNextSearchMatch() {
            if (!this.searchMatches.length) return;
            this.searchMatchIndex = (this.searchMatchIndex + 1) % this.searchMatches.length;
            this.scrollToMessage(this.searchMatches[this.searchMatchIndex]);
        },

        // 7. Voice / Audio Message Recording & Player
        async startAudioRecording() {
            if (!navigator.mediaDevices || !navigator.mediaDevices.getUserMedia) {
                Swal.fire('Hata', 'Tarayıcınız ses kaydını desteklemiyor.', 'error');
                return;
            }
            try {
                const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
                this.audioChunks = [];
                this.mediaRecorder = new MediaRecorder(stream);
                this.mediaRecorder.ondataavailable = (e) => {
                    if (e.data && e.data.size > 0) {
                        this.audioChunks.push(e.data);
                    }
                };
                this.mediaRecorder.start();
                this.isRecordingAudio = true;
                this.audioRecordDuration = 0;
                this.audioRecordTimer = setInterval(() => {
                    this.audioRecordDuration++;
                }, 1000);
            } catch (err) {
                console.error('Mikrofon erişim hatası:', err);
                Swal.fire('Mikrofon İzni Gerekli', 'Sesli mesaj kaydedebilmek için mikrofon izni vermelisiniz.', 'warning');
            }
        },
        cancelAudioRecording() {
            if (this.mediaRecorder && this.mediaRecorder.state !== 'inactive') {
                this.mediaRecorder.stop();
                this.mediaRecorder.stream.getTracks().forEach(t => t.stop());
            }
            if (this.audioRecordTimer) clearInterval(this.audioRecordTimer);
            this.isRecordingAudio = false;
            this.audioRecordDuration = 0;
            this.audioChunks = [];
        },
        finishAndSendAudio() {
            if (!this.mediaRecorder) return;
            if (this.audioRecordTimer) clearInterval(this.audioRecordTimer);
            const mimeType = this.mediaRecorder.mimeType || 'audio/webm';

            this.mediaRecorder.onstop = async () => {
                const audioBlob = new Blob(this.audioChunks, { type: mimeType });
                this.mediaRecorder.stream.getTracks().forEach(t => t.stop());
                this.isRecordingAudio = false;
                this.audioRecordDuration = 0;
                this.audioChunks = [];

                if (audioBlob.size < 100) return;

                const formData = new FormData();
                const ext = mimeType.includes('ogg') ? 'ogg' : 'webm';
                formData.append('file', audioBlob, `voice_${Date.now()}.${ext}`);

                try {
                    const response = await fetch('/api/MinioFile/upload', {
                        method: 'POST',
                        headers: { 'Authorization': `Bearer ${this.token}` },
                        body: formData
                    });
                    if (response.ok) {
                        const data = await response.json();
                        this.addMessage('Sesli Mesaj', 3, data.key);
                    } else {
                        Swal.fire('Hata', 'Sesli mesaj yüklenemedi.', 'error');
                    }
                } catch (err) {
                    console.error('Ses yükleme hatası:', err);
                    Swal.fire('Hata', 'Sesli mesaj gönderilirken bir hata oluştu.', 'error');
                }
            };
            this.mediaRecorder.stop();
        },
        formatAudioTime(sec) {
            if (!sec || isNaN(sec)) return '0:00';
            const m = Math.floor(sec / 60);
            const s = Math.floor(sec % 60);
            return `${m}:${s.toString().padStart(2, '0')}`;
        },
        togglePlayAudio(msg) {
            const audioEl = document.getElementById('audio-player-' + msg.id);
            if (!audioEl) return;

            document.querySelectorAll('audio').forEach(a => {
                if (a !== audioEl && !a.paused) {
                    a.pause();
                }
            });
            this.messages.forEach(g => {
                if (g && g.messages) {
                    g.messages.forEach(m => {
                        if (m.id !== msg.id) m.isPlaying = false;
                    });
                }
            });

            if (audioEl.paused) {
                audioEl.play().catch(e => console.warn('Audio play prevented:', e));
                msg.isPlaying = true;
            } else {
                audioEl.pause();
                msg.isPlaying = false;
            }
        },
        onAudioLoaded(msg, event) {
            msg.audioDuration = event.target.duration;
        },
        onAudioTimeUpdate(msg, event) {
            msg.audioCurrentTime = event.target.currentTime;
            if (event.target.duration) {
                msg.audioProgress = (event.target.currentTime / event.target.duration) * 100;
            }
        },
        onAudioEnded(msg) {
            msg.isPlaying = false;
            msg.audioProgress = 0;
            msg.audioCurrentTime = 0;
        },
        seekAudio(msg, event) {
            const bar = event.currentTarget;
            const rect = bar.getBoundingClientRect();
            const percent = Math.max(0, Math.min(1, (event.clientX - rect.left) / rect.width));
            const audioEl = document.getElementById('audio-player-' + msg.id);
            if (audioEl && audioEl.duration) {
                audioEl.currentTime = percent * audioEl.duration;
                msg.audioProgress = percent * 100;
            }
        },

        // =====================================================================
        // Phase 5: Fun Features (Buzz, XOX Game, Self-Destruct, Confetti)
        // =====================================================================

        // 1. Titreşim / Dürtme (Buzz / Nudge)
        async sendBuzz() {
            if (this.buzzCooldown > 0 || !this.selectedUserId || !this.connection) return;

            try {
                this.buzzCooldown = 10;
                if (this.buzzCooldownTimer) clearInterval(this.buzzCooldownTimer);
                this.buzzCooldownTimer = setInterval(() => {
                    this.buzzCooldown--;
                    if (this.buzzCooldown <= 0) {
                        clearInterval(this.buzzCooldownTimer);
                    }
                }, 1000);

                await this.connection.invoke('SendBuzz', this.selectedUserId.toString());
            } catch (err) {
                console.error('SendBuzz error:', err);
                const msg = err && err.message ? err.message : 'Dürtme gönderilemedi.';
                Swal.fire('Bilgi', msg, 'info');
            }
        },

        playBuzzEffect(data) {
            // Titreşim (Mobil)
            if ('vibrate' in navigator) {
                try {
                    navigator.vibrate([200, 100, 200, 100, 250]);
                } catch (e) {
                    console.warn('Vibration failed:', e);
                }
            }

            // Ekran sallantısı
            const chatEl = document.getElementById('chat') || document.querySelector('.chat-area-main');
            if (chatEl) {
                chatEl.classList.remove('screen-shake');
                void chatEl.offsetWidth; // reflow
                chatEl.classList.add('screen-shake');
                setTimeout(() => {
                    chatEl.classList.remove('screen-shake');
                }, 700);
            }

            // Retro Nudge sesi
            this.playRetroNudgeSound();

            // Bildirim toast
            Swal.fire({
                toast: true,
                position: 'top-end',
                icon: 'warning',
                title: `⚡ ${data.senderName || 'Arkadaşınız'} sizi dürttü!`,
                showConfirmButton: false,
                timer: 2500
            });
        },

        playRetroNudgeSound() {
            try {
                const AudioCtx = window.AudioContext || window.webkitAudioContext;
                if (!AudioCtx) return;
                const ctx = new AudioCtx();
                const now = ctx.currentTime;

                const osc1 = ctx.createOscillator();
                const osc2 = ctx.createOscillator();
                const gain = ctx.createGain();

                osc1.type = 'sawtooth';
                osc2.type = 'triangle';

                osc1.frequency.setValueAtTime(220, now);
                osc1.frequency.exponentialRampToValueAtTime(880, now + 0.18);

                osc2.frequency.setValueAtTime(110, now);
                osc2.frequency.exponentialRampToValueAtTime(440, now + 0.18);

                gain.gain.setValueAtTime(0.25, now);
                gain.gain.exponentialRampToValueAtTime(0.001, now + 0.4);

                osc1.connect(gain);
                osc2.connect(gain);
                gain.connect(ctx.destination);

                osc1.start(now);
                osc2.start(now);
                osc1.stop(now + 0.4);
                osc2.stop(now + 0.4);
            } catch (e) {
                console.warn('Web Audio sintetizörü başlatılamadı:', e);
            }
        },

        // 2. XOX ve Taş-Kağıt-Makas Mini Oyunları
        getGameType(msg) {
            if (!msg || !msg.metadataJson) return 'xox';
            try {
                const state = typeof msg.metadataJson === 'object' ? msg.metadataJson : JSON.parse(msg.metadataJson);
                return (state && (state.gameType || state.GameType)) || 'xox';
            } catch {
                return 'xox';
            }
        },

        async startXoxGame() {
            if (!this.selectedUserId || !this.connection) return;
            try {
                await this.connection.invoke('StartXoxGame', this.selectedUserId.toString());
            } catch (err) {
                console.error('StartXoxGame error:', err);
                Swal.fire('Hata', 'Oyun başlatılamadı.', 'error');
            }
        },

        getXoxState(msg) {
            if (!msg || !msg.metadataJson) return null;
            try {
                return typeof msg.metadataJson === 'object' ? msg.metadataJson : JSON.parse(msg.metadataJson);
            } catch {
                return null;
            }
        },

        getXoxBoard(msg) {
            const state = this.getXoxState(msg);
            return (state && state.board) || (state && state.Board) || ["", "", "", "", "", "", "", "", ""];
        },

        getXoxStatusText(msg) {
            const state = this.getXoxState(msg);
            if (!state) return '';
            const status = state.status || state.Status;
            const winner = state.winner || state.Winner;
            const turn = state.turn || state.Turn;
            const playerXName = state.playerXName || state.PlayerXName || 'Oyuncu 1';
            const playerOName = state.playerOName || state.PlayerOName || 'Oyuncu 2';

            if (status === 'finished') {
                if (winner === 'Draw') return '🤝 Oyun Berabere Bitti!';
                const winnerName = winner === 'X' ? playerXName : playerOName;
                return `🏆 Kazanan: ${winnerName} (${winner})!`;
            }

            const currentTurnPlayer = turn === 'X' ? playerXName : playerOName;
            const isMyTurn = (turn === 'X' && (state.playerX || state.PlayerX) == this.myuser.sub) ||
                             (turn === 'O' && (state.playerO || state.PlayerO) == this.myuser.sub);

            return isMyTurn ? `🎮 Sıra Sende! (${turn})` : `⏳ Sıra: ${currentTurnPlayer} (${turn})`;
        },

        isWinningCell(msg, cIdx) {
            const state = this.getXoxState(msg);
            if (!state) return false;
            const winLine = state.winningLine || state.WinningLine;
            return Array.isArray(winLine) && winLine.includes(cIdx);
        },

        async makeXoxMove(msg, cellIndex) {
            if (!this.connection || !msg.id) return;
            const state = this.getXoxState(msg);
            if (!state || (state.status || state.Status) !== 'playing') return;

            const board = this.getXoxBoard(msg);
            if (board[cellIndex]) return; // Zaten dolu

            try {
                await this.connection.invoke('MakeXoxMove', msg.id, cellIndex);
            } catch (err) {
                const errMsg = err && err.message ? err.message : 'Hamle yapılamadı.';
                Swal.fire({
                    toast: true,
                    position: 'top-end',
                    icon: 'info',
                    title: errMsg,
                    showConfirmButton: false,
                    timer: 2000
                });
            }
        },

        // 2b. Taş - Kağıt - Makas (Best of 3) Mini Oyunu
        async startRpsGame() {
            if (!this.selectedUserId || !this.connection) return;
            try {
                await this.connection.invoke('StartRpsGame', this.selectedUserId.toString());
            } catch (err) {
                console.error('StartRpsGame error:', err);
                Swal.fire('Hata', 'Taş-Kağıt-Makas başlatılamadı.', 'error');
            }
        },

        getRpsState(msg) {
            const s = this.getXoxState(msg);
            if (!s) {
                return {
                    player1Id: 0,
                    player2Id: 0,
                    player1Name: 'Oyuncu 1',
                    player2Name: 'Oyuncu 2',
                    player1Choice: null,
                    player2Choice: null,
                    player1Score: 0,
                    player2Score: 0,
                    round: 1,
                    targetScore: 3,
                    lastRoundPlayer1Choice: null,
                    lastRoundPlayer2Choice: null,
                    lastRoundSummary: null,
                    winner: null,
                    status: 'playing'
                };
            }
            return {
                player1Id: s.player1Id ?? s.Player1Id ?? 0,
                player2Id: s.player2Id ?? s.Player2Id ?? 0,
                player1Name: s.player1Name || s.Player1Name || 'Oyuncu 1',
                player2Name: s.player2Name || s.Player2Name || 'Oyuncu 2',
                player1Choice: s.player1Choice ?? s.Player1Choice ?? null,
                player2Choice: s.player2Choice ?? s.Player2Choice ?? null,
                player1Score: s.player1Score ?? s.Player1Score ?? 0,
                player2Score: s.player2Score ?? s.Player2Score ?? 0,
                round: s.round ?? s.Round ?? 1,
                targetScore: s.targetScore ?? s.TargetScore ?? 3,
                lastRoundPlayer1Choice: s.lastRoundPlayer1Choice ?? s.LastRoundPlayer1Choice ?? null,
                lastRoundPlayer2Choice: s.lastRoundPlayer2Choice ?? s.LastRoundPlayer2Choice ?? null,
                lastRoundSummary: s.lastRoundSummary ?? s.LastRoundSummary ?? null,
                winner: s.winner ?? s.Winner ?? null,
                status: s.status || s.Status || 'playing'
            };
        },

        getRpsPlayer1Name(msg) {
            const state = this.getRpsState(msg);
            if (!state) return 'Oyuncu 1';
            const myId = parseInt(this.myuser.sub);
            return state.player1Id === myId ? 'Siz' : state.player1Name;
        },

        getRpsPlayer2Name(msg) {
            const state = this.getRpsState(msg);
            if (!state) return 'Oyuncu 2';
            const myId = parseInt(this.myuser.sub);
            return state.player2Id === myId ? 'Siz' : state.player2Name;
        },

        getRpsChoiceName(choice) {
            if (!choice) return '';
            const c = choice.toLowerCase();
            if (c === 'rock') return '🪨 Taş';
            if (c === 'paper') return '📄 Kağıt';
            if (c === 'scissors') return '✂️ Makas';
            return choice;
        },

        isRpsWinnerMe(msg) {
            const state = this.getRpsState(msg);
            if (!state || state.status !== 'finished') return false;
            const myId = parseInt(this.myuser.sub);
            if (state.winner === 'Draw') return false;
            if (state.player1Score > state.player2Score) return state.player1Id === myId;
            if (state.player2Score > state.player1Score) return state.player2Id === myId;
            return false;
        },

        getMyRpsChoice(msg) {
            const state = this.getRpsState(msg);
            if (!state) return null;
            const myId = parseInt(this.myuser.sub);
            if (state.player1Id === myId) return state.player1Choice;
            if (state.player2Id === myId) return state.player2Choice;
            return null;
        },

        getRpsStatusText(msg) {
            const state = this.getRpsState(msg);
            if (!state) return '';
            if (state.status === 'finished') {
                return `🏆 Kazanan: ${state.winner}! (${state.player1Score} - ${state.player2Score})`;
            }
            const myId = parseInt(this.myuser.sub);
            const isP1 = state.player1Id === myId;
            const myChoice = isP1 ? state.player1Choice : state.player2Choice;
            const peerChoice = isP1 ? state.player2Choice : state.player1Choice;
            const peerName = isP1 ? state.player2Name : state.player1Name;

            if (myChoice && !peerChoice) {
                return `✅ Seçiminiz kaydedildi! ${peerName} bekleniyor...`;
            }
            if (!myChoice && peerChoice) {
                return `🔥 ${peerName} hamlesini seçti! Sıra sizde!`;
            }
            return `3 olan kazanır! Hamlenizi seçin:`;
        },

        async makeRpsMove(msg, choice) {
            if (!this.connection || !msg.id) return;
            const state = this.getRpsState(msg);
            if (!state || state.status !== 'playing') return;
            if (this.getMyRpsChoice(msg)) return;

            try {
                await this.connection.invoke('MakeRpsMove', msg.id, choice);
            } catch (err) {
                const errMsg = err && err.message ? err.message : 'Hamle gönderilemedi.';
                Swal.fire({
                    toast: true,
                    position: 'top-end',
                    icon: 'info',
                    title: errMsg,
                    showConfirmButton: false,
                    timer: 2000
                });
            }
        },

        // 3. Kendini İmha Eden Mesajlar
        selectSelfDestruct(seconds) {
            this.selectedSelfDestructSeconds = seconds;
            this.showSelfDestructMenu = false;
        },

        getExpireRemaining(msg) {
            if (!msg.expiresAt) return '';
            const remainingMs = new Date(msg.expiresAt).getTime() - Date.now();
            if (remainingMs <= 0) return '0s';
            const sec = Math.ceil(remainingMs / 1000);
            return `${sec}s`;
        },

        checkExpiredMessages() {
            if (!this.messages || !this.messages.length) return;
            const now = Date.now();
            this.messages.forEach(group => {
                if (group && group.messages) {
                    group.messages.forEach(m => {
                        if (m.expiresAt && !m.isDeleted) {
                            const expTime = new Date(m.expiresAt).getTime();
                            if (expTime <= now) {
                                m.isDeleted = true;
                                m.content = "🔥 Bu mesajın süresi doldu.";
                                if (m.senderId == this.myuser.sub && this.connection) {
                                    this.connection.invoke('ExpireMessage', m.id).catch(e => console.warn(e));
                                }
                            }
                        }
                    });
                }
            });
        },

        // 4. Konfeti Efekti (Pure Vanilla JS Canvas)
        triggerConfetti() {
            const canvas = document.getElementById('confetti-canvas');
            if (!canvas) return;
            const ctx = canvas.getContext('2d');
            canvas.width = window.innerWidth;
            canvas.height = window.innerHeight;

            const particles = [];
            const colors = ['#3b82f6', '#ef4444', '#10b981', '#f59e0b', '#8b5cf6', '#ec4899', '#06b6d4'];
            for (let i = 0; i < 90; i++) {
                particles.push({
                    x: canvas.width / 2,
                    y: canvas.height * 0.4,
                    r: Math.random() * 6 + 4,
                    dx: (Math.random() - 0.5) * 16,
                    dy: (Math.random() - 0.8) * 18,
                    color: colors[Math.floor(Math.random() * colors.length)],
                    tilt: Math.random() * 10 - 5,
                    tiltAngle: 0,
                    tiltAngleInc: Math.random() * 0.1 + 0.05
                });
            }

            let animationFrameId;
            let frames = 0;
            const maxFrames = 120;

            const render = () => {
                ctx.clearRect(0, 0, canvas.width, canvas.height);
                frames++;
                particles.forEach((p) => {
                    p.x += p.dx;
                    p.y += p.dy;
                    p.dy += 0.35; // gravity
                    p.tiltAngle += p.tiltAngleInc;
                    p.tilt = Math.sin(p.tiltAngle) * 12;

                    ctx.beginPath();
                    ctx.lineWidth = p.r;
                    ctx.strokeStyle = p.color;
                    ctx.moveTo(p.x + p.tilt + p.r / 2, p.y);
                    ctx.lineTo(p.x + p.tilt, p.y + p.tilt + p.r / 2);
                    ctx.stroke();
                });

                if (frames < maxFrames) {
                    animationFrameId = requestAnimationFrame(render);
                } else {
                    ctx.clearRect(0, 0, canvas.width, canvas.height);
                    cancelAnimationFrame(animationFrameId);
                }
            };
            render();
        }

    }
}).mount('#projetApp')
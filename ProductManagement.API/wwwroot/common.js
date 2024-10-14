function timeAgo(timestamp) {
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

function parseJwt(token) {
    const base64Url = token.split('.')[1]; // Payload kısmı
    const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/'); // Base64 URL kodlamasını düzelt
    const jsonPayload = decodeURIComponent(escape(atob(base64))); // Base64'ten JSON'a dönüştür
    return JSON.parse(jsonPayload); // JSON'ı objeye dönüştür
}
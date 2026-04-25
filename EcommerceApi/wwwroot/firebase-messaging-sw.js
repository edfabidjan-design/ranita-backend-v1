/* wwwroot/firebase-messaging-sw.js */
importScripts("https://www.gstatic.com/firebasejs/10.12.2/firebase-app-compat.js");
importScripts("https://www.gstatic.com/firebasejs/10.12.2/firebase-messaging-compat.js");

firebase.initializeApp({
    apiKey: "AIzaSyAEZxh97yKJKY0KMHESGNa5kEUCl7adpvI",
    authDomain: "ranita-e7fee.firebaseapp.com",
    projectId: "ranita-e7fee",
    storageBucket: "ranita-e7fee.firebasestorage.app",
    messagingSenderId: "18866880283",
    appId: "1:18866880283:web:2462a474ef3810c7751dbe"
});

const messaging = firebase.messaging();

self.addEventListener("notificationclick", (event) => {
    event.notification.close();

    const link = event.notification?.data?.link || "/admin-orders.html";
    const url = new URL(link, self.location.origin).href;

    event.waitUntil(clients.openWindow(url));
});

messaging.onBackgroundMessage((payload) => {
    console.log("[SW] bg payload:", payload);

    // ✅ si FCM affiche déjà la notification native, on ne double pas
    if (payload?.notification) {
        return;
    }

    const title = payload?.data?.title || "Ranita";
    const body = payload?.data?.body || "Notification";
    const icon = payload?.data?.icon || "/assets/logo-192.png";

    return self.registration.showNotification(title, {
        body,
        icon,
        data: payload?.data || {}
    });
});
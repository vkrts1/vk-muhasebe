// Ermay Muhasebe Cloud - Service Worker
const CACHE_NAME = 'ermay-cache-v3.0.3'; // Version increment to force update
const assets = [
    '/',
    '/index.html',
    '/css/app.css',
    '/manifest.json'
];

self.addEventListener('install', event => {
    self.skipWaiting(); // Force activation
    event.waitUntil(
        caches.open(CACHE_NAME).then(cache => {
            return cache.addAll(assets);
        })
    );
});

self.addEventListener('activate', event => {
    // Delete old caches and take control immediately
    event.waitUntil(
        caches.keys().then(cacheNames => {
            return Promise.all(
                cacheNames.filter(name => name !== CACHE_NAME)
                    .map(name => caches.delete(name))
            );
        }).then(() => self.clients.claim())
    );
});

// Network-First strategy: Fetch from network first. If offline or fails, serve from cache.
self.addEventListener('fetch', event => {
    // Skip non-GET requests and external resources (e.g. firebase)
    if (event.request.method !== 'GET' || !event.request.url.startsWith(self.location.origin)) {
        return;
    }
    
    event.respondWith(
        fetch(event.request)
            .then(response => {
                // If valid response, update the cache
                if (response && response.status === 200 && response.type === 'basic') {
                    const responseToCache = response.clone();
                    caches.open(CACHE_NAME).then(cache => {
                        cache.put(event.request, responseToCache);
                    });
                }
                return response;
            })
            .catch(() => {
                // Network failed, try to serve from cache
                return caches.match(event.request);
            })
    );
});

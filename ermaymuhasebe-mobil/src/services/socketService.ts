let io: any = null;
try {
  io = require('socket.io-client').io;
} catch {
  io = null;
}

let socketInstance: any = null;

/**
 * Initializes real-time WebSocket connection to Avalonia Desktop / Cloud Sync Server.
 */
export function connectSyncSocket(serverUrl: string, onDataUpdate?: (event: string, data: any) => void): any {
  try {
    if (!io) return null;

    if (socketInstance) {
      socketInstance.disconnect();
    }

    socketInstance = io(serverUrl, {
      transports: ['websocket'],
      autoConnect: true,
    });

    socketInstance.on('connect', () => {
      console.log('Real-time Desktop sync socket connected');
    });

    socketInstance.on('sync_event', (payload: any) => {
      if (onDataUpdate) {
        onDataUpdate('sync_event', payload);
      }
    });

    return socketInstance;
  } catch (e) {
    console.error('Socket connection error:', e);
    return null;
  }
}

export function disconnectSyncSocket() {
  if (socketInstance) {
    socketInstance.disconnect();
    socketInstance = null;
  }
}

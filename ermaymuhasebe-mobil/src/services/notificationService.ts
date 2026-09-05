let Notifications: any = null;
try {
  Notifications = require('expo-notifications');
} catch {
  Notifications = null;
}

// Configure notification behavior
if (Notifications) {
  try {
    Notifications.setNotificationHandler({
      handleNotification: async () => ({
        shouldShowAlert: true,
        shouldPlaySound: true,
        shouldSetBadge: true,
      }),
    });
  } catch (e) {
    // Graceful fallback
  }
}

/**
 * Requests iOS notification permissions from the user.
 */
export async function requestNotificationPermissions(): Promise<boolean> {
  try {
    if (!Notifications) return false;

    const { status: existingStatus } = await Notifications.getPermissionsAsync();
    let finalStatus = existingStatus;

    if (existingStatus !== 'granted') {
      const { status } = await Notifications.requestPermissionsAsync();
      finalStatus = status;
    }

    return finalStatus === 'granted';
  } catch (e) {
    console.error('Notification permissions error:', e);
    return false;
  }
}

/**
 * Schedules an iOS notification for due invoices, receivables, or stock alerts.
 */
export async function sendLocalNotification(
  title: string,
  body: string,
  data?: Record<string, any>
): Promise<string | null> {
  try {
    if (!Notifications) return null;

    return await Notifications.scheduleNotificationAsync({
      content: {
        title,
        body,
        data: data || {},
        sound: 'default',
      },
      trigger: null, // Triggers immediately
    });
  } catch (e) {
    console.error('Send local notification error:', e);
    return null;
  }
}

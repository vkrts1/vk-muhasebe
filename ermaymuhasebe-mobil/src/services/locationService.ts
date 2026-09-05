let Location: any = null;
try {
  Location = require('expo-location');
} catch {
  Location = null;
}

export interface GpsLocationResult {
  latitude: number;
  longitude: number;
  address?: string;
}

/**
 * Captures current GPS coordinates and reverse-geocodes address for Cari/Invoice.
 */
export async function getCurrentGpsLocation(): Promise<GpsLocationResult | null> {
  try {
    if (!Location) return null;

    const { status } = await Location.requestForegroundPermissionsAsync();
    if (status !== 'granted') {
      return null;
    }

    const location = await Location.getCurrentPositionAsync({
      accuracy: Location.Accuracy.Balanced,
    });

    const coords = {
      latitude: location.coords.latitude,
      longitude: location.coords.longitude,
    };

    try {
      const addresses = await Location.reverseGeocodeAsync(coords);
      if (addresses && addresses.length > 0) {
        const addr = addresses[0];
        const formatted = [addr.street, addr.subregion, addr.city]
          .filter(Boolean)
          .join(', ');
        return { ...coords, address: formatted };
      }
    } catch {
      // Ignore geocode failure, return coords
    }

    return coords;
  } catch (e) {
    console.error('GPS location error:', e);
    return null;
  }
}

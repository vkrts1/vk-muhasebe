let Contacts: any = null;
try {
  Contacts = require('expo-contacts');
} catch {
  Contacts = null;
}

export interface ImportedContact {
  name: string;
  phone: string;
  email?: string;
}

/**
 * Prompts user to pick a contact from iOS phonebook to auto-fill Cari.
 */
export async function pickContactForCari(): Promise<ImportedContact | null> {
  try {
    if (!Contacts) return null;

    const { status } = await Contacts.requestPermissionsAsync();
    if (status !== 'granted') {
      return null;
    }

    const contact = await Contacts.presentContactPickerAsync();
    if (contact) {
      const phone = contact.phoneNumbers && contact.phoneNumbers.length > 0
        ? contact.phoneNumbers[0].number
        : '';
      const email = contact.emails && contact.emails.length > 0
        ? contact.emails[0].email
        : '';

      return {
        name: contact.name || `${contact.firstName || ''} ${contact.lastName || ''}`.trim(),
        phone: phone || '',
        email: email || '',
      };
    }
    return null;
  } catch (e) {
    console.error('Pick contact error:', e);
    return null;
  }
}

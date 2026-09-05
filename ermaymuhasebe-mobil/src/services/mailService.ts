let MailComposer: any = null;
try {
  MailComposer = require('expo-mail-composer');
} catch {
  MailComposer = null;
}

export interface MailInvoiceOptions {
  recipientEmail: string;
  subject: string;
  body: string;
  attachments?: string[]; // Array of file URIs
}

/**
 * Opens iOS native Mail Composer with e-Invoice PDF attachment.
 */
export async function sendInvoiceMail(options: MailInvoiceOptions): Promise<boolean> {
  try {
    if (!MailComposer) return false;

    const isAvailable = await MailComposer.isAvailableAsync();
    if (!isAvailable) return false;

    const result = await MailComposer.composeAsync({
      recipients: [options.recipientEmail],
      subject: options.subject,
      body: options.body,
      attachments: options.attachments || [],
    });

    return result.status === MailComposer.MailComposerStatus.SENT;
  } catch (e) {
    console.error('Send invoice mail error:', e);
    return false;
  }
}

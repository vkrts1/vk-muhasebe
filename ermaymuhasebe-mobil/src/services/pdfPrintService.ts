let Print: any = null;
let Sharing: any = null;

try {
  Print = require('expo-print');
} catch {
  Print = null;
}

try {
  Sharing = require('expo-sharing');
} catch {
  Sharing = null;
}

/**
 * AirPrint HTML content directly to iOS wireless printer or generate PDF.
 */
export async function printHtmlContent(htmlContent: string): Promise<boolean> {
  try {
    if (Print) {
      await Print.printAsync({
        html: htmlContent,
      });
      return true;
    }
    return false;
  } catch (e) {
    console.error('AirPrint error:', e);
    return false;
  }
}

/**
 * Generates PDF file from HTML and opens iOS Share Sheet (WhatsApp, Mail, iMessage).
 */
export async function sharePdfFromHtml(htmlContent: string, fileName: string = 'Fatura.pdf'): Promise<boolean> {
  try {
    if (Print) {
      const { uri } = await Print.printToFileAsync({
        html: htmlContent,
      });

      if (Sharing && (await Sharing.isAvailableAsync())) {
        await Sharing.shareAsync(uri, {
          UTI: '.pdf',
          mimeType: 'application/pdf',
          dialogTitle: `${fileName} Paylaş`,
        });
        return true;
      }
    }
    return false;
  } catch (e) {
    console.error('PDF Share error:', e);
    return false;
  }
}

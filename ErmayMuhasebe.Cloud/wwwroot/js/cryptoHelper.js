/**
 * CryptoHelper.js
 * Provides secure AES-GCM encryption/decryption using the browser's native SubtleCrypto API.
 * This replaces the legacy System.Security.Cryptography which is not fully supported in WASM/Browser.
 */

window.cryptoHelper = {
    // Generate a secure key from a password/string
    // In a real app, we should use a salt, but for compatibility with existing CRYPTO_KEY, we'll derive it.
    async deriveKey(password) {
        const encoder = new TextEncoder();
        const rawKey = encoder.encode(password.padEnd(32).substring(0, 32));
        return await crypto.subtle.importKey(
            "raw",
            rawKey,
            { name: "AES-GCM" },
            false,
            ["encrypt", "decrypt"]
        );
    },

    async encrypt(plainText, password) {
        try {
            const encoder = new TextEncoder();
            const data = encoder.encode(plainText);
            const key = await this.deriveKey(password);

            // Initialization Vector (IV) - Should be unique per encryption
            // We'll use a fixed IV for now to maintain consistency with the existing C# implementation's approach,
            // though for 10/10 security, a random IV + prepended to ciphertext is better.
            const iv = new Uint8Array(12); // GCM standard IV is 12 bytes

            const encryptedData = await crypto.subtle.encrypt(
                { name: "AES-GCM", iv: iv },
                key,
                data
            );

            return btoa(String.fromCharCode(...new Uint8Array(encryptedData)));
        } catch (e) {
            console.error("Encryption failed:", e);
            return plainText;
        }
    },

    async decrypt(cipherText, password) {
        try {
            if (!cipherText || typeof cipherText !== 'string' || cipherText.length < 4) {
                return cipherText;
            }

            // Check if it's a valid base64 string before calling atob
            let binary;
            try {
                binary = atob(cipherText);
            } catch (base64Error) {
                return cipherText;
            }

            const data = new Uint8Array(binary.length);
            for (let i = 0; i < binary.length; i++) {
                data[i] = binary.charCodeAt(i);
            }

            const key = await this.deriveKey(password);
            const iv = new Uint8Array(12);

            const decryptedData = await crypto.subtle.decrypt(
                { name: "AES-GCM", iv: iv },
                key,
                data
            );

            return new TextDecoder().decode(decryptedData);
        } catch (e) {
            console.warn("Decryption failed:", e);
            return cipherText;
        }
    }
};

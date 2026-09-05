const { onRequest } = require("firebase-functions/v2/https");
const { onValueCreated } = require("firebase-functions/v2/database");
const logger = require("firebase-functions/logger");
const admin = require("firebase-admin");
const axios = require("axios");

admin.initializeApp();

const TELEGRAM_BOT_TOKEN = process.env.TELEGRAM_BOT_TOKEN;

// 1. Triggered when a new security request is created on Realtime Database
exports.onSecurityRequestCreated = onValueCreated("/security_requests/{requestId}", async (event) => {
    const requestId = event.params.requestId;
    const requestData = event.data.val();

    if (!requestData) return;

    try {
        // Fetch user data
        const userSnapshot = await admin.database().ref(`/users/${requestData.uid}`).once("value");
        const userData = userSnapshot.val();

        if (!userData || !userData.telegram || !userData.telegram.chat_id) {
            logger.error(`User ${requestData.uid} has no telegram configuration.`);
            await admin.database().ref(`/security_requests/${requestId}`).update({
                status: "FAILED_NO_TELEGRAM"
            });
            return;
        }

        const chatId = userData.telegram.chat_id;

        // Telegram Inline Keyboard Message
        const messageText = `🔐 <b>Ermay Muhasebe - Güvenlik Onayı</b>\n\n` +
            `Hesabınız için şifre değiştirme talebi oluşturuldu.\n\n` +
            `<b>Kullanıcı:</b> ${requestData.uid}\n` +
            `<b>Tarih:</b> ${new Date(requestData.createdAt).toLocaleString("tr-TR")}\n\n` +
            `Bu işlemi siz mi yaptınız?`;

        const keyboard = {
            inline_keyboard: [
                [
                    { text: "✅ Evet, Onayla", callback_data: `approve:${requestId}` },
                    { text: "❌ Hayır, Reddet", callback_data: `reject:${requestId}` }
                ]
            ]
        };

        const response = await axios.post(`https://api.telegram.org/bot${TELEGRAM_BOT_TOKEN}/sendMessage`, {
            chat_id: chatId,
            text: messageText,
            parse_mode: "HTML",
            reply_markup: keyboard
        });

        // Store telegram message id to edit it later
        if (response.data && response.data.result) {
            const messageId = response.data.result.message_id;
            await admin.database().ref(`/security_requests/${requestId}/verification`).update({
                telegram_sent: true,
                telegram_message_id: messageId
            });
        }
    } catch (error) {
        logger.error("Error sending Telegram message:", error);
    }
});

// 2. Telegram Webhook Endpoint to capture Callback Queries (Button clicks)
exports.telegramWebhook = onRequest(async (req, res) => {
    const update = req.body;

    if (!update.callback_query) {
        res.status(200).send("OK");
        return;
    }

    try {
        const callbackQuery = update.callback_query;
        const data = callbackQuery.data; // Format: "approve:requestId" or "reject:requestId"
        const chatId = callbackQuery.message.chat.id;
        const messageId = callbackQuery.message.message_id;

        const [action, requestId] = data.split(":");

        const requestRef = admin.database().ref(`/security_requests/${requestId}`);
        const requestSnapshot = await requestRef.once("value");
        const requestData = requestSnapshot.val();

        if (!requestData) {
            await answerCallbackQuery(callbackQuery.id, "Talep bulunamadı.");
            res.status(200).send("OK");
            return;
        }

        if (requestData.status !== "PENDING") {
            await answerCallbackQuery(callbackQuery.id, "Bu işlem zaten sonuçlandırılmış.");
            res.status(200).send("OK");
            return;
        }

        // Check expiry (15 mins)
        if (Date.now() > new Date(requestData.expires_at).getTime()) {
            await requestRef.update({ status: "EXPIRED" });
            await editTelegramMessage(chatId, messageId, "❌ Bu doğrulama talebinin süresi dolmuş.");
            await answerCallbackQuery(callbackQuery.id, "Süre aşımı.");
            res.status(200).send("OK");
            return;
        }

        if (action === "approve") {
            // Approve the request
            await requestRef.update({ status: "APPROVED" });

            // Parse hash and salt from payload
            const [newHash, newSalt] = requestData.newValue.split(":");

            // Update user security values on Realtime DB (triggers revocation)
            const timestamp = new Date().toISOString();
            await admin.database().ref(`/users/${requestData.uid}/security`).update({
                last_password_change: timestamp,
                active_sessions_revoked_at: timestamp
            });

            await editTelegramMessage(chatId, messageId, "✅ İşlem onaylandı ve uygulandı. Cihazlarınızdaki oturumlar güvenlik amacıyla sonlandırıldı.");
            await answerCallbackQuery(callbackQuery.id, "İşlem başarıyla onaylandı.");
        } else if (action === "reject") {
            await requestRef.update({ status: "REJECTED" });
            await editTelegramMessage(chatId, messageId, "❌ İşlem sizin tarafınızdan reddedildi.");
            await answerCallbackQuery(callbackQuery.id, "İşlem reddedildi.");
        }

        res.status(200).send("OK");
    } catch (error) {
        logger.error("Telegram Webhook Error:", error);
        res.status(500).send("Internal Server Error");
    }
});

async function answerCallbackQuery(callbackQueryId, text) {
    await axios.post(`https://api.telegram.org/bot${TELEGRAM_BOT_TOKEN}/answerCallbackQuery`, {
        callback_query_id: callbackQueryId,
        text: text
    });
}

async function editTelegramMessage(chatId, messageId, newText) {
    await axios.post(`https://api.telegram.org/bot${TELEGRAM_BOT_TOKEN}/editMessageText`, {
        chat_id: chatId,
        message_id: messageId,
        text: newText,
        reply_markup: { inline_keyboard: [] }
    });
}

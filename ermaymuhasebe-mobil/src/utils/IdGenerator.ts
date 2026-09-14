let lastGeneratedId = 10000000;

/**
 * Mobil ortamda üretilen ID'ler 10,000,000 ile 1,999,999,999 aralığındaki güvenli havuzdan tahsis edilir.
 * Masaüstü SQLite veritabanı autoincrement olarak 1, 2, 3... sayılarından başladığı için,
 * mobil çevrimdışıyken veya masaüstüyle eşzamanlı yeni kayıt eklendiğinde ID ÇAKIŞMASI KESİNLİKLE ENGELLENİR.
 */
export const generateInt32Id = (): number => {
  const epochSec = Math.floor(Date.now() / 1000); // ~1,740,000,000
  let newId = epochSec;

  if (newId <= lastGeneratedId) {
    newId = lastGeneratedId + 1;
  }

  lastGeneratedId = newId;
  return newId;
};

export const generateMobileRecordId = (): number => {
  return generateInt32Id();
};

/**
 * Platformlar arası tam tekillik ve bulutta veri ezilmesini önlemek için GUID üretici.
 */
export const generateSyncGuid = (): string => {
  return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, (c) => {
    const r = (Math.random() * 16) | 0;
    const v = c === 'x' ? r : (r & 0x3) | 0x8;
    return v.toString(16);
  });
};

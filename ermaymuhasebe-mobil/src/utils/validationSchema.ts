import * as yup from 'yup';

// Resmi TCKN Algoritması Doğrulaması
export const validateTCKNAlgo = (tckn: string | null | undefined): boolean => {
  if (!tckn) return true;
  const clean = tckn.trim();
  if (clean.length !== 11 || !/^\d{11}$/.test(clean) || clean[0] === '0') return false;

  const digits = clean.split('').map(Number);
  const oddSum = digits[0] + digits[2] + digits[4] + digits[6] + digits[8];
  const evenSum = digits[1] + digits[3] + digits[5] + digits[7];
  
  const digit10 = (oddSum * 7 - evenSum) % 10;
  if (digit10 < 0 ? (digit10 + 10) !== digits[9] : digit10 !== digits[9]) return false;

  const sumFirst10 = digits.slice(0, 10).reduce((a, b) => a + b, 0);
  return sumFirst10 % 10 === digits[10];
};

// Resmi VKN Algoritması Doğrulaması
export const validateVKNAlgo = (vkn: string | null | undefined): boolean => {
  if (!vkn) return true;
  const clean = vkn.trim();
  if (clean.length !== 10 || !/^\d{10}$/.test(clean)) return false;

  const v = clean.split('').map(Number);
  const lastDigit = v[9];

  let sum = 0;
  for (let i = 0; i < 9; i++) {
    const digit = (v[i] + (9 - i)) % 10;
    let temp = (digit * Math.pow(2, 9 - i)) % 9;
    if (digit !== 0 && temp === 0) temp = 9;
    sum += temp;
  }

  const calculatedLastDigit = (10 - (sum % 10)) % 10;
  return calculatedLastDigit === lastDigit;
};

// Resmi TR IBAN Doğrulaması
export const validateIBANAlgo = (iban: string | null | undefined): boolean => {
  if (!iban) return true;
  const clean = iban.replace(/\s+/g, '').toUpperCase();
  if (!clean.startsWith('TR') || clean.length !== 26) return false;
  return /^\d{24}$/.test(clean.substring(2));
};

// TCKN Validator
export const tcknValidator = yup
  .string()
  .nullable()
  .transform((value, originalValue) => (originalValue === '' ? null : value))
  .test('is-valid-tckn', 'Geçersiz TC Kimlik Numarası', value => validateTCKNAlgo(value));

// VKN Validator
export const vknValidator = yup
  .string()
  .nullable()
  .transform((value, originalValue) => (originalValue === '' ? null : value))
  .test('is-valid-vkn', 'Geçersiz Vergi Kimlik Numarası', value => validateVKNAlgo(value));

// IBAN Validator
export const ibanValidator = yup
  .string()
  .nullable()
  .transform((value, originalValue) => (originalValue === '' ? null : value))
  .test('is-valid-iban', 'Geçersiz IBAN Formatı (TR ile başlayan 26 hane)', value => validateIBANAlgo(value));

// Common Cari Form Schema
export const cariSchema = yup.object().shape({
  unvan: yup.string().required('Cari Ünvan zorunludur.'),
  email: yup.string().email('Geçerli bir e-posta adresi giriniz.').nullable().transform((value, originalValue) => (originalValue === '' ? null : value)),
  tckn: tcknValidator,
  vkn: vknValidator,
  vergiDairesi: yup.string().nullable(),
  iban: ibanValidator,
});


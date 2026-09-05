namespace ErmayMuhasebe.Shared.Helpers;

public static class ValidationHelper
{
    /// <summary>
    /// TC Kimlik No doğrulama (11 haneli algoritma)
    /// </summary>
    public static bool ValidateTCKN(string tckn)
    {
        if (string.IsNullOrWhiteSpace(tckn) || tckn.Length != 11)
            return false;

        if (!long.TryParse(tckn, out _))
            return false;

        if (tckn[0] == '0')
            return false;

        int[] digits = tckn.Select(c => int.Parse(c.ToString())).ToArray();

        // İlk 10 hanenin toplamının birler basamağı 11. haneye eşit olmalı
        int sum = digits.Take(10).Sum();
        if (sum % 10 != digits[10])
            return false;

        // 1,3,5,7,9. hanelerin toplamının 7 katından 2,4,6,8. hanelerin toplamı çıkarıldığında
        // elde edilen sonucun birler basamağı 10. haneye eşit olmalı
        int oddSum = digits[0] + digits[2] + digits[4] + digits[6] + digits[8];
        int evenSum = digits[1] + digits[3] + digits[5] + digits[7];
        int check = (oddSum * 7 - evenSum) % 10;
        
        return check == digits[9];
    }

    /// <summary>
    /// Vergi Kimlik No doğrulama (10 haneli algoritma)
    /// </summary>
    public static bool ValidateVKN(string vkn)
    {
        if (string.IsNullOrWhiteSpace(vkn) || vkn.Length != 10)
            return false;

        if (!long.TryParse(vkn, out _))
            return false;

        int[] v = vkn.Select(c => int.Parse(c.ToString())).ToArray();
        int lastDigit = v[9];

        int[] temp = new int[9];
        for (int i = 0; i < 9; i++)
        {
            int digit = (v[i] + (9 - i)) % 10;
            temp[i] = (digit * (int)Math.Pow(2, 9 - i)) % 9;
            if (digit != 0 && temp[i] == 0)
                temp[i] = 9;
        }

        int sum = temp.Sum();
        int calculatedLastDigit = (10 - (sum % 10)) % 10;

        return calculatedLastDigit == lastDigit;
    }

    /// <summary>
    /// IBAN doğrulama (TR ile başlayan 26 haneli)
    /// </summary>
    public static bool ValidateIBAN(string iban)
    {
        if (string.IsNullOrWhiteSpace(iban))
            return false;

        iban = iban.Replace(" ", "").ToUpper();

        if (!iban.StartsWith("TR") || iban.Length != 26)
            return false;

        // Basit format kontrolü (gerçek mod-97 algoritması daha karmaşık)
        return iban.Substring(2).All(char.IsDigit);
    }
}

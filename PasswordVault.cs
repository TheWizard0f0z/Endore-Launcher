using System;
using System.Text;
using System.Linq;

namespace AktualizatorEME
{
    public static class PasswordVault
    {
        // Klucz dla plików .json (niezależny od klucza gry)
        private static readonly string ProfileSecret = "m9S9UrS36qX9u9V8";

        // --- DLA GRY (Klucz wyliczony DESKTOP-92) ---
        public static string ToClassicUOPassword(string plainPassword)
        {
            if (string.IsNullOrEmpty(plainPassword)) return "";
            byte[] cuoKey = { 0x44, 0x45, 0x53, 0x4B, 0x54, 0x4F, 0x50, 0x2D, 0x39, 0x32 }; 
            byte[] data = Encoding.UTF8.GetBytes(plainPassword);
            StringBuilder sb = new StringBuilder("1-");
            for (int i = 0; i < data.Length; i++)
            {
                byte encryptedByte = (byte)(data[i] ^ cuoKey[i % cuoKey.Length]);
                sb.Append(encryptedByte.ToString("X2"));
            }
            return sb.ToString();
        }

        // --- DLA PROFILI .JSON (Szyfrowanie/Deszyfrowanie) ---
        public static string Encrypt(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            byte[] data = Encoding.UTF8.GetBytes(text);
            byte[] key = Encoding.UTF8.GetBytes(ProfileSecret);
            byte[] result = new byte[data.Length];
            for (int i = 0; i < data.Length; i++)
                result[i] = (byte)(data[i] ^ key[i % key.Length]);
            
            return Convert.ToBase64String(result);
        }

        public static string Decrypt(string base64Text)
        {
            if (string.IsNullOrEmpty(base64Text)) return "";
            try {
                byte[] data = Convert.FromBase64String(base64Text);
                byte[] key = Encoding.UTF8.GetBytes(ProfileSecret);
                byte[] result = new byte[data.Length];
                for (int i = 0; i < data.Length; i++)
                    result[i] = (byte)(data[i] ^ key[i % key.Length]);
                
                return Encoding.UTF8.GetString(result);
            } catch { return ""; }
        }
    }
}
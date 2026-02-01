using System;
using System.Text;
using System.Linq;

namespace AktualizatorEME
{
    public static class PasswordVault
    {
        // Szyfrowanie DLA GRY (Format ClassicUO) - KLUCZ: DESKTOP-92
        public static string ToClassicUOPassword(string plainPassword)
        {
            if (string.IsNullOrEmpty(plainPassword)) return "";

            // Pełny wyliczony klucz: D E S K T O P - 9 2
            byte[] cuoKey = { 0x44, 0x45, 0x53, 0x4B, 0x54, 0x4F, 0x50, 0x2D, 0x39, 0x32 }; 
    
            byte[] data = Encoding.UTF8.GetBytes(plainPassword);
            StringBuilder sb = new StringBuilder("1-");

            for (int i = 0; i < data.Length; i++)
            {
                // Teraz modulo 10 sprawi, że klucz będzie pasował do hasła o maks. dł. 10 znaków
                byte encryptedByte = (byte)(data[i] ^ cuoKey[i % cuoKey.Length]);
                sb.Append(encryptedByte.ToString("X2"));
            }

            return sb.ToString();
        }

        // Metody pomocnicze (zwracają czysty tekst, żeby nie psuć reszty programu)
        public static string Encrypt(string password) => password;
        public static string Decrypt(string password) => password;
    }
}
using System;
using System.IO;
using System.Security.Cryptography;

namespace Mvp24Hours.Infrastructure.Helpers
{
    /// <summary>
    /// 
    /// </summary>
    public static class EncryptionHelper
    {
        public static string CreateKeyBase64()
        {
            return CreateKeyBase64(32);
        }

        public static string CreateKeyBase64(int keySizeInBytes)
        {
            using var rngCryptoServiceProvider = RandomNumberGenerator.Create();
            byte[] key = new byte[keySizeInBytes];
            rngCryptoServiceProvider.GetBytes(key);
            return Convert.ToBase64String(key);
        }

        public static string EncryptWithAes(string plainText, string keyBase64, out string vectorBase64)
        {
            using Aes aesAlgorithm = Aes.Create();
            aesAlgorithm.Key = Convert.FromBase64String(keyBase64);
            aesAlgorithm.GenerateIV();

            //set the parameters with out keyword
            vectorBase64 = Convert.ToBase64String(aesAlgorithm.IV);

            // Create encryptor object
            ICryptoTransform encryptor = aesAlgorithm.CreateEncryptor();

            byte[] encryptedData;

            //Encryption will be done in a memory stream through a CryptoStream object
            using MemoryStream ms = new();
            using CryptoStream cs = new(ms, encryptor, CryptoStreamMode.Write);
            using (StreamWriter sw = new(cs))
            {
                sw.Write(plainText);
            }
            encryptedData = ms.ToArray();

            return Convert.ToBase64String(encryptedData);
        }

        public static string DecryptWithAes(string cipherText, string keyBase64, string vectorBase64)
        {
            using Aes aesAlgorithm = Aes.Create();
            aesAlgorithm.Key = Convert.FromBase64String(keyBase64);
            aesAlgorithm.IV = Convert.FromBase64String(vectorBase64);

            // Create decryptor object
            ICryptoTransform decryptor = aesAlgorithm.CreateDecryptor();

            byte[] cipher = Convert.FromBase64String(cipherText);

            //Decryption will be done in a memory stream through a CryptoStream object
            using MemoryStream ms = new(cipher);
            using CryptoStream cs = new(ms, decryptor, CryptoStreamMode.Read);
            using StreamReader sr = new(cs);
            return sr.ReadToEnd();
        }
    }
}

using BioLicense_Portal.Application.Interfaces;
using System;
using System.Security.Cryptography;
using System.Text;

namespace BioLicense_Portal.Infrastructure.Security
{
    public class KeyGeneratorService : IKeyGeneratorService
    {
        public (string PublicKey, string PrivateKey) GenerateKeyPair()
        {
            using var rsa = RSA.Create(2048);
            var publicKey = Convert.ToBase64String(rsa.ExportRSAPublicKey());
            var privateKey = Convert.ToBase64String(rsa.ExportRSAPrivateKey());
            
            return (publicKey, privateKey);
        }

        public string EncryptPrivateKey(string privateKey, string passphrase)
        {
            var key = DeriveKey(passphrase);
            using var aes = Aes.Create();
            aes.Key = key;
            aes.GenerateIV();
            var iv = aes.IV;

            using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            var plainBytes = Encoding.UTF8.GetBytes(privateKey);
            var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

            // Combine IV and CipherText
            var result = new byte[iv.Length + cipherBytes.Length];
            Buffer.BlockCopy(iv, 0, result, 0, iv.Length);
            Buffer.BlockCopy(cipherBytes, 0, result, iv.Length, cipherBytes.Length);

            return Convert.ToBase64String(result);
        }

        public string DecryptPrivateKey(string encryptedPrivateKey, string passphrase)
        {
            var fullBytes = Convert.FromBase64String(encryptedPrivateKey);
            var key = DeriveKey(passphrase);

            using var aes = Aes.Create();
            aes.Key = key;
            var iv = new byte[aes.BlockSize / 8];
            var cipherBytes = new byte[fullBytes.Length - iv.Length];

            Buffer.BlockCopy(fullBytes, 0, iv, 0, iv.Length);
            Buffer.BlockCopy(fullBytes, iv.Length, cipherBytes, 0, cipherBytes.Length);

            aes.IV = iv;
            using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            var plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);

            return Encoding.UTF8.GetString(plainBytes);
        }

        private byte[] DeriveKey(string passphrase)
        {
            // Simple key derivation for internal use. 
            // In high security environments, use Argon2 or PBKDF2 with salt.
            using var sha256 = SHA256.Create();
            return sha256.ComputeHash(Encoding.UTF8.GetBytes(passphrase));
        }
    }
}

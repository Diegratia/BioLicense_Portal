using BioLicense_Portal.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using System;
using System.Security.Cryptography;
using System.Text;

namespace BioLicense_Portal.Infrastructure.Security
{
    public class KeyGeneratorService : IKeyGeneratorService
    {
        private readonly IConfiguration _configuration;
        private readonly string _masterSecret;

        public KeyGeneratorService(IConfiguration configuration)
        {
            _configuration = configuration;
            _masterSecret = _configuration["AppKeys:MasterSecret"] ?? throw new Exception("Master secret for key encryption is not configured.");
        }

        public (string PublicKey, string PrivateKey) GenerateKeyPair()
        {
            var keyGenerator = Standard.Licensing.Security.Cryptography.KeyGenerator.Create();
            var keyPair = keyGenerator.GenerateKeyPair();
            
            var rawPrivKey = keyPair.ToEncryptedPrivateKeyString(string.Empty);
            var finalEncryptedPriv = EncryptPrivateKeyInternal(rawPrivKey, _masterSecret);

            return (keyPair.ToPublicKeyString(), finalEncryptedPriv);
        }

        private string EncryptPrivateKeyInternal(string privateKey, string passphrase)
        {
            var key = DeriveKey(passphrase);
            using var aes = Aes.Create();
            aes.Key = key;
            aes.GenerateIV();
            var iv = aes.IV;

            using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            var plainBytes = Encoding.UTF8.GetBytes(privateKey);
            var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

            var result = new byte[iv.Length + cipherBytes.Length];
            Buffer.BlockCopy(iv, 0, result, 0, iv.Length);
            Buffer.BlockCopy(cipherBytes, 0, result, iv.Length, cipherBytes.Length);

            return Convert.ToBase64String(result);
        }

        public string DecryptPrivateKey(string encryptedPrivateKey)
        {
            var fullBytes = Convert.FromBase64String(encryptedPrivateKey);
            var key = DeriveKey(_masterSecret);

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

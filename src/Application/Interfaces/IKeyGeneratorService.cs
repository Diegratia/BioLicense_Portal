namespace BioLicense_Portal.Application.Interfaces
{
    public interface IKeyGeneratorService
    {
        (string PublicKey, string PrivateKey) GenerateKeyPair();
        string EncryptPrivateKey(string privateKey, string passphrase);
        string DecryptPrivateKey(string encryptedPrivateKey, string passphrase);
    }
}

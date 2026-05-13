namespace BioLicense_Portal.Application.Interfaces
{
    public interface IKeyGeneratorService
    {
        (string PublicKey, string PrivateKey) GenerateKeyPair();
        string DecryptPrivateKey(string encryptedPrivateKey);
    }
}

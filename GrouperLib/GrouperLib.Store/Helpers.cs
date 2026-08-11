using System.Security.Cryptography.X509Certificates;

namespace GrouperLib.Store;

internal static class Helpers
{
    public static X509Certificate2 GetCertificateFromFile(string fileName, string password)
    {
        return X509CertificateLoader.LoadPkcs12FromFile(fileName, password);
    }

    public static X509Certificate2 GetCertificateFromBase64String(string base64String, string password)
    {
        return X509CertificateLoader.LoadPkcs12(Convert.FromBase64String(base64String), password);
    }

    public static X509Certificate2 GetCertificateFromStore(string thumbprint, StoreLocation storeLocation)
    {

        using X509Store store = new(StoreName.My, storeLocation);
        store.Open(OpenFlags.OpenExistingOnly);
        X509Certificate2Collection certificates = store.Certificates.Find(X509FindType.FindByThumbprint, thumbprint, validOnly: false);
        if (certificates.Count > 0)
        {
            return certificates[0];
        }
        throw new ArgumentException($"No certificate found with thumbprint {thumbprint}");
    }

    public static char HexChar(int value)
    {
        value &= 0xF;
        value += 48;
        return (char)(value > 57 ? value + 7 : value);
    }
}
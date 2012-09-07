using Datavail.Delta.Infrastructure.Agent.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Datavail.Delta.Infrastructure.Agent.Tests.Ecryption_Tests
{
    [TestClass]
    public class WhenDecryptingData
    {
        [TestMethod]
        public void ThenOriginalDataIsReturned()
        {
            const string dataToEncrypt = "My Secret String";
            var crypto = new Encryption();
            var encrypted = crypto.EncryptToString(dataToEncrypt);

            Assert.AreNotEqual(dataToEncrypt, encrypted);

            var decrypted = crypto.DecryptString(encrypted);

            Assert.AreEqual(dataToEncrypt, decrypted);
        }
    }
}

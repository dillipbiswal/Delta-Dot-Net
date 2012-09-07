using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Datavail.Delta.Infrastructure.Agent.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Datavail.Delta.Infrastructure.Agent.Tests.Ecryption_Tests
{
    [TestClass]
    public class WhenEncryptingData
    {
        [TestMethod]
        public void ThenDataReturnedIsEncrypted()
        {
            const string dataToEncrypt = "My Secret String";
            var crypto = new Encryption();

            var encrypted = crypto.EncryptToString(dataToEncrypt);

            Assert.AreNotEqual(dataToEncrypt, encrypted);
        }
    }
}

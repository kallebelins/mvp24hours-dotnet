//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.Helpers;
using System.Threading.Tasks;
using Xunit;
using Xunit.Priority;

namespace Mvp24Hours.Patterns.Test
{
    /// <summary>
    /// 
    /// </summary>
    [TestCaseOrderer(PriorityOrderer.Name, PriorityOrderer.Name)]
    public class EncryptionTest
    {
        [Fact, Priority(1)]
        public void EncryptDecrypt()
        {
            // arrange
            string keyBase64 = EncryptionHelper.CreateKeyBase64();
            string text = "test-test";
            // act
            string textEncrypt = EncryptionHelper.EncryptWithAes(text, keyBase64, out string vectorBase64);
            string textDecrypt = EncryptionHelper.DecryptWithAes(textEncrypt, keyBase64, vectorBase64);
            // assert
            Assert.Equal(textDecrypt, text);
        }
    }
}

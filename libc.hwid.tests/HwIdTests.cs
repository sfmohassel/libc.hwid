using System;
using System.Security.Cryptography;
using Xunit;

namespace libc.hwid.tests
{
    public class HwIdTests
    {
        [Fact]
        public void Generate()
        {
            var hardwareId1 = HwId.Generate();
            Assert.True(hardwareId1.Length == 40);

            var hardwareId2 = HwId.Generate();
            Assert.True(hardwareId2.Length == 40);

            var hardwareIdMac = HwId.Generate(null, true);
            Assert.True(hardwareIdMac.Length == 40);

            Assert.True(hardwareId1.Equals(hardwareId2, StringComparison.Ordinal));
            Assert.False(hardwareId1.Equals(hardwareIdMac, StringComparison.Ordinal));

            var sha256 = SHA256.Create();

            var hardwareId1Sha256 = HwId.Generate(sha256);
            Assert.True(hardwareId1.Length == 64);

            var hardwareId2Sha256 = HwId.Generate(sha256);
            Assert.True(hardwareId1.Length == 64);

            var hardwareIdMacSha256 = HwId.Generate(sha256, true);
            Assert.True(hardwareId1.Length == 64);

            Assert.True(hardwareId1Sha256.Equals(hardwareId2Sha256, StringComparison.Ordinal));
            Assert.False(hardwareId1Sha256.Equals(hardwareIdMacSha256, StringComparison.Ordinal));
        }
    }
}
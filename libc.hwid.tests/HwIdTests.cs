using System;
using Xunit;
namespace libc.hwid.tests {
    public class HwIdTests {
        [Fact]
        public void Generate() {
            var hardwareId1 = HwId.Generate();
            Assert.True(hardwareId1.Length == 40);

            var hardwareId2 = HwId.Generate();
            Assert.True(hardwareId2.Length == 40);

            Assert.True(hardwareId1.Equals(hardwareId2, StringComparison.Ordinal));
        }
    }
}
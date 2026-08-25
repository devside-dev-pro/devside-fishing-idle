using Devside.FishingIdle.Core;
using NUnit.Framework;

namespace Devside.FishingIdle.Core.Tests
{
    public class NumbersTests
    {
        [TestCase(0, "0")]
        [TestCase(7, "7")]
        [TestCase(42.5, "42.5")]
        [TestCase(999, "999")]
        [TestCase(1_000, "1K")]
        [TestCase(1_234, "1.23K")]
        [TestCase(1_000_000, "1M")]
        [TestCase(2_500_000_000, "2.5B")]
        [TestCase(7.2e12, "7.2T")]
        [TestCase(-1_234, "-1.23K")]
        public void Format_UsesSuffixes(double value, string expected)
        {
            Assert.That(Numbers.Format(value), Is.EqualTo(expected));
        }

        [Test]
        public void Format_FallsBackToScientificBeyondKnownSuffixes()
        {
            Assert.That(Numbers.Format(1e40), Does.Contain("e"));
        }
    }
}

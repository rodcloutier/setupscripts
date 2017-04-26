using System;
using NUnit.Framework;

using CSLauncher.Deployer;

namespace CSLauncher.Deployer.Tests
{
    [TestFixture]
    public class ParseVersionTests
    {
        [Test]
        public void SemanticVersionCanBeParsed()
        {
            Assert.IsNotNull(Utils.ParseVersion("0.0.0"));
        }
    }
}


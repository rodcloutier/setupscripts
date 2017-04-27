using System;
using System.IO;
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

        [Test]
        public void InvalidVersionRaiseException()
        {
            Assert.Throws( typeof(InvalidDataException),
                delegate {
                    Utils.ParseVersion("invalid");
                });
        }
    }
}


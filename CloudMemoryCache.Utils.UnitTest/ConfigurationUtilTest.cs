using System;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudMemoryCache.Utils.UnitTest
{
    [TestClass]
    public class ConfigurationUtilTest
    {
        private ConfigurationUtil _target;

        [TestInitialize]
        public void TestInitialize()
        {
            _target = new ConfigurationUtil();
        }

        [TestMethod]
        public void ConfigurationUtil_EmptyKey_Return_DefaultValue()
        {
            var defaultValue = Guid.NewGuid().ToString();
            _target.TryReadAppSetting("", defaultValue).Should().Be(defaultValue);
        }

        [TestMethod]
        public void ConfigurationUtil_KeyNotExists_Return_DefaultValue()
        {
            var defaultValue = Guid.NewGuid().ToString();
            _target.TryReadAppSetting(Guid.NewGuid().ToString(), defaultValue).Should().Be(defaultValue);
        }
    }
}

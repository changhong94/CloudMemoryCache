using System;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudMemoryCache.Utils.UnitTest
{
    [TestClass]
    public class AccountUtilTest
    {
        [TestMethod]
        public void AccountUtil_IsValidEmail_True()
        {
            AccountUtil.IsValidEmail("abc@de.com").Should().BeTrue();
        }

        [TestMethod]
        public void AccountUtil_IsValidEmail_False()
        {
            AccountUtil.IsValidEmail(Guid.NewGuid().ToString()).Should().BeFalse();
        }
    }
}

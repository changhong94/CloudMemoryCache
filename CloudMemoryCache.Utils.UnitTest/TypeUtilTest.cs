using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudMemoryCache.Utils.UnitTest
{
    [TestClass]
    public class TypeUtilTest
    {
        [TestMethod]
        public void TypeUtil_IsValidEmail_True()
        {
            var defaultValue = Guid.NewGuid().ToString();
            TypeUtil.ReadValue(null, defaultValue).Should().Be(defaultValue);
        }

        [TestMethod]
        public void TypeUtil_Enum()
        {
            TypeUtil.ReadValue("InvariantCultureIgnoreCase", StringComparison.Ordinal).Should().Be(StringComparison.InvariantCultureIgnoreCase);
        }

        [TestMethod]
        public void TypeUtil_SameType()
        {
            var obj = new Dictionary<string, string>
            {
                {Guid.NewGuid().ToString(), Guid.NewGuid().ToString()}
            };
            TypeUtil.ReadValue(obj, new Dictionary<string, string>()).Should().BeSameAs(obj);
        }

        [TestMethod]
        public void TypeUtil_ConvertableType()
        {
            TypeUtil.ReadValue("10", 1).Should().Be(10);
        }

        [TestMethod]
        public void TypeUtil_UnconvertableType()
        {
            TypeUtil.ReadValue(new Dictionary<string, string>
            {
                {Guid.NewGuid().ToString(), Guid.NewGuid().ToString()}
            }, 1).Should().Be(1);
        }
    }
}

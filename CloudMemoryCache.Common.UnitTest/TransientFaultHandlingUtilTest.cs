using System;
using System.Threading.Tasks;
using CloudMemoryCache.Common.TransientFaultHandling;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudMemoryCache.Common.UnitTest
{
    [TestClass]
    public class TransientFaultHandlingUtilTest
    {
        [TestMethod]
        public void TransientFaultHandlingUtil_RetryAllException()
        {
            var executedTimes = 0;
            try
            {
                TransientFaultHandlingUtil.RetryAllException(() =>
                {
                    ++executedTimes;
                    throw new Exception();
                });
            }
            catch (Exception)
            {
            }
            executedTimes.Should().Be(4);
        }

        [TestMethod]
        public async Task TransientFaultHandlingUtil_RetryAllExceptionAsync()
        {
            var executedTimes = 0;
            try
            {
                await TransientFaultHandlingUtil.RetryAllExceptionAsync(() =>
                {
                    ++executedTimes;
                    throw new Exception();
                });
            }
            catch (Exception)
            {
            }
            executedTimes.Should().Be(4);
        }

        [TestMethod]
        public void TransientFaultHandlingUtil_RetryAllException_ReturnValue()
        {
            var executedTimes = 0;
            TransientFaultHandlingUtil.RetryAllException(() =>
            {
                if (++executedTimes == 2)
                {
                    return executedTimes;
                }
                throw new Exception();
            }).Should().Be(2);
        }

        [TestMethod]
        public async Task TransientFaultHandlingUtil_RetryAllExceptionAsync_ReturnValue()
        {
            var executedTimes = 0;
            (await TransientFaultHandlingUtil.RetryAllExceptionAsync(async () =>
            {
                return await Task.Factory.StartNew(() =>
                {
                    if (++executedTimes == 2)
                    {
                        return executedTimes;
                    }
                    throw new Exception();
                });
            })).Should().Be(2);
        }

        [TestMethod]
        public void TransientFaultHandlingUtil_SafeExecute()
        {
            var executedTimes = 0;
            TransientFaultHandlingUtil.SafeExecute(() =>
            {
                ++executedTimes;
                throw new Exception();
            }).IsSuccessful.Should().BeFalse();
            executedTimes.Should().Be(4);
        }

        [TestMethod]
        public async Task TransientFaultHandlingUtil_SafeExecuteAsync()
        {
            var executedTimes = 0;
            (await TransientFaultHandlingUtil.SafeExecuteAsync(() =>
            {
                ++executedTimes;
                throw new Exception();
            })).IsSuccessful.Should().BeFalse();
            executedTimes.Should().Be(4);
        }

        [TestMethod]
        public void TransientFaultHandlingUtil_SafeExecute_ReturnValue()
        {
            var executedTimes = 0;
            var operationResponse = TransientFaultHandlingUtil.SafeExecute(() =>
            {
                if (++executedTimes == 2)
                {
                    return executedTimes;
                }
                throw new Exception();
            });
            operationResponse.Value.Should().Be(2);
            operationResponse.IsSuccessful.Should().BeTrue();
            operationResponse.Message.Should().BeEmpty();
        }

        [TestMethod]
        public async Task TransientFaultHandlingUtil_SafeExecuteAsync_ReturnValue()
        {
            var executedTimes = 0;
            var operationResponse = await TransientFaultHandlingUtil.SafeExecuteAsync(async () =>
            {
                return await Task.Factory.StartNew(() =>
                {
                    if (++executedTimes == 2)
                    {
                        return executedTimes;
                    }
                    throw new Exception();
                });
            });
            operationResponse.Value.Should().Be(2);
            operationResponse.IsSuccessful.Should().BeTrue();
            operationResponse.Message.Should().BeEmpty();
        }

        [TestMethod]
        public void TransientFaultHandlingUtil_SafeExecute_ReturnValue_Failed()
        {
            var executedTimes = 0;
            var operationResponse = TransientFaultHandlingUtil.SafeExecute(() =>
            {
                if (++executedTimes == 10)
                {
                    return executedTimes;
                }
                throw new Exception();
            });
            operationResponse.Value.Should().Be(0);
            operationResponse.IsSuccessful.Should().BeFalse();
            operationResponse.Message.Should().Contain("System.Exception");
        }

        [TestMethod]
        public async Task TransientFaultHandlingUtil_SafeExecuteAsync_ReturnValue_Failed()
        {
            var executedTimes = 0;
            var operationResponse = await TransientFaultHandlingUtil.SafeExecuteAsync(async () =>
            {
                return await Task.Factory.StartNew(() =>
                {
                    if (++executedTimes == 20)
                    {
                        return executedTimes;
                    }
                    throw new Exception();
                });
            });
            operationResponse.Value.Should().Be(0);
            operationResponse.IsSuccessful.Should().BeFalse();
            operationResponse.Message.Should().Contain("System.Exception");
        }
    }
}

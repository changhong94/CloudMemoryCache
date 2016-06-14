using System;
using System.Threading.Tasks;
using AutoMoq;
using CloudMemoryCache.Common.TransientFaultHandling;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudMemoryCache.UnitTest
{
    [TestClass]
    public class TransientFaultHandlingUtilTest
    {
        private AutoMoqer _autoMoqer;
        private ITransientFaultHandler _target;

        [TestInitialize]
        public void TestInitialize()
        {
            _autoMoqer = new AutoMoqer();
            _target = _autoMoqer.Resolve<TransientFaultHandler>();
        }

        [TestMethod]
        public void TransientFaultHandlingUtil_SafeExecute_WithRetry_Failed_Once()
        {
            bool firstTime = true;
            int executedTimes = 0;
            _target.SafeExecute(() =>
            {
                ++executedTimes;
                if (firstTime)
                {
                    firstTime = false;
                    throw new Exception();
                }
            }).Value.Should().BeTrue();
            firstTime.Should().BeFalse();
            executedTimes.Should().Be(2);
        }

        [TestMethod]
        public async Task TransientFaultHandlingUtil_SafeExecute_WithRetry_Failed_Once_Async()
        {
            bool firstTime = true;
            int executedTimes = 0;
            (await _target.SafeExecuteAsync(async () =>
            {
                ++executedTimes;
                if (firstTime)
                {
                    firstTime = false;
                    throw new Exception();
                }
                await Task.Factory.StartNew(() => { });
            })).Value.Should().BeTrue();
            firstTime.Should().BeFalse();
            executedTimes.Should().Be(2);
        }

        [TestMethod]
        public void TransientFaultHandlingUtil_SafeExecute_Func_WithRetry_Failed_Once()
        {
            bool firstTime = true;
            int executedTimes = 0;
            _target.SafeExecute(() =>
            {
                ++executedTimes;
                if (firstTime)
                {
                    firstTime = false;
                    throw new Exception();
                }
                return executedTimes;
            }).Value.Should().Be(2);
            firstTime.Should().BeFalse();
        }

        [TestMethod]
        public async Task TransientFaultHandlingUtil_SafeExecute_Func_WithRetry_Failed_Once_Async()
        {
            bool firstTime = true;
            int executedTimes = 0;
            (await _target.SafeExecuteAsync(async () =>
            {
                ++executedTimes;
                if (firstTime)
                {
                    firstTime = false;
                    throw new Exception();
                }
                await Task.Factory.StartNew(() => { });
                return executedTimes;
            })).Value.Should().Be(2);
            firstTime.Should().BeFalse();
        }

        [TestMethod]
        public void TransientFaultHandlingUtil_SafeExecute_WithRetry_Failed()
        {
            int executedTimes = 0;
            _target.SafeExecute(() =>
            {
                ++executedTimes;
                throw new Exception();
            }).Value.Should().BeFalse();
            executedTimes.Should().Be(4);
        }

        [TestMethod]
        public async Task TransientFaultHandlingUtil_SafeExecute_WithRetry_Failed_Async()
        {
            int executedTimes = 0;
            (await _target.SafeExecuteAsync(async () =>
            {
                ++executedTimes;
                await Task.Factory.StartNew(() => { });
                throw new Exception();
            })).Value.Should().BeFalse();
            executedTimes.Should().Be(4);
        }

        [TestMethod]
        public void TransientFaultHandlingUtil_SafeExecute_Func_WithRetry_Failed()
        {
            int executedTimes = 0;
            bool shouldThrow = true;
            _target.SafeExecute(() =>
            {
                ++executedTimes;

                if (shouldThrow)
                {
                    throw new Exception();
                }
                else
                {
                    return executedTimes;
                }
            }).Value.Should().Be(0);
        }

        [TestMethod]
        public async Task TransientFaultHandlingUtil_SafeExecute_Func_WithRetry_Failed_Async()
        {
            int executedTimes = 0;
            bool shouldThrow = true;
            (await _target.SafeExecuteAsync(async () =>
            {
                ++executedTimes;
                await Task.Factory.StartNew(() => { });
                if (shouldThrow)
                {
                    throw new Exception();
                }
                else
                {
                    return executedTimes;
                }
            })).Value.Should().Be(0);
        }

        [TestMethod]
        public void TransientFaultHandlingUtil_SafeExecute_Failed_With_FailedAction()
        {
            int executedTimes = 0;
            int failedActionTimes = 0;
            _target.SafeExecute(() =>
            {
                ++executedTimes;
                throw new Exception();
            },
            () => ++failedActionTimes).Value.Should().BeFalse();
            executedTimes.Should().Be(4);
            failedActionTimes.Should().Be(1);
        }

        [TestMethod]
        public async Task TransientFaultHandlingUtil_SafeExecute_Failed_With_FailedAction_Async()
        {
            int executedTimes = 0;
            int failedActionTimes = 0;
            (await _target.SafeExecuteAsync(
                async () =>
                {
                    ++executedTimes;
                    await Task.Factory.StartNew(() => { });
                    throw new Exception();
                },
                async () =>
                {
                    ++failedActionTimes;
                    await Task.Factory.StartNew(() => { });
                })).Value.Should().BeFalse();
            executedTimes.Should().Be(4);
            failedActionTimes.Should().Be(1);
        }

        [TestMethod]
        public void TransientFaultHandlingUtil_SafeExecute_Func_Failed_With_FailedAction()
        {
            int executedTimes = 0;
            int failedActionTimes = 0;
            bool shouldThrow = true;
            _target.SafeExecute(() =>
            {
                ++executedTimes;

                if (shouldThrow)
                {
                    throw new Exception();
                }
                else
                {
                    return executedTimes;
                }
            },
            () => ++failedActionTimes).Value.Should().Be(0);
            failedActionTimes.Should().Be(1);
        }

        [TestMethod]
        public async Task TransientFaultHandlingUtil_SafeExecute_Func_Failed_With_FailedAction_Async()
        {
            int executedTimes = 0;
            int failedActionTimes = 0;
            bool shouldThrow = true;
            (await _target.SafeExecuteAsync(
                async () =>
                {
                    ++executedTimes;
                    await Task.Factory.StartNew(() => { });
                    if (shouldThrow)
                    {
                        throw new Exception();
                    }
                    else
                    {
                        return executedTimes;
                    }
                },
                async () =>
                {
                    ++failedActionTimes;
                    await Task.Factory.StartNew(() => { });
                })).Value.Should().Be(0);
            failedActionTimes.Should().Be(1);
        }

        [TestMethod]
        public void TransientFaultHandlingUtil_SafeExecute_WithoutRetry_Failed_Once()
        {
            bool firstTime = true;
            int executedTimes = 0;
            _target.SafeExecute(() =>
            {
                ++executedTimes;
                if (firstTime)
                {
                    firstTime = false;
                    throw new Exception();
                }
            }, needRetry: false).Value.Should().BeFalse();
            firstTime.Should().BeFalse();
            executedTimes.Should().Be(1);
        }

        [TestMethod]
        public async Task TransientFaultHandlingUtil_SafeExecute_WithoutRetry_Failed_Once_Async()
        {
            bool firstTime = true;
            int executedTimes = 0;
            (await _target.SafeExecuteAsync(async () =>
            {
                ++executedTimes;
                if (firstTime)
                {
                    firstTime = false;
                    throw new Exception();
                }
                await Task.Factory.StartNew(() => { });
            }, needRetry: false)).Value.Should().BeFalse();
            firstTime.Should().BeFalse();
            executedTimes.Should().Be(1);
        }

        [TestMethod]
        public void TransientFaultHandlingUtil_SafeExecute_Func_WithoutRetry_Failed_Once()
        {
            bool firstTime = true;
            int executedTimes = 0;
            _target.SafeExecute(() =>
            {
                ++executedTimes;
                if (firstTime)
                {
                    firstTime = false;
                    throw new Exception();
                }
                return executedTimes;
            }, needRetry: false).Value.Should().Be(0);
            firstTime.Should().BeFalse();
        }

        [TestMethod]
        public async Task TransientFaultHandlingUtil_SafeExecute_Func_WithoutRetry_Failed_Once_Async()
        {
            bool firstTime = true;
            int executedTimes = 0;
            (await _target.SafeExecuteAsync(async () =>
            {
                ++executedTimes;
                if (firstTime)
                {
                    firstTime = false;
                    throw new Exception();
                }
                await Task.Factory.StartNew(() => { });
                return executedTimes;
            }, needRetry: false)).Value.Should().Be(0);
            firstTime.Should().BeFalse();
        }

        [TestMethod]
        public void TransientFaultHandlingUtil_SafeExecute_WithoutRetry_Failed()
        {
            int executedTimes = 0;
            _target.SafeExecute(() =>
            {
                ++executedTimes;
                throw new Exception();
            }, needRetry: false).Value.Should().BeFalse();
            executedTimes.Should().Be(1);
        }

        [TestMethod]
        public async Task TransientFaultHandlingUtil_SafeExecute_WithoutRetry_Failed_Async()
        {
            int executedTimes = 0;
            (await _target.SafeExecuteAsync(async () =>
            {
                ++executedTimes;
                await Task.Factory.StartNew(() => { });
                throw new Exception();
            }, needRetry: false)).Value.Should().BeFalse();
            executedTimes.Should().Be(1);
        }

        [TestMethod]
        public void TransientFaultHandlingUtil_SafeExecute_Func_WithoutRetry_Failed()
        {
            int executedTimes = 0;
            bool shouldThrow = true;
            _target.SafeExecute(() =>
            {
                ++executedTimes;

                if (shouldThrow)
                {
                    throw new Exception();
                }
                else
                {
                    return executedTimes;
                }
            }, needRetry: false).Value.Should().Be(0);
        }

        [TestMethod]
        public async Task TransientFaultHandlingUtil_SafeExecute_Func_WithoutRetry_Failed_Async()
        {
            int executedTimes = 0;
            bool shouldThrow = true;
            (await _target.SafeExecuteAsync(async () =>
            {
                ++executedTimes;
                await Task.Factory.StartNew(() => { });
                if (shouldThrow)
                {
                    throw new Exception();
                }
                else
                {
                    return executedTimes;
                }
            }, needRetry: false)).Value.Should().Be(0);
        }
    }
}

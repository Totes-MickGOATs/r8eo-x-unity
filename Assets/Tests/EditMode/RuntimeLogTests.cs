using NUnit.Framework;
using R8EOX.Shared;

namespace R8EOX.Tests.EditMode
{
    public class RuntimeLogTests
    {
        [Test]
        public void Log_CalledWithMessage_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => RuntimeLog.Log("test message"));
        }

        [Test]
        public void LogWarning_CalledWithMessage_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => RuntimeLog.LogWarning("test warning"));
        }

        [Test]
        public void LogError_CalledWithMessage_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => RuntimeLog.LogError("test error"));
        }

        [Test]
        public void LogFormat_CalledWithFormatAndArgs_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => RuntimeLog.LogFormat("value={0}", 42));
        }
    }
}

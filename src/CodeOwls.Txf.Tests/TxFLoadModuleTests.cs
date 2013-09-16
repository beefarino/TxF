using System.Linq;
using NUnit.Framework;

namespace CodeOwls.Txf.Tests
{
    public class TxFLoadModuleTests
    {
        [Test]
        public void CanLoadTxFModule()
        {
            var script = "123";
            var result = PowerShellTestHost.Invoke(script).FirstOrDefault();
            Assert.NotNull(result);
        }
    }
}
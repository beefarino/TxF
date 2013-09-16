using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace CodeOwls.Txf.Tests
{
    public class TxFNewItemTests
    {
        [TearDown]
        public void TestTearDown()
        {
            try
            {
                System.IO.File.Delete("test.txt");
            }
            catch
            {
            }
        }

        [Test]
        public void CanCreateNewFileWithoutTransaction()
        {

            var script = "new-item \"x$pwd\\test.txt\" -type file -value 'testing 123'";
            var result = PowerShellTestHost.Invoke(script).FirstOrDefault();
            Assert.NotNull(result);
        }

        [Test]
        public void CanCreateNewFileWithTransaction()
        {
            var script = @"
start-transaction
new-item ""x$pwd\\test.txt"" -type file -value 'txf testing 123' -usetransaction;
undo-transaction
";
            var result = PowerShellTestHost.Invoke(script).FirstOrDefault();
            Assert.NotNull(result);
        }

        [Test]
        public void NewFileExistsInTransactionScope()
        {
            var script = @"
start-transaction
new-item ""x$pwd\\test.txt"" -type file -value 'txf testing 123' -usetransaction | out-null;
test-path ""x$pwd\\test.txt"" -usetransaction
undo-transaction
";
            var result = PowerShellTestHost.Invoke(script).FirstOrDefault();
            Assert.AreEqual(true, result);
        }


        [Test]
        public void NewFileExistsOutsideCommittedTransactionScope()
        {
            var script = @"
start-transaction
new-item ""x$pwd\\test.txt"" -type file -value 'txf testing 123' -usetransaction | out-null;
complete-transaction
test-path ""x$pwd\\test.txt"" 
";
            var result = PowerShellTestHost.Invoke(script).FirstOrDefault();
            Assert.AreEqual(true, result);
        }


        [Test]
        public void NewFileDoesNotExistOutsideRolledbackTransactionScope()
        {
            var script = @"
start-transaction
new-item ""x$pwd\\test.txt"" -type file -value 'txf testing 123' -usetransaction | out-null;
undo-transaction
test-path ""x$pwd\\test.txt"" 
";
            var result = PowerShellTestHost.Invoke(script).FirstOrDefault();
            Assert.AreEqual(false, result);
        }

    }
}

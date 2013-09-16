using System;
using System.Linq;
using System.Management.Automation;
using NUnit.Framework;

namespace CodeOwls.Txf.Tests
{
    public class TxFGetItemTests
    {
        [SetUp]
        public void SetUp()
        {
            try
            {
                System.IO.File.Delete( "tmp.txt" );
            }
            catch
            {
            }
        }

        [Test]
        [ExpectedException(typeof(ItemNotFoundException))]
        public void RaisesErrorWhenPathDoesNotExist()
        {
            var script = "$i = new-item tmp.txt -type file; $p = 'x' + $i.fullname; remove-item $p; get-item $p;";
            PowerShellTestHost.Invoke(script).FirstOrDefault();
        }

        [Test]
        [ExpectedException(typeof(ItemNotFoundException))]
        public void RaisesErrorWhenPathDoesNotExistInTransaction()
        {
            var script = @"
$i = new-item tmp.txt -type file; 
$p = 'x' + $i.fullname; 
remove-item tmp.txt; 
start-transaction; 
get-item $p -usetrans;";
            PowerShellTestHost.Invoke(script).FirstOrDefault();
        }

        [Test]
        public void CanGetExistingFileWithoutTransaction()
        {
            var script = "$i = new-item tmp.txt -type file; get-item ('x' + $i.fullname)";
            var result = PowerShellTestHost.Invoke(script).FirstOrDefault();
            Assert.NotNull(result);
        }

        [Test]
        public void CanGetExistingFileWithTransaction()
        {
            var script = @"
$i = new-item tmp.txt -type file; 
start-transaction
get-item ('x' + $i.fullname) -usetransaction
undo-transaction                
";
            var result = PowerShellTestHost.Invoke(script).FirstOrDefault();
            Assert.NotNull(result);
        }

        [Test]
        public void CanGetTransactionalFileWithTransaction()
        {
            var script = @"
start-transaction
$twd = $pwd;
$i = new-item -path ('x' + $twd + '/tmp.txt') -type file -usetrans; 
get-item ($i.pspath) -usetransaction
$i.fullname;
undo-transaction                
";
            var result = PowerShellTestHost.Invoke(script).FirstOrDefault();
            Assert.NotNull(result);
        }

        [Test]
        [ExpectedException(typeof(ItemNotFoundException))]
        public void CannotGetTransactionalFileWithoutTransaction()
        {
            var script = @"
start-transaction;
$twd = $pwd;
$i = new-item ('x' + $twd + '/tmp.txt') -type file -usetrans; 
get-item $i.pspath
undo-transaction                
";
            PowerShellTestHost.Invoke(script).FirstOrDefault();
        }
    }
}
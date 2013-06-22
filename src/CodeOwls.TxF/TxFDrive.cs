using System;
using System.Management.Automation;
using System.Text;
using CodeOwls.PowerShell.Provider;

namespace CodeOwls.TxF
{
    public class TxFDrive : Drive
    {
        static TxFDrive()
        {
            AppDomain.CurrentDomain.UnhandledException += (s, a) =>
                                                              {
                                                                  var ex = a.ExceptionObject;
                                                                  var isTerm = a.IsTerminating;
                                                              };
        }
        public TxFDrive(PSDriveInfo driveInfo) : base(driveInfo)
        {
        }
    }
}

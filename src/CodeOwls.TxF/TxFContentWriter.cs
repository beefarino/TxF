using System;
using System.Collections;
using System.IO;
using System.Management.Automation.Provider;
using System.Text;
using System.Transactions;
using Microsoft.KtmIntegration;

namespace CodeOwls.TxF
{
    public class TxFContentWriter : IContentWriter
    {
        private readonly Stream _stream;
        private readonly StreamWriter _writer;

        public TxFContentWriter(Stream stream)
        {
            _stream = stream;
            _writer = new StreamWriter(_stream);
            _writer.AutoFlush = true;
        }

        public void Dispose()
        {
            _stream.Dispose();
            _writer.Dispose();
        }

        public IList Write(IList content)
        {
            using (var scope = new TransactionScope())
            {
                foreach (var item in content)
                {
                    _writer.Write(item.ToString());
                }
                _writer.Flush();
                scope.Complete();
            }

            return content;
            
        }

        public void Seek(long offset, SeekOrigin origin)
        {
            _stream.Seek(offset, origin);
        }

        public void Close()
        {
            using (var scope = new TransactionScope())
            {
                _writer.Close();
                _stream.Close();
                
                scope.Complete();
            }
        }
    }
}
using System.Collections;
using System.IO;
using System.Linq;
using System.Management.Automation.Provider;
using System.Text;
using System.Transactions;
using Microsoft.KtmIntegration;

namespace CodeOwls.TxF
{
    public class TxFContentReader : IContentReader
    {
        private readonly FileSystemInfo _fileInfo;
        private readonly StreamReader _reader;
        private readonly Stream _stream;

        public TxFContentReader(Stream stream)
        {
            _stream = stream;
            _reader = new StreamReader(_stream);
        }

        public void Dispose()
        {
            _stream.Dispose();
            _reader.Dispose();
        }

        public IList Read(long readCount)
        {
            using (var scope = new TransactionScope())
            {
                var list = new ArrayList();
                while( 0 < readCount-- )
                {
                    var line = _reader.ReadLine();
                    if (null == line)
                    {
                        continue;
                    }
                    list.Add(line);
                }
                scope.Complete();
                return list;
            }
        }

        public void Seek(long offset, SeekOrigin origin)
        {
            _stream.Seek(offset, origin);
        }

        public void Close()
        {
            _stream.Close();
            _reader.Close();
        }
    }
}
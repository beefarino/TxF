using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Remoting;
using System.Text;
using System.Transactions;

namespace CodeOwls.TxF
{
    public class TransactedStreamDecorator : Stream
    {
        private readonly Stream _stream;

        public TransactedStreamDecorator(FileStream stream)
        {
            _stream = stream;
        }

        public override  object InitializeLifetimeService()
        {
            using (TransactionScope scope = new TransactionScope())
            return _stream.InitializeLifetimeService();
        }

        public override  ObjRef CreateObjRef(Type requestedType)
        {
            using (TransactionScope scope = new TransactionScope())
            return _stream.CreateObjRef(requestedType);
        }

        public override  void Close()
        {
            using (TransactionScope scope = new TransactionScope())
            _stream.Close();
        }

        public override  void Flush()
        {
            using (TransactionScope scope = new TransactionScope())
            _stream.Flush();
        }

        public override  IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            using (TransactionScope scope = new TransactionScope())
            return _stream.BeginRead(buffer, offset, count, callback, state);
        }

        public override  int EndRead(IAsyncResult asyncResult)
        {
            using (TransactionScope scope = new TransactionScope())
            return _stream.EndRead(asyncResult);
        }

        public override  IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            using (TransactionScope scope = new TransactionScope())
            return _stream.BeginWrite(buffer, offset, count, callback, state);
        }

        public override  void EndWrite(IAsyncResult asyncResult)
        {
            using (TransactionScope scope = new TransactionScope())
            _stream.EndWrite(asyncResult);
        }

        public override  long Seek(long offset, SeekOrigin origin)
        {
            using (TransactionScope scope = new TransactionScope())
            return _stream.Seek(offset, origin);
        }

        public override  void SetLength(long value)
        {
            using (TransactionScope scope = new TransactionScope())
            _stream.SetLength(value);
        }

        public override  int Read(byte[] buffer, int offset, int count)
        {
            using (TransactionScope scope = new TransactionScope())
            return _stream.Read(buffer, offset, count);
        }

        public override  int ReadByte()
        {
            using (TransactionScope scope = new TransactionScope())
            return _stream.ReadByte();
        }

        public override  void Write(byte[] buffer, int offset, int count)
        {
            using (TransactionScope scope = new TransactionScope())
            _stream.Write(buffer, offset, count);
        }

        public override  void WriteByte(byte value)
        {
            using (TransactionScope scope = new TransactionScope())
            _stream.WriteByte(value);
        }

        public override  bool CanRead
        {
            get
            {
                using (TransactionScope scope = new TransactionScope()) 
                    return _stream.CanRead;
            }
        }

        public override  bool CanSeek
        {
            get
            {
                using (TransactionScope scope = new TransactionScope()) 
                    return _stream.CanSeek;
            }
        }

        public override  bool CanTimeout
        {
            get
            {
                using (TransactionScope scope = new TransactionScope()) 
                    return _stream.CanTimeout;
            }
        }

        public override  bool CanWrite
        {
            get
            {
                using (TransactionScope scope = new TransactionScope()) 
                    return _stream.CanWrite;
            }
        }

        public override  long Length
        {
            get
            {
                using (TransactionScope scope = new TransactionScope()) 
                    return _stream.Length;
            }
        }

        public override long Position
        {
            get
            {
                using (TransactionScope scope = new TransactionScope()) 
                    return _stream.Position;
            }
            set
            {
                using (TransactionScope scope = new TransactionScope()) 
                    _stream.Position = value;
            }
        }

        public override int ReadTimeout
        {
            get
            {
                using (TransactionScope scope = new TransactionScope()) 
                    return _stream.ReadTimeout;
            }
            set
            {
                using (TransactionScope scope = new TransactionScope()) 
                    _stream.ReadTimeout = value;
            }
        }

        public override int WriteTimeout
        {
            get
            {
                using (TransactionScope scope = new TransactionScope()) 
                    return _stream.WriteTimeout;
            }
            set
            {
                using (TransactionScope scope = new TransactionScope()) 
                    _stream.WriteTimeout = value;
            }
        }
    }
}

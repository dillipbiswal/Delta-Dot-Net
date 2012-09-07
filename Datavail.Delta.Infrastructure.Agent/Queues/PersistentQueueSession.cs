namespace Datavail.Delta.Infrastructure.Agent.Queues
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;

    public class PersistentQueueSession : IDisposable
    {
        private readonly List<Operation> _operations = new List<Operation>();
        private readonly IList<Exception> _pendingWritesFailures = new List<Exception>();
        private readonly IList<WaitHandle> _pendingWritesHandles = new List<WaitHandle>();
        private Stream _currentStream;
        private readonly int _writeBufferSize;
        private readonly IPersistentQueueImpl _queue;
        private readonly List<Stream> _streamsToDisposeOnFlush = new List<Stream>();

        private readonly List<byte[]> _buffer = new List<byte[]>();
        private int _bufferSize;

        private const int MinSizeThatMakeAsyncWritePractical = 64 * 1024;

        public PersistentQueueSession(IPersistentQueueImpl queue, Stream currentStream, int writeBufferSize)
        {
            _queue = queue;
            _currentStream = currentStream;
            if (writeBufferSize < MinSizeThatMakeAsyncWritePractical)
                writeBufferSize = MinSizeThatMakeAsyncWritePractical;
            _writeBufferSize = writeBufferSize;
        }

        public void Enqueue(byte[] data)
        {
            _buffer.Add(data);
            _bufferSize += data.Length;
            if (_bufferSize > _writeBufferSize)
            {
                AsyncFlushBuffer();
            }
        }

        private void AsyncFlushBuffer()
        {
            _queue.AcquireWriter(_currentStream, AsyncWriteToStream, OnReplaceStream);
        }

        private void SyncFlushBuffer()
        {
            _queue.AcquireWriter(_currentStream, stream =>
            {
                byte[] data = ConcatenateBufferAndAddIndividualOperations(stream);
                stream.Write(data, 0, data.Length);
                return stream.Position;
            }, OnReplaceStream);
        }

        private long AsyncWriteToStream(Stream stream)
        {
            byte[] data = ConcatenateBufferAndAddIndividualOperations(stream);
            var resetEvent = new ManualResetEvent(false);
            _pendingWritesHandles.Add(resetEvent);
            long positionAfterWrite = stream.Position + data.Length;
            stream.BeginWrite(data, 0, data.Length, delegate(IAsyncResult ar)
            {
                try
                {
                    stream.EndWrite(ar);
                }
                catch (Exception e)
                {
                    lock (_pendingWritesFailures)
                    {
                        _pendingWritesFailures.Add(e);
                    }
                }
                finally
                {
                    resetEvent.Set();
                }
            }, null);
            return positionAfterWrite;
        }

        private byte[] ConcatenateBufferAndAddIndividualOperations(Stream stream)
        {
            var data = new byte[_bufferSize];
            var start = (int)stream.Position;
            var index = 0;
            foreach (var bytes in _buffer)
            {
                _operations.Add(new Operation(
                    OperationType.Enqueue,
                    _queue.CurrentFileNumber,
                    start,
                    bytes.Length
                ));
                Buffer.BlockCopy(bytes, 0, data, index, bytes.Length);
                start += bytes.Length;
                index += bytes.Length;
            }
            _bufferSize = 0;
            _buffer.Clear();
            return data;
        }

        private void OnReplaceStream(Stream newStream)
        {
            _streamsToDisposeOnFlush.Add(_currentStream);
            _currentStream = newStream;
        }

        public byte[] Dequeue()
        {
            var entry = _queue.Dequeue();
            if (entry == null)
                return null;
            _operations.Add(new Operation(
                OperationType.Dequeue,
                entry.FileNumber,
                entry.Start,
                entry.Length
            ));
            return entry.Data;
        }

        public void Flush()
        {
            try
            {
                WaitForPendingWrites();
                SyncFlushBuffer();
            }
            finally
            {
                foreach (var stream in _streamsToDisposeOnFlush)
                {
                    stream.Flush();
                    stream.Dispose();
                }
                _streamsToDisposeOnFlush.Clear();
            }
            _currentStream.Flush();
            _queue.CommitTransaction(_operations);
            _operations.Clear();
        }

        private void WaitForPendingWrites()
        {
            while (_pendingWritesHandles.Count != 0)
            {
                var handles = _pendingWritesHandles.Take(64).ToArray();
                foreach (var handle in handles)
                {
                    _pendingWritesHandles.Remove(handle);
                }
                WaitHandle.WaitAll(handles);
                foreach (var handle in handles)
                {
                    handle.Close();
                }
                AssertNoPendingWritesFailures();
            }
        }

        private void AssertNoPendingWritesFailures()
        {
            lock (_pendingWritesFailures)
            {
                if (_pendingWritesFailures.Count == 0)
                    return;

                var array = _pendingWritesFailures.ToArray();
                _pendingWritesFailures.Clear();
                throw new PendingWriteException(array);
            }
        }

        public void Dispose()
        {
            _queue.Reinstate(_operations);
            _operations.Clear();
            foreach (var stream in _streamsToDisposeOnFlush)
            {
                stream.Dispose();
            }
            _currentStream.Dispose();
            GC.SuppressFinalize(this);
        }

        ~PersistentQueueSession()
        {
            Dispose();
        }
    }
}
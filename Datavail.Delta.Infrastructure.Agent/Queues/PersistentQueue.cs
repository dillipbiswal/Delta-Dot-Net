
using Datavail.Delta.Infrastructure.Agent.Logging;

namespace Datavail.Delta.Infrastructure.Agent.Queues
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    public class PersistentQueue : IPersistentQueueImpl
    {
        private readonly HashSet<Entry> _checkedOutEntries = new HashSet<Entry>();

        private readonly Dictionary<int, int> _countOfItemsPerFile =
            new Dictionary<int, int>();

        private readonly LinkedList<Entry> _entries = new LinkedList<Entry>();

        private readonly string _path;

        private readonly object _transactionLogLock = new object();
        private readonly object _writerLock = new object();
        private FileStream _fileLock;
        private readonly IDeltaLogger _logger;

        public bool TrimTransactionLogOnDispose { get; set; }
        public int SuggestedReadBuffer { get; set; }
        public int SuggestedWriteBuffer { get; set; }
        public long SuggestedMaxTransactionLogSize { get; set; }

        public PersistentQueue(string path, int maxFileSize)
        {
            _logger = new DeltaLogger();

            TrimTransactionLogOnDispose = true;
            SuggestedMaxTransactionLogSize = 32 * 1024 * 1024;
            SuggestedReadBuffer = 1024 * 1024;
            SuggestedWriteBuffer = 1024 * 1024;

            _path = Path.GetFullPath(path);
            MaxFileSize = maxFileSize;
            try
            {
                _fileLock = new FileStream(Path.Combine(path, "lock"), FileMode.Create, FileAccess.ReadWrite, FileShare.None);
            }
            catch (IOException e)
            {
                _logger.LogUnhandledException("Unhandled Exception in PersistentQueue(string path, int maxFileSize)", e);
                GC.SuppressFinalize(this); //avoid finalizing invalid instance
                throw new InvalidOperationException("Another instance of the queue is already in action, or directory does not exists", e);
            }

            try
            {
                ReadMetaState();
                ReadTransactionLog();
            }
            catch (Exception)
            {
                GC.SuppressFinalize(this); //avoid finalizing invalid instance
                _fileLock.Dispose();
                throw;
            }
        }

        public PersistentQueue(string path)
            : this(path, 32 * 1024 * 1024)
        {
            _path = path;
        }

        /// <summary>
        /// Gets the estimated count of items in queue.
        /// This is estimated number because it is not synced across threads
        /// </summary>
        /// <value>The estimated count of items in queue.</value>
        public int EstimatedCountOfItemsInQueue
        {
            get { return _entries.Count + _checkedOutEntries.Count; }
        }

        private int CurrentCountOfItemsInQueue
        {
            get
            {
                lock (_entries)
                {
                    return _entries.Count + _checkedOutEntries.Count;
                }
            }
        }

        public int MaxFileSize { get; private set; }
        public long CurrentFilePosition { get; private set; }

        private string TransactionLog
        {
            get { return Path.Combine(_path, "transaction.log"); }
        }

        private string Meta
        {
            get { return Path.Combine(_path, "meta.state"); }
        }

        #region IPersistentQueueImpl Members

        public int CurrentFileNumber { get; private set; }

        public void Dispose()
        {
            if (TrimTransactionLogOnDispose)
            {
                lock (_transactionLogLock)
                {
                    FlushTrimmedTransactionLog();
                }
            }
            if (_fileLock != null)
                _fileLock.Dispose();
            _fileLock = null;
            GC.SuppressFinalize(this);
        }


        public void AcquireWriter(
            Stream stream,
            Func<Stream, long> action,
            Action<Stream> onReplaceStream)
        {
            lock (_writerLock)
            {
                if (stream.Position != CurrentFilePosition)
                {
                    stream.Position = CurrentFilePosition;
                }
                CurrentFilePosition = action(stream);
                if (CurrentFilePosition < MaxFileSize)
                    return;
                CurrentFileNumber += 1;
                var writer = CreateWriter();
                // we assume same size messages, or near size messages
                // that gives us a good heuroistic for creating the size of 
                // the new file, so it wouldn't be fragmented
                writer.SetLength(CurrentFilePosition);
                CurrentFilePosition = 0;
                onReplaceStream(writer);
            }
        }

        public void CommitTransaction(ICollection<Operation> operations)
        {
            if (operations.Count == 0)
                return;

            byte[] transactionBuffer = GenerateTransactionBuffer(operations);

            lock (_transactionLogLock)
            {
                long txLogSize;
                using (
                    var stream = new FileStream(TransactionLog,
                                                FileMode.Append,
                                                FileAccess.Write,
                                                FileShare.None,
                                                transactionBuffer.Length,
                                                FileOptions.SequentialScan | FileOptions.WriteThrough)
                    )
                {
                    stream.Write(transactionBuffer, 0, transactionBuffer.Length);
                    txLogSize = stream.Position;
                    stream.Flush();
                }

                ApplyTransactionOperations(operations);
                TrimTransactionLogIfNeeded(txLogSize);

                Atomic.Write(Meta, stream =>
                {
                    var bytes = BitConverter.GetBytes(CurrentFileNumber);
                    stream.Write(bytes, 0, bytes.Length);
                    bytes = BitConverter.GetBytes(CurrentFilePosition);
                    stream.Write(bytes, 0, bytes.Length);
                });
            }
        }

        public Entry Dequeue()
        {
            lock (_entries)
            {
                var first = _entries.First;
                if (first == null)
                    return null;
                var entry = first.Value;
                if (entry.Data == null)
                {
                    ReadAhead();
                }
                _entries.RemoveFirst();
                // we need to create a copy so we will not hold the data
                // in memory as well as the position
                _checkedOutEntries.Add(new Entry(entry.FileNumber, entry.Start, entry.Length));
                return entry;
            }
        }

        /// <summary>
        /// Assumes that entries has at least one entry
        /// </summary>
        private void ReadAhead()
        {
            long currentBufferSize = 0;
            var firstEntry = _entries.First.Value;
            Entry lastEntry = firstEntry;
            foreach (var entry in _entries)
            {
                // we can't read ahead to another file or
                // if we have unordered queue, or sparse items
                if (entry != lastEntry &&
                    (entry.FileNumber != lastEntry.FileNumber ||
                    entry.Start != (lastEntry.Start + lastEntry.Length)))
                    break;
                if (currentBufferSize + entry.Length > SuggestedReadBuffer)
                    break;
                lastEntry = entry;
                currentBufferSize += entry.Length;
            }
            if (lastEntry == firstEntry)
                currentBufferSize = lastEntry.Length;

            byte[] buffer = ReadEntriesFromFile(firstEntry, currentBufferSize);

            var index = 0;
            foreach (var entry in _entries)
            {
                entry.Data = new byte[entry.Length];
                Buffer.BlockCopy(buffer, index, entry.Data, 0, entry.Length);
                index += entry.Length;
                if (entry == lastEntry)
                    break;
            }
        }

        private byte[] ReadEntriesFromFile(Entry firstEntry, long currentBufferSize)
        {
            var buffer = new byte[currentBufferSize];
            using (var reader = new FileStream(GetDataPath(firstEntry.FileNumber),
                                               FileMode.OpenOrCreate, FileAccess.Read, FileShare.ReadWrite))
            {
                reader.Position = firstEntry.Start;
                var totalRead = 0;
                do
                {
                    var bytesRead = reader.Read(buffer, totalRead, buffer.Length - totalRead);
                    if (bytesRead == 0)
                        throw new InvalidOperationException("End of file reached while trying to read queue item");
                    totalRead += bytesRead;
                } while (totalRead < buffer.Length);
            }
            return buffer;
        }

        public PersistentQueueSession OpenSession()
        {
            return new PersistentQueueSession(this, CreateWriter(), SuggestedWriteBuffer);
        }

        public void Reinstate(IEnumerable<Operation> reinstatedOperations)
        {
            lock (_entries)
            {
                ApplyTransactionOperations(
                    from entry in reinstatedOperations
                    where entry.Type == OperationType.Dequeue
                    select new Operation(
                        OperationType.Reinstate,
                        entry.FileNumber,
                        entry.Start,
                        entry.Length
                        )
                    );
            }
        }

        #endregion

        private void ReadTransactionLog()
        {
            var requireTxLogTrimming = false;
            Atomic.Read(TransactionLog, stream =>
            {
                using (var binaryReader = new BinaryReader(stream))
                {
                    bool readingTransaction = false;
                    try
                    {
                        int txCount = 0;
                        while (true)
                        {
                            txCount += 1;
                            // this code ensures that we read the full transaction
                            // before we start to apply it. The last truncated transaction will be
                            // ignored automatically.
                            AssertTransactionSeperator(binaryReader, txCount, Constants.StartTransactionSeparatorGuid,
                                                       () => readingTransaction = true);
                            var opsCount = binaryReader.ReadInt32();
                            var txOps = new List<Operation>(opsCount);
                            for (var i = 0; i < opsCount; i++)
                            {
                                AssertOperationSeparator(binaryReader);
                                var operation = new Operation(
                                    (OperationType)binaryReader.ReadByte(),
                                    binaryReader.ReadInt32(),
                                    binaryReader.ReadInt32(),
                                    binaryReader.ReadInt32()
                                );
                                txOps.Add(operation);
                                //if we have non enqueue entries, this means 
                                // that we have not closed properly, so we need
                                // to trim the log
                                if (operation.Type != OperationType.Enqueue)
                                    requireTxLogTrimming = true;
                            }
                            AssertTransactionSeperator(binaryReader, txCount, Constants.EndTransactionSeparatorGuid, () => { });
                            readingTransaction = false;
                            ApplyTransactionOperations(txOps);
                        }
                    }
                    catch (EndOfStreamException)
                    {
                        // we have a truncated transaction, need to clear that
                        if (readingTransaction)
                            requireTxLogTrimming = true;
                        return;
                    }
                }
            });
            if (requireTxLogTrimming)
                FlushTrimmedTransactionLog();
        }

        private void FlushTrimmedTransactionLog()
        {
            byte[] transactionBuffer;
            using (var ms = new MemoryStream())
            {
                ms.Write(Constants.StartTransactionSeparator, 0, Constants.StartTransactionSeparator.Length);

                var count = BitConverter.GetBytes(EstimatedCountOfItemsInQueue);
                ms.Write(count, 0, count.Length);

                foreach (var entry in _checkedOutEntries)
                {
                    WriteEntryToTransactionLog(ms, entry, OperationType.Enqueue);
                }
                foreach (var entry in _entries)
                {
                    WriteEntryToTransactionLog(ms, entry, OperationType.Enqueue);
                }
                ms.Write(Constants.EndTransactionSeparator, 0, Constants.EndTransactionSeparator.Length);
                ms.Flush();
                transactionBuffer = ms.ToArray();
            }
            Atomic.Write(TransactionLog, stream =>
            {
                stream.SetLength(transactionBuffer.Length);
                stream.Write(transactionBuffer, 0, transactionBuffer.Length);
            });
        }

        private static void WriteEntryToTransactionLog(Stream ms, Entry entry, OperationType operationType)
        {
            ms.Write(Constants.OperationSeparatorBytes, 0, Constants.OperationSeparatorBytes.Length);

            ms.WriteByte((byte)operationType);

            var fileNumber = BitConverter.GetBytes(entry.FileNumber);
            ms.Write(fileNumber, 0, fileNumber.Length);

            var start = BitConverter.GetBytes(entry.Start);
            ms.Write(start, 0, start.Length);

            var length = BitConverter.GetBytes(entry.Length);
            ms.Write(length, 0, length.Length);
        }

        private static void AssertOperationSeparator(BinaryReader reader)
        {
            var separator = reader.ReadInt32();
            if (separator != Constants.OperationSeparator)
                throw new InvalidOperationException(
                    "Unexpected data in transaction log. Expected to get transaction separator but got unknonwn data");
        }

        public int[] ApplyTransactionOperationsInMemory(IEnumerable<Operation> operations)
        {
            foreach (var operation in operations)
            {
                switch (operation.Type)
                {
                    case OperationType.Enqueue:
                        var entryToAdd = new Entry(operation);
                        _entries.AddLast(entryToAdd);
                        var itemCountAddition = _countOfItemsPerFile.GetValueOrDefault(entryToAdd.FileNumber);
                        _countOfItemsPerFile[entryToAdd.FileNumber] = itemCountAddition + 1;
                        break;

                    case OperationType.Dequeue:
                        var entryToRemove = new Entry(operation);
                        _checkedOutEntries.Remove(entryToRemove);
                        var itemCountRemoval = _countOfItemsPerFile.GetValueOrDefault(entryToRemove.FileNumber);
                        _countOfItemsPerFile[entryToRemove.FileNumber] = itemCountRemoval - 1;
                        break;

                    case OperationType.Reinstate:
                        var entryToReistate = new Entry(operation);
                        _entries.AddFirst(entryToReistate);
                        _checkedOutEntries.Remove(entryToReistate);
                        break;
                }
            }
            var filesToRemove = new HashSet<int>(
                from pair in _countOfItemsPerFile
                where pair.Value < 1
                select pair.Key
                );
            foreach (var i in filesToRemove)
            {
                _countOfItemsPerFile.Remove(i);
            }
            return filesToRemove.ToArray();
        }

        private static void AssertTransactionSeperator(
            BinaryReader binaryReader, int txCount, Guid expectedValue, Action hasData)
        {
            var bytes = binaryReader.ReadBytes(16);
            if (bytes.Length == 0)
                throw new EndOfStreamException();
            hasData();
            if (bytes.Length != 16)
            {
                // looks like we have a truncated transaction in this case, we will 
                // say that we run into end of stream and let the log trimming to deal with this
                if (binaryReader.BaseStream.Length == binaryReader.BaseStream.Position)
                {
                    throw new EndOfStreamException();
                }
                throw new InvalidOperationException(
                    "Unexpected data in transaction log. Expected to get transaction separator but got truncated data. Tx #" + txCount);
            }
            var separator = new Guid(bytes);
            if (separator != expectedValue)
                throw new InvalidOperationException(
                    "Unexpected data in transaction log. Expected to get transaction separator but got unknonwn data. Tx #" + txCount);
        }


        private void ReadMetaState()
        {
            Atomic.Read(Meta, stream =>
            {
                using (var binaryReader = new BinaryReader(stream))
                {
                    try
                    {
                        CurrentFileNumber = binaryReader.ReadInt32();
                        CurrentFilePosition = binaryReader.ReadInt64();
                    }
                    catch (EndOfStreamException)
                    {
                    }
                }
            });
        }


        private void TrimTransactionLogIfNeeded(long txLogSize)
        {
            if (txLogSize < SuggestedMaxTransactionLogSize)// it is not big enough to care
                return;
            var optimalSize = GetOptimalTransactionLogSize();
            if (txLogSize < (optimalSize * 2))   // not enough disparity to bother trimming
                return;
            FlushTrimmedTransactionLog();
        }

        private void ApplyTransactionOperations(IEnumerable<Operation> operations)
        {
            int[] filesToRemove;
            lock (_entries)
            {
                filesToRemove = ApplyTransactionOperationsInMemory(operations);
            }
            foreach (var fileNumber in filesToRemove)
            {
                if (CurrentFileNumber == fileNumber)
                    continue;
                File.Delete(GetDataPath(fileNumber));
            }
        }

        private static byte[] GenerateTransactionBuffer(ICollection<Operation> operations)
        {
            byte[] transactionBuffer;
            using (var ms = new MemoryStream())
            {
                ms.Write(Constants.StartTransactionSeparator, 0, Constants.StartTransactionSeparator.Length);

                var count = BitConverter.GetBytes(operations.Count);
                ms.Write(count, 0, count.Length);

                foreach (var operation in operations)
                {
                    WriteEntryToTransactionLog(ms, new Entry(operation), operation.Type);
                }
                ms.Write(Constants.EndTransactionSeparator, 0, Constants.EndTransactionSeparator.Length);

                ms.Flush();
                transactionBuffer = ms.ToArray();
            }
            return transactionBuffer;
        }

        private FileStream CreateWriter()
        {
            return new FileStream(
                GetDataPath(CurrentFileNumber),
                FileMode.OpenOrCreate,
                FileAccess.Write,
                FileShare.ReadWrite,
                0x10000,
                FileOptions.Asynchronous | FileOptions.SequentialScan | FileOptions.WriteThrough);
        }

        public string GetDataPath(int index)
        {
            return Path.Combine(_path, "data." + index);
        }

        ~PersistentQueue()
        {
            Dispose();
        }

        private long GetOptimalTransactionLogSize()
        {
            long size = 0;
            size += 16 /*sizeof(guid)*/; //	initial tx separator
            size += sizeof(int); // 	operation count

            size +=
                ( // entry size == 16
                sizeof(int) + // 		operation separator
                sizeof(int) + // 		file number
                sizeof(int) + //		start
                sizeof(int) //		length
                )
                *
                (CurrentCountOfItemsInQueue);

            return size;
        }
    }
}
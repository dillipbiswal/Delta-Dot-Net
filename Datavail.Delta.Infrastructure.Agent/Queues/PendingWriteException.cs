
namespace Datavail.Delta.Infrastructure.Agent.Queues
{
    using System;
    using System.Text;

    public class PendingWriteException : Exception
    {
        private readonly Exception[] _pendingWritesExceptions;

        public PendingWriteException(Exception[] pendingWritesExceptions)
            : base("Error during pending writes")
        {
            _pendingWritesExceptions = pendingWritesExceptions;
        }

        public Exception[] PendingWritesExceptions
        {
            get { return _pendingWritesExceptions; }
        }

        public override string Message
        {
            get
            {
                var sb = new StringBuilder(base.Message).Append(":");
                foreach (var exception in _pendingWritesExceptions)
                {
                    sb.AppendLine().Append(" - ").Append(exception.Message);
                }
                return sb.ToString();
            }
        }

        public override string ToString()
        {
            var sb = new StringBuilder(base.Message).Append(":");
            foreach (var exception in _pendingWritesExceptions)
            {
                sb.AppendLine().Append(" - ").Append(exception);
            }
            return sb.ToString();
        }
    }
}
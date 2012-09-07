using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Datavail.Delta.Agent.SharedCode.Queues
{
    [Serializable]
    public class QueueMessage
    {
        public DateTime Timestamp { get; set; }
        public string Data { get; set; }

        public byte[] ToByteArray()
        {
            var stream = new MemoryStream();
            var formatter = new BinaryFormatter();
            formatter.Serialize(stream, this);
            return stream.ToArray();
        }

        public static QueueMessage FromByteArray(byte[] array)
        {
            var stream = new MemoryStream(array);
            var formatter = new BinaryFormatter();
            var msg = (QueueMessage)formatter.Deserialize(stream);

            return msg;
        }
    }
}

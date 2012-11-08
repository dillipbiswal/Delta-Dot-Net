
using System;
using System.IO;

namespace Datavail.Delta.Agent.SharedCode.Queues
{
    /// <summary>
    /// Allow to overwrite a file in a transactional manner.
    /// That is, either we completely succeed or completely fail in writing to the file.
    /// Read will correct previous failed transaction if a previous write has failed.
    /// Assumptions:
    ///  * You want to always rewrite the file, rathar than edit it.
    ///  * The underlying file system has at least transactional metadata.
    ///  * Thread safety is provided by the calling code.
    /// 
    /// Write implementation:
    ///  - Rename file to "[file].old_copy" (overwrite if needed
    ///  - Create new file stream with the file name and pass it to the client
    ///  - After client is done, close the stream
    ///  - Delete old file
    /// 
    /// Read implementation:
    ///  - If old file exists, remove new file and rename old file
    /// 
    /// </summary>
    public static class Atomic
    {
        public static void Read(string name, Action<Stream> action)
        {
            if (File.Exists(name + ".old_copy"))
            {
                File.Delete(name);
                File.Move(name + ".old_copy", name);
            }

            using (
                var stream = new FileStream(name,
                    FileMode.OpenOrCreate,
                    FileAccess.Read,
                    FileShare.None,
                    0x10000,
                    FileOptions.SequentialScan)
                    )
            {
                action(stream);
            }
        }

        public static void Write(string name, Action<Stream> action)
        {
            // if the old copy file exists, this means that we have
            // a previous corrupt write, so we will not overrite it, but 
            // rather overwrite the current file and keep it as our backup.
            if (File.Exists(name + ".old_copy") == false)
                File.Move(name, name + ".old_copy");

            using (
                var stream = new FileStream(name,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.None,
                    0x10000,
                    FileOptions.WriteThrough | FileOptions.SequentialScan)
                    )
            {
                action(stream);
                stream.Flush();
            }

            File.Delete(name + ".old_copy");
        }
    }
}
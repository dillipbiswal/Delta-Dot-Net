using System;

namespace Datavail.Delta.Domain
{
    public class ServerDisk : DomainBase
    {
        #region Fields
        #endregion

        #region Properties
        public virtual Cluster Cluster { get; set; }
        public string Label { get; set; }
        public string Path { get; set; }
        public virtual Server Server { get; set; }
        public long TotalBytes { get; set; }
        #endregion

        #region ctor
        [Obsolete("Use static NewServerDisk factory instead")]
        public ServerDisk()
        {
            Initialize();
        }

#pragma warning disable 612,618
        public static ServerDisk NewServerDisk(string path, Server server, long totalBytes, string label = null)
        {
            var entity = new ServerDisk { Path = path, Server = server, TotalBytes = totalBytes};
            if (label != null) entity.Label = label;
            return entity;
        }

        public static ServerDisk NewServerDisk(string path, Cluster cluster, long totalBytes, string label = null)
        {
            var entity = new ServerDisk { Path = path, Cluster = cluster, TotalBytes = totalBytes};
            if (label != null) entity.Label = label;
            return entity;
        }
#pragma warning restore 612,618

        private void Initialize()
        {
        }
        #endregion

        #region Methods
        #endregion
    }
}

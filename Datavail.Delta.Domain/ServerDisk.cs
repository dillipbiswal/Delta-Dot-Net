using System;

namespace Datavail.Delta.Domain
{
    public class ServerDisk : DomainBase, IEquatable<ServerDisk>
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
            var entity = new ServerDisk { Path = path, Server = server, TotalBytes = totalBytes };
            if (label != null) entity.Label = label;
            return entity;
        }

        public static ServerDisk NewServerDisk(string path, Cluster cluster, long totalBytes, string label = null)
        {
            var entity = new ServerDisk { Path = path, Cluster = cluster, TotalBytes = totalBytes };
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

        #region Equality

        public virtual bool Equals(ServerDisk other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(other.Path, Path);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != typeof(ServerDisk)) return false;
            return Equals((ServerDisk)obj);
        }

        public override int GetHashCode()
        {
            return (Path != null ? Path.GetHashCode() : 0);
        }

        public static bool operator ==(ServerDisk left, ServerDisk right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(ServerDisk left, ServerDisk right)
        {
            return !Equals(left, right);
        }

        #endregion
    }
}

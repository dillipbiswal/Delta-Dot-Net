using System;

namespace Datavail.Delta.Domain
{
    public class WebSiteData : DomainBase, IEquatable<WebSiteData>
    {
        #region Fields
        #endregion

        #region Properties
        public string Label { get; set; }
        public string Path { get; set; }
        public virtual Server Server { get; set; }
        public string Identity { get; set; }
        #endregion

        #region ctor
        [Obsolete("Use static NewWebSiteData factory instead")]
        public WebSiteData()
        {
            Initialize();
        }

#pragma warning disable 612,618
        public static WebSiteData NewWebSiteData(string path, Server server, string label, string identity)
        {
            var entity = new WebSiteData { Path = path, Server = server, Identity = identity };
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

        public virtual bool Equals(WebSiteData other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(other.Path, Path);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != typeof(WebSiteData)) return false;
            return Equals((WebSiteData)obj);
        }

        public override int GetHashCode()
        {
            return (Path != null ? Path.GetHashCode() : 0);
        }

        public static bool operator ==(WebSiteData left, WebSiteData right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(WebSiteData left, WebSiteData right)
        {
            return !Equals(left, right);
        }

        #endregion
    }
}

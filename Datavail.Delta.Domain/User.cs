using System;
using System.Security.Cryptography;
using System.Text;
using System.Collections.Generic;

namespace Datavail.Delta.Domain
{
    public class User : DomainBase, IEquatable<User>
    {
        #region Fields
        #endregion

        #region Properties
        public string EmailAddress { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string PasswordHash { get; set; }
        public string PasswordSalt { get; set; }
        public virtual IList<Role> Roles { get; set; }
        #endregion

        #region ctor
        [Obsolete("Use static NewUser factory instead")]
        public User()
        {
            Initialize();
        }

#pragma warning disable 612,618
        public static User NewUser(string emailAddress, string firstName, string lastName, string password)
        {
            var entity = new User() { EmailAddress = emailAddress, FirstName = firstName, LastName = lastName };
            entity.SetPassword(password);

            return entity;
        }
#pragma warning restore 612,618

        private void Initialize()
        {
        }
        #endregion

        #region Methods
        public virtual void SetPassword(string password)
        {
            GenerateNewSalt();
            PasswordHash = HashPassword(password);
        }

        public virtual bool HasPassword(string password)
        {
            var hash = HashPassword(password);
            return hash == PasswordHash;
        }

        private string HashPassword(string password)
        {
            var hasher = SHA256.Create();
            return Encoding.UTF8.GetString(hasher.ComputeHash(Encoding.UTF8.GetBytes(PasswordSalt + password)));
        }

        private void GenerateNewSalt()
        {
            var cryptoService = new RNGCryptoServiceProvider();
            var buffer = new byte[10];
            cryptoService.GetBytes(buffer);
            PasswordSalt = Encoding.UTF8.GetString(buffer);
        }
        #endregion

        #region Equality

        public virtual bool Equals(User other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(other.EmailAddress, EmailAddress);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != typeof(User)) return false;
            return Equals((User)obj);
        }

        public override int GetHashCode()
        {
            return (EmailAddress != null ? EmailAddress.GetHashCode() : 0);
        }

        public static bool operator ==(User left, User right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(User left, User right)
        {
            return !Equals(left, right);
        }

        #endregion
    }
}

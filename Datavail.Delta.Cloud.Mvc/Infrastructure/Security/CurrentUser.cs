using System.Security.Principal;
using Datavail.Delta.Infrastructure.Repository;
using Datavail.Delta.Domain;
using Datavail.Delta.Infrastructure.Specification;

namespace Datavail.Delta.Cloud.Mvc.Infrastructure.Security
{
	public class CurrentUser
	{
		private readonly IRepository _users;
		private readonly IIdentity _currentUser;

		public CurrentUser(IRepository users, IIdentity currentUser)
		{
			_users = users;
			_currentUser = currentUser;
		}

		public User Instance
		{
			get { return _users.FindOne(new Specification<User>(u => u.EmailAddress == _currentUser.Name)); }
		}
	}
}
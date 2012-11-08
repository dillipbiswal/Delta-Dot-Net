using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using Datavail.Delta.Application.Interface;
using Datavail.Delta.Domain;
using Datavail.Delta.Infrastructure.Repository;
using Datavail.Delta.Infrastructure.Specification;
using Datavail.Delta.Repository.EfWithMigrations;
using StructureMap.Attributes;
using StructureMap;

namespace Datavail.Delta.Cloud.Mvc.Infrastructure
{
    public class DeltaRoleProvider : RoleProvider
    {
        #region "Private Variables"
        public IServerService _serverService;
        private IRepository _repository;
        #endregion

        public DeltaRoleProvider()
            : this(DependencyResolver.Current.GetService<IRepository>())
    { }

        public DeltaRoleProvider(IRepository repository)
        {
            _serverService = ObjectFactory.GetInstance<IServerService>();
            _repository = repository;
        }

        public override void AddUsersToRoles(string[] usernames, string[] roleNames)
        {
            throw new NotImplementedException();
        }

        public override string ApplicationName
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public override void CreateRole(string roleName)
        {
            throw new NotImplementedException();
        }

        public override bool DeleteRole(string roleName, bool throwOnPopulatedRole)
        {
            throw new NotImplementedException();
        }

        public override string[] FindUsersInRole(string roleName, string usernameToMatch)
        {
            throw new NotImplementedException();
        }

        public override string[] GetAllRoles()
        {
            throw new NotImplementedException();
        }

        public override string[] GetRolesForUser(string username)
        {
            using (var context = new DeltaDbContext())
            {
                var roles = new string[] { };
                var user = context.Users.FirstOrDefault(u => u.EmailAddress == username);

                if (user != null)
                {
                    roles = user.Roles.Select(x => x.Name).ToArray();
                }

                return roles;    
            }
        }

        public override string[] GetUsersInRole(string roleName)
        {
            throw new NotImplementedException();
        }

        public override bool IsUserInRole(string username, string roleName)
        {
            var user = _serverService.Find<User>(new Specification<User>(x => x.EmailAddress.Equals(username))).FirstOrDefault();

            return user.Roles.Any(x => x.Name.Equals(roleName));
        }

        public override void RemoveUsersFromRoles(string[] usernames, string[] roleNames)
        {
            throw new NotImplementedException();
        }

        public override bool RoleExists(string roleName)
        {
            throw new NotImplementedException();
        }
    }
}
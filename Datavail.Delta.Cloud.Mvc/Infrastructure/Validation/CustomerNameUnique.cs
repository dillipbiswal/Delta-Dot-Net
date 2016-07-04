using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;
using Datavail.Delta.Application.Interface;

namespace Datavail.Delta.Cloud.Mvc.Infrastructure.Validation
{
    public sealed class CustomerNameUnique : ValidationAttribute
    {
        private IServerService _serverService;

        public CustomerNameUnique(IServerService serverService)
        {
            _serverService = serverService;
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            return null;
        }
    }
}
using System;
using Datavail.Delta.Domain;
using Datavail.Delta.Repository.Interface;

namespace Datavail.Delta.Repository.Mock
{
    public class CustomerRepository : RepositoryBase<Customer>, ICustomerRepository
    {
        public void CreateTestData()
        {
            var guids = new[]
                            {
                                new Guid("{B87F0263-0CEB-4B80-BCBA-472F9B8510A5}"),
                                new Guid("{4983AD78-7DB5-4B18-8842-7115EDC4C606}"),
                                new Guid("{74F23C4A-EC2C-43E9-A222-8C80CE5086D0}"),
                            };

            for (var i = 0; i < 3; i++)
            {
                var tenant = Tenant.NewTenant("Test Tenant");
                var customer = Customer.NewCustomer(tenant,"Customer" + i);
                customer.GetType().GetProperty("Id").SetValue(customer, guids[i], null);

                if (i % 2 == 0)
                {
                    customer.Status = CustomerStatus.Hold;
                }

                EntityList.Add(customer);
            }
        }
    }
}
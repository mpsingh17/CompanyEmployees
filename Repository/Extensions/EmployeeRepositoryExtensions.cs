using Entities.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Linq.Dynamic.Core;

namespace Repository.Extensions
{
    public static class EmployeeRepositoryExtensions
    {
        public static IQueryable<Employee> Sort(this IQueryable<Employee> employees, string orderByQueryString)
        {
            if (string.IsNullOrWhiteSpace(orderByQueryString))
                return employees.OrderBy(e => e.Name);

            var orderByParams = orderByQueryString.Trim().Split(",");

            var propertyInfos = typeof(Employee).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            var orderByQueryBuilder = new StringBuilder();

            foreach (var param in orderByParams)
            {
                if (string.IsNullOrWhiteSpace(param))
                    continue;

                var orderByPropertyNameInQueryString = param.Trim().Split(" ")[0];

                var orderByPropertyName = propertyInfos.FirstOrDefault(
                    pi => pi.Name.Equals(orderByPropertyNameInQueryString, StringComparison.InvariantCultureIgnoreCase)
                );

                if (orderByPropertyName == null)
                    continue;

                var direction = param.EndsWith(" desc") ? "descending" : "ascending";

                orderByQueryBuilder.Append($"{orderByPropertyName.Name} {direction}, ");
            }

            var orderQuery = orderByQueryBuilder.ToString().TrimEnd(',', ' ');

            if (string.IsNullOrWhiteSpace(orderQuery))
                return employees.OrderBy(e => e.Name);

            return employees.OrderBy(orderQuery);
        }
    }
}

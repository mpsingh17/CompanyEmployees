using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Repository.Extensions.Utility
{
    public static class OrderByQueryBuilder
    {
        public static string CreateOderByQuery<T>(string orderByQueryString)
        {
            var propertyInfos = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            var orderByQueryBuilder = new StringBuilder();

            var orderByParams = orderByQueryString.Trim().Split(",");

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

            return orderByQueryBuilder.ToString().TrimEnd(',', ' ');
        }
    }
}

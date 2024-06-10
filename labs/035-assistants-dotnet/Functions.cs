using System.Data;
using System.Text;
using Dapper;
using Microsoft.Data.SqlClient;
using OpenAI.Assistants;
using OpenAI.Chat;

namespace AssistantsDotNet;

public static class Functions
{
    public static readonly FunctionToolDefinition GetCustomersFunctionDefinition = new()
    {
        FunctionName = "getCustomers",
        Description = "Gets a filtered list of customers. At least one filter MUST be provided in the parameters. The result list is limited to 25 customers.",
        Parameters = JsonHelpers.FromObjectAsJson(
            new
            {
                Type = "object",
                Properties = new
                {
                    CustomerID = new { Type = "integer", Description = "Optional filter for the customer ID." },
                    FirstName = new { Type = "string", Description = "Optional filter for the first name (true if first name contains filter value)." },
                    MiddleName = new { Type = "string", Description = "Optional filter for the middle name (true if middle name contains filter value)." },
                    LastName = new { Type = "string", Description = "Optional filter for the last name (true if last name contains filter value)." },
                    CompanyName = new { Type = "string", Description = "Optional filter for the company name (true if company name contains filter value)." }
                },
                Required = Array.Empty<string>()
            })
    };

    public class GetCustomersParameters
    {
        public int? CustomerID { get; set; }
        public string? FirstName { get; set; }
        public string? MiddleName { get; set; }
        public string? LastName { get; set; }
        public string? CompanyName { get; set; }
    }

    public class Customer
    {
        public int CustomerID { get; set; }
        public string? FirstName { get; set; }
        public string? MiddleName { get; set; }
        public string? LastName { get; set; }
        public string? CompanyName { get; set; }
    }

    public static async Task<IEnumerable<Customer>> GetCustomers(SqlConnection connection, GetCustomersParameters filter)
    {
        if (!filter.CustomerID.HasValue && string.IsNullOrEmpty(filter.FirstName) && string.IsNullOrEmpty(filter.MiddleName) && string.IsNullOrEmpty(filter.LastName) && string.IsNullOrEmpty(filter.CompanyName))
        {
            throw new Exception("At least one filter must be provided.");
        }

        var query = new StringBuilder("SELECT TOP 25 CustomerID, FirstName, MiddleName, LastName, CompanyName FROM SalesLT.Customer WHERE CustomerID >= 29485");
        var parameters = new DynamicParameters();

        if (filter.CustomerID.HasValue)
        {
            query.Append(" AND CustomerID = @customerID");
            parameters.Add("customerID", filter.CustomerID.Value, DbType.Int32);
        }
        if (!string.IsNullOrEmpty(filter.FirstName))
        {
            query.Append(" AND FirstName LIKE '%' + @firstName + '%'");
            parameters.Add("firstName", filter.FirstName, DbType.String);
        }
        if (!string.IsNullOrEmpty(filter.MiddleName))
        {
            query.Append(" AND MiddleName LIKE '%' + @middleName + '%'");
            parameters.Add("middleName", filter.MiddleName, DbType.String);
        }
        if (!string.IsNullOrEmpty(filter.LastName))
        {
            query.Append(" AND LastName LIKE '%' + @lastName + '%'");
            parameters.Add("lastName", filter.LastName, DbType.String);
        }
        if (!string.IsNullOrEmpty(filter.CompanyName))
        {
            query.Append(" AND CompanyName LIKE '%' + @companyName + '%'");
            parameters.Add("companyName", filter.CompanyName, DbType.String);
        }

        Console.WriteLine($"Executing query: {query}");

        var result = await connection.QueryAsync<Customer>(query.ToString(), parameters);

        return result;
    }

    public static readonly FunctionToolDefinition GetProductsFunctionDefinition = new()
    {
        FunctionName = "getProducts",
        Description = "Gets a filtered list of products. At least one filter MUST be provided in the parameters. The result list is limited to 25 products.",
        Parameters = JsonHelpers.FromObjectAsJson(
            new
            {
                Type = "object",
                Properties = new
                {
                    ProductID = new { Type = "integer", Description = "Optional filter for the product ID." },
                    Name = new { Type = "string", Description = "Optional filter for the product name (true if product name contains filter value)." },
                    ProductNumber = new { Type = "string", Description = "Optional filter for the product number." }
                },
                Required = Array.Empty<string>()
            })
    };

    public class GetProductsParameters
    {
        public int? ProductID { get; set; }
        public string? Name { get; set; }
        public string? ProductNumber { get; set; }
    }

    public class Product
    {
        public int ProductID { get; set; }
        public string? Name { get; set; }
        public string? ProductNumber { get; set; }
        public int ProductCategoryID { get; set; }
    }

    public static async Task<IEnumerable<Product>> GetProducts(SqlConnection connection, GetProductsParameters filter)
    {
        if (!filter.ProductID.HasValue && string.IsNullOrEmpty(filter.Name) && string.IsNullOrEmpty(filter.ProductNumber))
        {
            throw new Exception("At least one filter must be provided.");
        }

        var query = new StringBuilder("SELECT TOP 25 ProductID, Name, ProductNumber, ProductCategoryID FROM SalesLT.Product WHERE 1 = 1");
        var parameters = new DynamicParameters();

        if (filter.ProductID.HasValue)
        {
            query.Append(" AND ProductID = @productID");
            parameters.Add("productID", filter.ProductID.Value, DbType.Int32);
        }
        if (!string.IsNullOrEmpty(filter.Name))
        {
            query.Append(" AND Name LIKE '%' + @name + '%'");
            parameters.Add("name", filter.Name, DbType.String);
        }
        if (!string.IsNullOrEmpty(filter.ProductNumber))
        {
            query.Append(" AND ProductNumber = @productNumber");
            parameters.Add("productNumber", filter.ProductNumber, DbType.String);
        }

        Console.WriteLine($"Executing query: {query}");

        var result = await connection.QueryAsync<Product>(query.ToString(), parameters);

        return result;
    }

    public static readonly FunctionToolDefinition GetCustomerProductsRevenueFunctionDefinition = new()
    {
        FunctionName = "getCustomerProductsRevenue",
        Description = "Gets the revenue of the customer and products. The result is ordered by the revenue in descending order. The result list is limited to 25 records.",
        Parameters = JsonHelpers.FromObjectAsJson(
            new
            {
                Type = "object",
                Properties = new
                {
                    CustomerID = new { Type = "integer", Description = "Optional filter for the customer ID." },
                    ProductID = new { Type = "integer", Description = "Optional filter for the product ID." },
                    Year = new { Type = "integer", Description = "Optional filter for the year." },
                    Month = new { Type = "integer", Description = "Optional filter for the month." },
                    GroupByCustomer = new { Type = "boolean", Description = "If true, revenue is grouped by customer ID." },
                    GroupByProduct = new { Type = "boolean", Description = "If true, revenue is grouped by product ID." },
                    GroupByYear = new { Type = "boolean", Description = "If true, revenue is grouped by year." },
                    GroupByMonth = new { Type = "boolean", Description = "If true, revenue is grouped by month." }
                },
                Required = Array.Empty<string>()
            })
    };

    public class GetCustomerProductsRevenueParameters
    {
        public int? CustomerID { get; set; }
        public int? ProductID { get; set; }
        public int? Year { get; set; }
        public int? Month { get; set; }
        public bool? GroupByCustomer { get; set; }
        public bool? GroupByProduct { get; set; }
        public bool? GroupByYear { get; set; }
        public bool? GroupByMonth { get; set; }
    }

    public class CustomerProductsRevenue
    {
        public decimal Revenue { get; set; }
        public int? CustomerID { get; set; }
        public int? ProductID { get; set; }
        public int? Year { get; set; }
        public int? Month { get; set; }
    }

    public static async Task<List<CustomerProductsRevenue>> GetCustomerProductsRevenue(SqlConnection connection, GetCustomerProductsRevenueParameters filter)
    {
        var query = new StringBuilder("SELECT TOP 25 SUM(LineTotal) AS Revenue");
        var parameters = new DynamicParameters();

        if (filter.GroupByCustomer.HasValue && filter.GroupByCustomer.Value) { query.Append(", CustomerID"); }
        if (filter.GroupByProduct.HasValue && filter.GroupByProduct.Value) { query.Append(", ProductID"); }
        if (filter.GroupByYear.HasValue && filter.GroupByYear.Value) { query.Append(", YEAR(OrderDate) AS Year"); }
        if (filter.GroupByMonth.HasValue && filter.GroupByMonth.Value) { query.Append(", MONTH(OrderDate) AS Month"); }

        query.Append(" FROM SalesLT.SalesOrderDetail d INNER JOIN SalesLT.SalesOrderHeader h ON d.SalesOrderID = h.SalesOrderID WHERE 1 = 1");

        if (filter.CustomerID.HasValue)
        {
            query.Append(" AND CustomerID = @customerID");
            parameters.Add("customerID", filter.CustomerID.Value, DbType.Int32);
        }
        if (filter.ProductID.HasValue)
        {
            query.Append(" AND ProductID = @productID");
            parameters.Add("productID", filter.ProductID.Value, DbType.Int32);
        }
        if (filter.Year.HasValue)
        {
            query.Append(" AND YEAR(OrderDate) = @year");
            parameters.Add("year", filter.Year.Value, DbType.Int32);
        }
        if (filter.Month.HasValue)
        {
            query.Append(" AND MONTH(OrderDate) = @month");
            parameters.Add("month", filter.Month.Value, DbType.Int32);
        }

        if (filter.GroupByCustomer.HasValue || filter.GroupByProduct.HasValue || filter.GroupByYear.HasValue || filter.GroupByMonth.HasValue)
        {
            var groupColumns = new List<string>();
            if (filter.GroupByCustomer.HasValue && filter.GroupByCustomer.Value) { groupColumns.Add("CustomerID"); }
            if (filter.GroupByProduct.HasValue && filter.GroupByProduct.Value) { groupColumns.Add("ProductID"); }
            if (filter.GroupByYear.HasValue && filter.GroupByYear.Value) { groupColumns.Add("YEAR(OrderDate)"); }
            if (filter.GroupByMonth.HasValue && filter.GroupByMonth.Value) { groupColumns.Add("MONTH(OrderDate)"); }
            query.Append($" GROUP BY {string.Join(", ", groupColumns)}");
        }

        query.Append(" ORDER BY SUM(LineTotal) DESC");

        Console.WriteLine($"Executing query: {query.ToString()}");

        var result = await connection.QueryAsync<CustomerProductsRevenue>(query.ToString(), parameters);

        return result.ToList();
    }
}

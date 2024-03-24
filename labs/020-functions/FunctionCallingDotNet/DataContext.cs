using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;

namespace FunctionCallingDotNet;

class ApplicationDataContext(DbContextOptions options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductCategory> ProductCategories => Set<ProductCategory>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<SalesOrderHeader> SalesOrderHeaders => Set<SalesOrderHeader>();
    public DbSet<SalesOrderDetail> SalesOrderDetails => Set<SalesOrderDetail>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(entity => entity.ToTable("Product", "SalesLT"));
        modelBuilder.Entity<ProductCategory>(entity => entity.ToTable("ProductCategory", "SalesLT"));
        modelBuilder.Entity<Customer>(entity => entity.ToTable("Customer", "SalesLT"));
        modelBuilder.Entity<SalesOrderHeader>(entity => entity.ToTable("SalesOrderHeader", "SalesLT"));
        modelBuilder.Entity<SalesOrderDetail>(entity =>
        {
            entity.ToTable("SalesOrderDetail", "SalesLT");
            entity.HasKey(nameof(SalesOrderDetail.SalesOrderDetailID), nameof(SalesOrderDetail.SalesOrderID));
        });
    }

    /// <summary>
    /// Get filtered list of customers.
    /// </summary>
    /// <param name="filter">Filter for retrieving customers</param>
    /// <returns>
    /// Array of customers matching the filter. A maximum of 25 customers are returned.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no filter is provided in <paramref name="filter"/>.
    /// </exception>
    public async Task<Customer[]> GetCustomers(CustomerFilter filter)
    {
        if (filter.CustomerID is null && filter.FirstName is null && filter.MiddleName is null
            && filter.LastName is null && filter.CompanyName is null)
        {
            throw new InvalidOperationException("At least one filter must be provided.");
        }

        var query = Customers.AsNoTracking().AsQueryable();
        if (filter.CustomerID is not null)
        {
            query = query.Where(c => c.CustomerID == filter.CustomerID);
        }
        if (filter.FirstName is not null)
        {
            query = query.Where(c => c.FirstName.Contains(filter.FirstName));
        }
        if (filter.MiddleName is not null)
        {
            query = query.Where(c => c.MiddleName!.Contains(filter.MiddleName));
        }
        if (filter.LastName is not null)
        {
            query = query.Where(c => c.LastName.Contains(filter.LastName));
        }
        if (filter.CompanyName is not null)
        {
            query = query.Where(c => c.CompanyName!.Contains(filter.CompanyName));
        }

        var result = await query.Take(25).ToArrayAsync();
        return result;
    }

    /// <summary>
    /// Get filtered list of products.
    /// </summary>
    /// <param name="filter">Filter for retrieving products</param>
    /// <returns>
    /// Array of products matching the filter. A maximum of 25 products are returned.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no filter is provided in <paramref name="filter"/>.
    /// </exception>
    /// <remarks>
    /// If you specify a filter on product category ID, the filter returns all
    /// products that belong to the specified category or any of its subcategories.
    /// </remarks>
    public async Task<Product[]> GetProducts(ProductFilter filter)
    {
        if (filter.ProductID is null && filter.Name is null && filter.ProductNumber is null
            && filter.ProductCategoryID is null)
        {
            throw new InvalidOperationException("At least one filter must be provided.");
        }

        var query = Products.AsNoTracking().AsQueryable();
        if (filter.ProductID is not null)
        {
            query = query.Where(p => p.ProductID == filter.ProductID);
        }
        if (filter.Name is not null)
        {
            query = query.Where(p => p.Name.Contains(filter.Name));
        }
        if (filter.ProductNumber is not null)
        {
            query = query.Where(p => p.ProductNumber.Contains(filter.ProductNumber));
        }
        if (filter.ProductCategoryID is not null)
        {
            var productCategoryIDs = await GetAllProductCategoryIDs(filter.ProductCategoryID.Value);
            query = query.Where(p => productCategoryIDs.Contains(p.ProductCategoryID!.Value));
        }

        return await query.Take(25).ToArrayAsync();
    }

    private async Task<List<int>> GetAllProductCategoryIDs(int parentProductCategoryID)
    {
        if (!await ProductCategories.AsNoTracking().AnyAsync(pc => pc.ProductCategoryID == parentProductCategoryID))
        {
            throw new InvalidOperationException("There is no product category with the specified ID.");
        }

        var result = new List<int> { parentProductCategoryID };
        var childProductCategories = await ProductCategories.AsNoTracking()
            .Where(pc => pc.ParentProductCategoryID == parentProductCategoryID)
            .Select(pc => pc.ProductCategoryID)
            .ToArrayAsync();
        foreach (var childProductCategoryID in childProductCategories)
        {
            result.AddRange(await GetAllProductCategoryIDs(childProductCategoryID));
        }
        return result;
    }

    /// <summary>
    /// Get revenue statistics for a customer.
    /// </summary>
    /// <param name="filter">Filter for retrieving revenue statistics</param>
    /// <returns>
    /// Array of revenue statistics for the customer.
    /// </returns>
    public async Task<CustomerRevenueStats[]> GetCustomerRevenueStats(CustomerRevenueFilter filter)
    {
        var query = SalesOrderDetails.AsNoTracking().AsQueryable();

        if (filter.CustomerID is not null)
        {
            query = query.Where(sod => sod.SalesOrder!.CustomerID == filter.CustomerID);
        }

        var result = query
            .GroupBy(sod => new { sod.SalesOrder!.CustomerID, sod.ProductID, sod.SalesOrder!.OrderDate.Year, sod.SalesOrder!.OrderDate.Month })
            .Select(g => new CustomerRevenueStats(
                g.Key.CustomerID,
                g.Key.Year,
                g.Key.Month,
                g.Sum(sod => sod.LineTotal)
            ));

        return await result.ToArrayAsync();

    }
}

record CustomerFilter(
    int? CustomerID,
    string? FirstName,
    string? MiddleName,
    string? LastName,
    string? CompanyName
);

record ProductFilter(
    int? ProductID,
    string? Name,
    string? ProductNumber,
    int? ProductCategoryID
);

record CustomerRevenueFilter(
    int? CustomerID
);

record CustomerRevenueStats(
    int CustomerID,
    int Year,
    int Month,
    decimal TotalRevenue
);

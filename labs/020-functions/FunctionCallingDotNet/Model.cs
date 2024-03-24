using System.ComponentModel.DataAnnotations;

namespace FunctionCallingDotNet;

// Simplified model for AdventureWorksLT

class Product
{
    public int ProductID { get; set; }
    public string Name { get; set; } = "";
    public string ProductNumber { get; set; } = "";
    public int Color { get; set; }
    public decimal ListPrice { get; set; }
    public int? ProductCategoryID { get; set; }
    public ProductCategory? ProductCategory { get; set; }
    public List<SalesOrderDetail> SalesOrderDetails { get; set; } = [];
}

class ProductCategory
{
    public int ProductCategoryID { get; set; }
    public string Name { get; set;} = "";
    public int? ParentProductCategoryID { get; set; }
    public ProductCategory? ParentProductCategory { get; set; }
    public List<Product> Products { get; set; } = [];
}

class Customer
{
    public int CustomerID { get; set; }
    public string FirstName { get; set; } = "";
    public string? MiddleName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string? CompanyName { get; set; } = "";
    public List<SalesOrderHeader> SalesOrderHeaders { get; set; } = [];
}

class SalesOrderHeader
{
    [Key]
    public int SalesOrderID { get; set; }
    public int CustomerID { get; set; }
    public Customer? Customer { get; set; }
    public DateTime OrderDate { get; set; }
    public List<SalesOrderDetail> SalesOrderDetails { get; set; } = [];
}

class SalesOrderDetail
{
    public int SalesOrderDetailID { get; set; }
    public int SalesOrderID { get; set; }
    public SalesOrderHeader? SalesOrder { get; set; }
    public int ProductID { get; set; }
    public Product? Product { get; set; }
    public int OrderQty { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal UnitPriceDiscount { get; set; }
    public decimal LineTotal { get; set; }
}

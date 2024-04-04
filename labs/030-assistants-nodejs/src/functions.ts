import sql from 'mssql';
import winston from 'winston';

export const getCustomersFunctionDefinition = {
    name: 'getCustomers',
    description: 'Gets a filtered list of customers. At least one filter MUST be provided in the parameters. The result list is limited to 25 customer.',
    parameters: {
        type: 'object',
        properties: {
            customerID: { type: 'integer', description: 'Optional filter for the customer ID.' },
            firstName: { type: 'string', description: 'Optional filter for the first name (true if first name contains filter value).' },
            middleName: { type: 'string', description: 'Optional filter for the middle name (true if middle name contains filter value).' },
            lastName: { type: 'string', description: 'Optional filter for the last name (true if last name contains filter value).' },
            companyName: { type: 'string', description: 'Optional filter for the company name (true if company name contains filter value).' }
        },
        required: []
    }
};

export type GetCustomersParameters = {
    customerID?: number;
    firstName?: string;
    middleName?: string;
    lastName?: string;
    companyName?: string;
};

export type Customer = {
    customerID: number;
    firstName: string;
    middleName?: string;
    lastName: string;
    companyName?: string;
};

export async function getCustomers(pool: sql.ConnectionPool, filter: GetCustomersParameters, logger: winston.Logger | undefined): Promise<Customer[]> {
    if (!filter.customerID && !filter.firstName && !filter.middleName && !filter.lastName && !filter.companyName) {
        throw new Error('At least one filter must be provided.');
    }

    const request = pool.request();
    let query = `SELECT TOP 25 CustomerID, FirstName, MiddleName, LastName, CompanyName FROM SalesLT.Customer WHERE CustomerID >= 29485`;
    if (filter.customerID) {
        query += ' AND CustomerID = @customerID';
        request.input('customerID', sql.Int, filter.customerID);
    }
    if (filter.firstName) {
        query += ' AND FirstName LIKE \'%\' + @firstName + \'%\'';
        request.input('firstName', sql.NVarChar, filter.firstName);
    }
    if (filter.middleName) {
        query += ' AND MiddleName LIKE \'%\' + @middleName + \'%\'';
        request.input('middleName', sql.NVarChar, filter.middleName);
    }
    if (filter.lastName) {
        query += ' AND LastName LIKE \'%\' + @lastName + \'%\'';
        request.input('lastName', sql.NVarChar, filter.lastName);
    }
    if (filter.companyName) {
        query += ' AND CompanyName LIKE \'%\' + @companyName + \'%\'';
        request.input('companyName', sql.NVarChar, filter.companyName);
    }

    logger?.info('Executing query', { query });
    const result = await request.query(query);

    return result.recordset as Customer[];
}

export const getProductsFunctionDefinition = {
    name: 'getProducts',
    description: 'Gets a filtered list of products. At least one filter MUST be provided in the parameters. The result list is limited to 25 products.',
    parameters: {
        type: 'object',
        properties: {
            productID: { type: 'integer', description: 'Optional filter for the product ID.' },
            name: { type: 'string', description: 'Optional filter for the product name (true if product name contains filter value).' },
            productNumber: { type: 'string', description: 'Optional filter for the product number.' }
        },
        required: []
    }
};

export type GetProductsParameters = {
    productID?: number;
    name?: string;
    productNumber?: string;
};

export type Product = {
    productID: number;
    name: string;
    productNumber: string;
};

export async function getProducts(pool: sql.ConnectionPool, filter: GetProductsParameters, logger: winston.Logger | undefined): Promise<Product[]> {
    if (!filter.productID && !filter.name && !filter.productNumber) {
        throw new Error('At least one filter must be provided.');
    }

    const request = pool.request();
    let query = `SELECT TOP 25 ProductID, Name, ProductNumber, ProductCategoryID FROM SalesLT.Product WHERE 1 = 1`;
    if (filter.productID) {
        query += ' AND ProductID = @productID';
        request.input('productID', sql.Int, filter.productID);
    }
    if (filter.name) {
        query += ' AND Name LIKE \'%\' + @name + \'%\'';
        request.input('name', sql.NVarChar, filter.name);
    }
    if (filter.productNumber) {
        query += ' AND ProductNumber = @productNumber';
        request.input('productNumber', sql.NVarChar, filter.productNumber);
    }

    logger?.info('Executing query', { query });
    const result = await request.query(query);

    return result.recordset as Product[];
}


export const getCustomerProductsRevenueFunctionDefinition = {
    name: 'getCustomerProductsRevenue',
    description: 'Gets the revenue of the customer and products. The result is ordered by the revenue in descending order. The result list is limited to 25 records.',
    parameters: {
        type: 'object',
        properties: {
            customerID: { type: 'integer', description: 'Optional filter for the customer ID.' },
            productID: { type: 'integer', description: 'Optional filter for the product ID.' },
            year: { type: 'integer', description: 'Optional filter for the year.' },
            month: { type: 'integer', description: 'Optional filter for the month.' },
            groupByCustomer: { type: 'boolean', description: 'If true, revenue is grouped by customer ID.' },
            groupByProduct: { type: 'boolean', description: 'If true, revenue is grouped by product ID.' },
            groupByYear: { type: 'boolean', description: 'If true, revenue is grouped by year.' },
            groupByMonth: { type: 'boolean', description: 'If true, revenue is grouped by month.' }
        },
        required: []
    }

};

export type GetCustomerProductsRevenueParameters = {
    customerID?: number;
    productID?: number;
    year?: number;
    month?: number;
    groupByCustomer?: number;
    groupByProduct?: number;
    groupByYear?: number;
    groupByMonth?: number;
}

export type CustomerProductsRevenue = {
    revenue: number;
    customerID?: number;
    productID?: number;
    year?: number;
    month?: number;
}

export async function getCustomerProductsRevenue(pool: sql.ConnectionPool, filter: GetCustomerProductsRevenueParameters, logger: winston.Logger | undefined)
    : Promise<CustomerProductsRevenue[]> {
    const request = pool.request();

    let query = `SELECT TOP 25 SUM(LineTotal) AS Revenue`;
    if (filter.groupByCustomer) { query += ', CustomerID'; }
    if (filter.groupByProduct) { query += ', ProductID'; }
    if (filter.groupByYear) { query += ', YEAR(OrderDate) AS Year'; }
    if (filter.groupByMonth) { query += ', MONTH(OrderDate) AS Month'; }

    query += ' FROM SalesLT.SalesOrderDetail d INNER JOIN SalesLT.SalesOrderHeader h ON d.SalesOrderID = h.SalesOrderID WHERE 1 = 1';

    if (filter.customerID) {
        query += ' AND CustomerID = @customerID';
        request.input('customerID', sql.Int, filter.customerID);
    }
    if (filter.productID) {
        query += ' AND ProductID = @productID';
        request.input('productID', sql.Int, filter.productID);
    }
    if (filter.year) {
        query += ' AND YEAR(OrderDate) = @year';
        request.input('year', sql.Int, filter.year);
    }
    if (filter.month) {
        query += ' AND MONTH(OrderDate) = @month';
        request.input('month', sql.Int, filter.month);
    }

    if (filter.groupByCustomer || filter.groupByProduct || filter.groupByYear || filter.groupByMonth) {
        const groupColumns = [];
        if (filter.groupByCustomer) { groupColumns.push('CustomerID'); }
        if (filter.groupByProduct) { groupColumns.push('ProductID'); }
        if (filter.groupByYear) { groupColumns.push('YEAR(OrderDate)'); }
        if (filter.groupByMonth) { groupColumns.push('MONTH(OrderDate)'); }
        query += ` GROUP BY ${groupColumns.join(', ')}`;
    }

    query += ' ORDER BY SUM(LineTotal) DESC';

    logger?.info('Executing query', { query });
    const result = await request.query(query);

    return result.recordset as CustomerProductsRevenue[];
}

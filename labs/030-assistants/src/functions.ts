import sql from 'mssql';

export const getCustomersFunctionDefinition = {
    name: 'getCustomers',
    description: 'Gets a filtered list of customers. At least one filter MUST be provided in the parameters. The result list is limited to 25 customer.',
    parameters: {
        type: 'object',
        properties: {
            customerID: { type: 'integer', description: 'Optional filter for the customer ID.' },
            firstName: { type: 'string', description: 'Optional filter for the first name.' },
            middleName: { type: 'string', description: 'Optional filter for the middle name.' },
            lastName: { type: 'string', description: 'Optional filter for the last name.' },
            companyName: { type: 'string', description: 'Optional filter for the company name.' }
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

export async function getCustomers(pool: sql.ConnectionPool, filter: GetCustomersParameters): Promise<Customer[]> {
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

    const result = await request.query(query);

    return result.recordset as Customer[];
}
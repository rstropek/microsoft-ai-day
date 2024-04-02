import sql from 'mssql';

export type ProductModel = {
    productModelID: number;
    name: string;
    description: string;
    productGroupDescription1: string;
    productGroupDescription2: string;
}

export async function getProductModels(pool: sql.ConnectionPool): Promise<ProductModel[]> {
    const result = await pool.request().query(`select pm.ProductModelID as productModelID, pm.Name as name, pd.[Description] as description,
        (
            select top 1 pg.Name
            from SalesLT.Product p 
            inner join SalesLT.ProductCategory pg on p.ProductCategoryID = pg.ProductCategoryID
            where pm.ProductModelID = p.ProductModelID
        ) as productGroupDescription1,
        (
            select top 1 pg2.Name
            from SalesLT.Product p 
            inner join SalesLT.ProductCategory pg on p.ProductCategoryID = pg.ProductCategoryID
            inner join SalesLT.ProductCategory pg2 on pg.ParentProductCategoryID = pg2.ProductCategoryID
            where pm.ProductModelID = p.ProductModelID
        ) as productGroupDescription2
        from SalesLT.ProductModel pm
        inner join SalesLT.ProductModelProductDescription pmpd on pmpd.ProductModelID = pm.ProductModelID and pmpd.Culture = 'en'
        inner join SalesLT.ProductDescription pd on pmpd.ProductDescriptionID = pd.ProductDescriptionID
        where exists (
            select top 1 1
            from SalesLT.Product p 
            where pm.ProductModelID = p.ProductModelID
        )`);

    
    return result.recordset as ProductModel[];
}

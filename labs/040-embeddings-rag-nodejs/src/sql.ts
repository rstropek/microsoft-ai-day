import sql from 'mssql';

/**
 * Creates a connection pool to the Azure SQL Server database and connects to it
 */
export async function createConnectionPool(connectionString: string): Promise<sql.ConnectionPool> {
    if (!connectionString) {
        throw new Error('Connection string is required');
    }

    // Parse the connection string as we need server, user, etc. separately
    const connectionStringParts = new Map<string, string>(
        connectionString.split(';').map(p => p.split('=') as [string, string])
    );
    let server = connectionStringParts.get('Server') ?? '';
    if (server.startsWith('tcp:')) {
        server = server.substring(4);
    }
    if (server.endsWith(',1433')) {
        server = server.substring(0, server.length - 5);
    }

    const config: sql.config = {
        user: connectionStringParts.get('User ID') ?? '',
        password: connectionStringParts.get('Password') ?? '',
        server: server,
        database: connectionStringParts.get('Initial Catalog') ?? '',
        options: {
            encrypt: true,
            trustServerCertificate: false
        }
    };
    const pool = new sql.ConnectionPool(config);

    await pool.connect();

    return pool;
}

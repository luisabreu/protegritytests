using System.Data;
using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using Microsoft.Data.SqlClient;
using NHibernate;
using NHibernate.Driver;

namespace Demo;

public class NhTest {
    private static readonly string _cnnString = "Data source=ws-windows1001; initial catalog=protegritytests; user id=nopermissions_user; password=d3mo_u5er";

    // set connection release mode to on_close so that tran does not "close" the connection (set in kill state)
    private readonly ISessionFactory _factory = Fluently.Configure( )
                                                        .Database(MsSqlConfiguration.MsSql2012.ConnectionString(_cnnString).Driver<MicrosoftDataSqlClientDriver>( ).ShowSql( ))
                                                        .Mappings(m => m.FluentMappings.AddFromAssemblyOf<Employee>( ))
                                                        .ExposeConfiguration( cfg => cfg.SetProperty("connection.release_mode", "on_close"))
                                                        .BuildSessionFactory( );

    public async Task<int> SearchEmployeeIdByNameAsync(string name) {
        using var session = _factory.OpenSession();
        
        var cookie = await NhSqlAppRoleHelper.SetApplicationRoleAsync(session, "read_employees", "Test_123");

        var tran = session.BeginTransaction( );

        const string sql = "select employeeId from employees where name like :name";

        var employeeId = await session.CreateSQLQuery(sql)
                                      .SetString("name", $"{name}%")
                                      .FutureValue<int>( )
                                      .GetValueAsync( );
        await tran.CommitAsync( );
        
        await NhSqlAppRoleHelper.UnsetApplicationRoleAsync(session, cookie);
        return employeeId;
    }


    public async Task<int> SaveEployeeAsync() {
        using var session = _factory.OpenSession( );
        
        var id = DateTime.Now.Ticks;
        var employee = new Employee {
                                        Username = $"User{id}",
                                        Name = $"User {id}",
                                        Contacts = new List<Contact> {
                                                                         new( ) { Value = "123123123", ContactKind = ContactKind.Phone },
                                                                         new( ) { Value = "test@mail.pt", ContactKind = ContactKind.Email }
                                                                     }
                                    };

        var cookie = await NhSqlAppRoleHelper.SetApplicationRoleAsync(session, "write_employees", "Test_123");
        
        var tran = session.BeginTransaction( );
        
        await session.SaveOrUpdateAsync(employee);
        await tran.CommitAsync( );
        
        await NhSqlAppRoleHelper.UnsetApplicationRoleAsync(session, cookie);

        return employee.EmployeeId;
    }
 
    

    public async Task<Employee> LoadEmployeeAsync(int employeeId) {
        using var session = _factory.OpenSession( );
        
        var cookie = await NhSqlAppRoleHelper.SetApplicationRoleAsync(session, "write_employees", "Test_123");

        using var tran = session.BeginTransaction( );

        var employee = await session.GetAsync<Employee>(employeeId);

        await tran.CommitAsync( );
        
        await NhSqlAppRoleHelper.UnsetApplicationRoleAsync(session, cookie);

        return employee;
    }
}

/// <summary>
/// Helper for setting up app role and cookies
/// </summary>
public static class NhSqlAppRoleHelper {
    /// <summary>
    /// Setup app role that should be used in the current connection
    /// </summary>
    /// <param name="session"><see cref="ISession"/> that has the connection that should be updated</param>
    /// <param name="role">SQL app role that should be used for the connection</param>
    /// <param name="password">SQL app role's password</param>
    /// <remarks>Can't be called inside a transaction and if reusing the connection, then must change connection release mode to on_close</remarks>
    /// <returns>Returns the cookie that identifies the role that has been set</returns>
    public static async Task<byte[]> SetApplicationRoleAsync(ISession session, string role, string password) {
        await using var cmd = session.Connection.CreateCommand( );
        //tran.Enlist(cmd);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.CommandText = "sp_setapprole";
        cmd.Parameters.Add(new SqlParameter("@rolename", role));
        cmd.Parameters.Add(new SqlParameter("@password", password));
        cmd.Parameters.Add(new SqlParameter("@fCreateCookie", true));
        cmd.Parameters.Add(new SqlParameter {
                                                ParameterName = "@cookie",
                                                DbType = DbType.Binary,
                                                Direction = ParameterDirection.Output,
                                                Size = 8000
                                            });

        await cmd.ExecuteNonQueryAsync( );
        return (byte[])cmd.Parameters["@cookie"].Value;
    }

    /// <summary>
    /// Clear the sql app role that has been used
    /// </summary>
    /// <param name="session"><see cref="ISession"/> that has the connection that should be updated</param>
    /// <param name="cookie">cookie that identifies the current role</param>
    /// <remarks>Can't be called inside a transaction and if reusing the connection, then must change connection release mode to on_close</remarks>
    public static async Task UnsetApplicationRoleAsync(ISession session,  byte[] cookie) {
        var cnn = session.Connection as SqlConnection;

        await using var cmd = cnn.CreateCommand( );
        cmd.CommandText = "sp_unsetapprole";
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.Add(new SqlParameter("@cookie", cookie));
        await cmd.ExecuteNonQueryAsync( );
    }
    
}
/*
public class NHTestConnectionProvider : MicrosoftDataSqlClientDriver {
    public override DbConnection CreateConnection() {
        var cnn =  base.CreateConnection( );

        return cnn;
    }
    
    private void SetAppRole()
}*/

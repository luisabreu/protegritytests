using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using NHibernate;
using NHibernate.Driver;

namespace Demo;

public class NhTest {
    // set connection release mode to on_close so that tran does not "close" the connection (set in kill state)
    private readonly ISessionFactory _factory = Fluently.Configure( )
                                                        .Database(MsSqlConfiguration.MsSql2012
                                                                                    .ConnectionString(SqlConfiguration.CnnString)
                                                                                    .Driver<MicrosoftDataSqlClientDriver>( )
                                                                                    .ShowSql( ))
                                                        .Mappings(m => m.FluentMappings.AddFromAssemblyOf<Employee>( ))
                                                        .ExposeConfiguration( cfg => cfg.SetProperty("connection.release_mode", "on_close"))
                                                        .BuildSessionFactory( );

    public async Task<int> SearchEmployeeIdByNameAsync(string name) {
        using var session = _factory.OpenSession();
        
        var cookie = await SqlAppRoleHelper.SetApplicationRoleAsync(session, "read_employees", "Test_123");

        var tran = session.BeginTransaction( );

        const string sql = "select employeeId from employees where name like :name";

        var employeeId = await session.CreateSQLQuery(sql)
                                      .SetString("name", $"{name}%")
                                      .FutureValue<int>( )
                                      .GetValueAsync( );
        await tran.CommitAsync( );
        
        await SqlAppRoleHelper.UnsetApplicationRoleAsync(session, cookie);
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

        var cookie = await SqlAppRoleHelper.SetApplicationRoleAsync(session, "write_employees", "Test_123");
        
        var tran = session.BeginTransaction( );
        
        await session.SaveOrUpdateAsync(employee);
        await tran.CommitAsync( );
        
        await SqlAppRoleHelper.UnsetApplicationRoleAsync(session, cookie);

        return employee.EmployeeId;
    }
 
    

    public async Task<Employee> LoadEmployeeAsync(int employeeId) {
        using var session = _factory.OpenSession( );
        
        var cookie = await SqlAppRoleHelper.SetApplicationRoleAsync(session, "write_employees", "Test_123");

        using var tran = session.BeginTransaction( );

        var employee = await session.GetAsync<Employee>(employeeId);

        await tran.CommitAsync( );
        
        await SqlAppRoleHelper.UnsetApplicationRoleAsync(session, cookie);

        return employee;
    }
}

using Dapper;
using Microsoft.Data.SqlClient;

namespace Demo; 

public class DapperTests {
    public async Task<int> SearchEmployeeIdByNameAsync(string name) {
        await using var cnn = new SqlConnection(SqlConfiguration.CnnString);
        cnn.Open();
        
        var cookie = await SqlAppRoleHelper.SetApplicationRoleAsync(cnn, "read_employees", "Test_123");

        await using var tran = cnn.BeginTransaction( );
        const string sql = "select employeeId from employees where name like @name + '%'";
        var employeeId = await cnn.ExecuteScalarAsync<int>(sql, new { name }, tran);

        Console.WriteLine($"Found employee with ID: {employeeId}");
        
        await tran.CommitAsync( );
        
        await SqlAppRoleHelper.UnsetApplicationRoleAsync(cnn, cookie);
        return employeeId;
    }


    public async Task<int> SaveEployeeAsync() {
        await using var cnn = new SqlConnection(SqlConfiguration.CnnString);
        cnn.Open();
        
        var cookie = await SqlAppRoleHelper.SetApplicationRoleAsync(cnn, "write_employees", "Test_123");
        
        var id = DateTime.Now.Ticks;
        var employee = new Employee {
                                        Username = $"User{id}",
                                        Name = $"User {id}",
                                        Contacts = new List<Contact> {
                                                                         new( ) { Value = "123123123", ContactKind = ContactKind.Phone },
                                                                         new( ) { Value = "test@mail.pt", ContactKind = ContactKind.Email }
                                                                     }
                                    };

        const string employeeSql = "insert into employees (name, username) values(@name, @username); select cast(scope_identity() as int)";

        await using var tran = cnn.BeginTransaction( );

        var employeeId = await cnn.ExecuteScalarAsync<int>(employeeSql,
                                                           new {
                                                                   employee.Name,
                                                                   employee.Username
                                                               },
                                                           tran);

        const string contactsSql = "insert into contacts (employeeid, kind, value) values(@employeeid, @kind, @value)";
        await cnn.ExecuteAsync(contactsSql,
                               employee.Contacts.Select(c => new {
                                                                     employeeId,
                                                                     c.Value,
                                                                     Kind = c.ContactKind
                                                                 }),
                               tran);
        await tran.CommitAsync(  );
        
        await SqlAppRoleHelper.UnsetApplicationRoleAsync(cnn, cookie);

        return employeeId;
    }
 
    

    public async Task<Employee?> LoadEmployeeAsync(int employeeId) {
        await using var cnn = new SqlConnection(SqlConfiguration.CnnString);
        cnn.Open();
        
        var cookie = await SqlAppRoleHelper.SetApplicationRoleAsync(cnn, "write_employees", "Test_123");

        const string sql = "select e.EmployeeId, Version, Name, Username, Kind, Value from employees e inner join Contacts c on e.employeeid=c.employeeid where e.employeeid=@employeeid";
        
        await using var tran = cnn.BeginTransaction( );

        var results = await cnn.QueryAsync<Employee, Contact, Employee>(sql,
                                                                        (e, c) => {
                                                                            e.Contacts.Add(c);
                                                                            return e;
                                                                        },
                                                                        new { employeeId },
                                                                        transaction: tran,
                                                                        splitOn: "Kind");
        
        await tran.CommitAsync( );
        
        await SqlAppRoleHelper.UnsetApplicationRoleAsync(cnn, cookie);

        return results.FirstOrDefault();
    }
}

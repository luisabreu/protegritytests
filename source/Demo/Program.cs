// See https://aka.ms/new-console-template for more information

using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using FluentNHibernate.Mapping;
using NHibernate;
using NHibernate.Dialect;
using NHibernate.Driver;

var cnnString = "Data source=.; initial catalog=protegritytests; user id=demoUser; password=d3mo_u5er; trust server certificate=true";

var factory = Fluently.Configure( )
                      .Database(MsSqlConfiguration.MsSql2012.ConnectionString(cnnString).Driver<MicrosoftDataSqlClientDriver>( ).ShowSql( ))
                      .Mappings(m => m.FluentMappings.AddFromAssemblyOf<Employee>( ))
                      .BuildSessionFactory( );

var employeeId = await SaveEployeeAsync(factory.OpenSession( ));

await LoadEmployeeAsync(employeeId, factory.OpenSession( ));

var loadedEmployeeId = await SearchEmployeeIdByNameAsync("us", factory.OpenSession( ));


async Task<int> SearchEmployeeIdByNameAsync(string name, ISession session) {
    var tran = session.BeginTransaction( );

    const string sql = "select employeeId from employees where name like :name";

    var employeeId = await session.CreateSQLQuery(sql)
                                  .SetString("name", $"{name}%")
                                  .FutureValue<int>( )
                                  .GetValueAsync( );
    await tran.CommitAsync( );
    return employeeId;
}


async Task<int> SaveEployeeAsync(ISession session) {
    var id = DateTime.Now.Ticks;
    var employee = new Employee {
                                    Username = $"User{id}",
                                    Name = $"User {id}",
                                    Contacts = new List<Contact> {
                                                                     new( ) { Value = "123123123", ContactKind = ContactKind.Phone },
                                                                     new( ) { Value = "test@mail.pt", ContactKind = ContactKind.Email }
                                                                 }
                                };
    using var tran = session.BeginTransaction( );

    await session.SaveOrUpdateAsync(employee);
    await tran.CommitAsync( );
    return employee.EmployeeId;
}

async Task<Employee> LoadEmployeeAsync(int employeeId, ISession session) {
    using var tran = session.BeginTransaction( );

    var employee = await session.GetAsync<Employee>(employeeId);

    await tran.CommitAsync( );

    return employee;
}

public class Employee {
    public int EmployeeId { get; set; }

    public string Username { get; set; } = "";

    public string Name { get; set; } = "";

    public IList<Contact> Contacts { get; set; } = new List<Contact>( );
    public byte[] Version { get; set; } = Array.Empty<byte>( );
}

public enum ContactKind {
    Phone, Email
}

public class Contact {
    public ContactKind ContactKind { get; set; }

    public string Value { get; set; } = "";
}

public class EmployeeMapping : ClassMap<Employee> {
    public EmployeeMapping() {
        SetupMappings( );
    }

    private void SetupMappings() {
        Table("Employees");
        Not.LazyLoad(  );

        Id(e => e.EmployeeId)
            .Default(0)
            .GeneratedBy.Identity( );
        Version(e => e.Version)
            .Generated.Always(  )
            .CustomType<byte[]>( )
            .CustomSqlType("rowversion");

        Map(e => e.Name);
        Map(e => e.Username);
        
        HasMany(e => e.Contacts)
            .Table("Contacts")
            .KeyColumn("EmployeeId")
            .Component(ct => {
                           ct.Map(c => c.Value);
                           ct.Map(c => c.ContactKind, "Kind")
                             .CustomType<ContactKind>(  )
                             .CustomSqlType("int");
                       })
            .Not.LazyLoad();
            

    }
}

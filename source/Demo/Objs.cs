using FluentNHibernate.Mapping;

namespace Demo;

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
        Not.LazyLoad( );

        Id(e => e.EmployeeId)
            .Default(0)
            .GeneratedBy.Identity( );
        Version(e => e.Version)
            .Generated.Always( )
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
                             .CustomType<ContactKind>( )
                             .CustomSqlType("int");
                       })
            .Not.LazyLoad( );
    }
}

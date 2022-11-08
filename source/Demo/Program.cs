// See https://aka.ms/new-console-template for more information

using Demo;

//var cnnString = "Data source=.; initial catalog=protegritytests; user id=demoUser; password=d3mo_u5er; trust server certificate=true";


// run with NH
// await RunItWithNhibernate( );

// run with Dapper
await RunItWithDapper( );

async Task RunItWithDapper() {
    var dapper = new DapperTests( );

    var employeeId = await dapper.SaveEployeeAsync( );

    await dapper.SearchEmployeeIdByNameAsync("us");

    var loadedEmployee = await dapper.LoadEmployeeAsync(employeeId);

    Console.WriteLine($"{loadedEmployee?.Name} - {loadedEmployee?.Username}");
    foreach( var c in loadedEmployee?.Contacts ?? Enumerable.Empty<Contact>(  ) ) {
        Console.WriteLine($"{c.Value} {c.ContactKind}");
    }
}

async Task RunItWithNhibernate() {
    var nhTest = new NhTest( );

    var employeeId = await nhTest.SaveEployeeAsync( );

    await nhTest.LoadEmployeeAsync(employeeId);

    var loadedEmployeeId = await nhTest.SearchEmployeeIdByNameAsync("us");
}

// See https://aka.ms/new-console-template for more information

using Demo;

//var cnnString = "Data source=.; initial catalog=protegritytests; user id=demoUser; password=d3mo_u5er; trust server certificate=true";
var cnnString = "Data source=ws-windows1001; initial catalog=protegritytests; user id=nopermissions_user; password=d3mo_u5er";

var nhTest = new NhTest( );

var employeeId = await nhTest.SaveEployeeAsync( );

await nhTest.LoadEmployeeAsync(employeeId);

var loadedEmployeeId = await nhTest.SearchEmployeeIdByNameAsync("us");

CREATE DATABASE ProtegrityTests;
GO

EXEC sp_configure 'contained database authentication', 1
GO

RECONFIGURE
GO

use ProtegrityTests;
ALTER DATABASE ProtegrityTests SET CONTAINMENT = PARTIAL
GO

CREATE USER [demoUser] WITH PASSWORD = 'd3mo_u5er'
GO

exec sp_addrolemember 'db_datareader', 'demoUser'
exec sp_addrolemember 'db_datawriter', 'demoUser'
go

-- Civil parish tables 
CREATE TABLE dbo.Employees(
    EmployeeId INT IDENTITY PRIMARY KEY CLUSTERED,
    [Version] rowversion NOT NULL,
    [Name] NVARCHAR(200) NOT NULL,
    Username NVARCHAR(50) NOT NULL,
    CONSTRAINT Unique_Username UNIQUE(Username)    
)
GO
-- index Username
CREATE NONCLUSTERED INDEX IX_Username_CivilParishes 
    ON dbo.Employees(Username) 
GO

-- Create Contacts table
CREATE TABLE dbo.Contacts(
    ContactId INT IDENTITY PRIMARY KEY CLUSTERED,
    EmployeeId INT NOT NULL,
    Kind INT NOT NULL,
    [Value] NVARCHAR(200)  NOT NULL
)
GO

CREATE NONCLUSTERED INDEX IX_EmployeeId_Contacts
ON dbo.Contacts(EmployeeId)





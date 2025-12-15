-- ============================================================================
-- Create SQL Database User for Container App Managed Identity
-- ============================================================================
-- Run this script after deploying the infrastructure to grant the API service
-- managed identity access to the SQL database.
--
-- Prerequisites:
-- 1. Azure AD admin must be configured on SQL Server
-- 2. Connect to the database as the Azure AD admin
-- 3. Get the managed identity name from the Azure portal or deployment output
--
-- Connection: Use Azure Data Studio or SSMS with Azure AD authentication
-- Server: <your-sql-server>.database.windows.net
-- Database: fencemark
-- Authentication: Azure Active Directory
-- ============================================================================

-- Replace '<container-app-name>' with your actual Container App name
-- Format is typically: ca-apiservice-<resourcetoken>
-- You can find this in the Azure Portal or deployment outputs

-- Create the user from the external provider (managed identity)
CREATE USER [<container-app-name>] FROM EXTERNAL PROVIDER;

-- Grant necessary permissions
-- db_datareader: Read all data
-- db_datawriter: Insert, update, delete all data
ALTER ROLE db_datareader ADD MEMBER [<container-app-name>];
ALTER ROLE db_datawriter ADD MEMBER [<container-app-name>];

-- If you need schema modification permissions (for EF migrations), also add:
-- ALTER ROLE db_ddladmin ADD MEMBER [<container-app-name>];

-- Verify the user was created
SELECT name, type_desc, authentication_type_desc 
FROM sys.database_principals 
WHERE name = '<container-app-name>';

-- ============================================================================
-- Example with actual values (DEV environment):
-- ============================================================================
-- CREATE USER [ca-apiservice-abc123xyz] FROM EXTERNAL PROVIDER;
-- ALTER ROLE db_datareader ADD MEMBER [ca-apiservice-abc123xyz];
-- ALTER ROLE db_datawriter ADD MEMBER [ca-apiservice-abc123xyz];
-- ============================================================================

-- Test file for timezone conversion patterns
CREATE TABLE [dbo].[Users]
(
    [UserId] [int] IDENTITY(1,1) NOT NULL,
    [Username] [nvarchar](100) NOT NULL,
    [CreatedDate] [datetime] NOT NULL CONSTRAINT [DF_Users_CreatedDate] DEFAULT (cast(getdate() AT TIME ZONE 'UTC' AT TIME ZONE 'Pacific Standard Time' as datetime)),
    [ModifiedDate] [datetime] NULL,
    [LastLoginDate] [datetime] NULL
)

-- Stored procedure with timezone usage
CREATE PROCEDURE [dbo].[sp_GetRecentUsers]
AS
BEGIN
    SELECT * 
    FROM [dbo].[Users]
    WHERE CreatedDate > CAST(GETDATE()AT TIME ZONE 'UTC' AT TIME ZONE 'Pacific Standard Time' AS DATETIME)-7
    AND LastLoginDate < CAST(GETDATE()AT TIME ZONE 'UTC' AT TIME ZONE 'Pacific Standard Time' AS DATETIME)
END
GO

-- Function using SYSDATETIME
CREATE FUNCTION [dbo].[fn_GetCurrentMonth]()
RETURNS INT
AS
BEGIN
    RETURN MONTH( cast(getdate() AT TIME ZONE 'UTC' AT TIME ZONE 'Pacific Standard Time' as datetime) )
END
GO

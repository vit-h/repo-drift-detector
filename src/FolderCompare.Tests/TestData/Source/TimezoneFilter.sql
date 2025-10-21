-- Test file for timezone conversion patterns
CREATE TABLE [dbo].[Users]
(
    [UserId] [int] IDENTITY(1,1) NOT NULL,
    [Username] [nvarchar](100) NOT NULL,
    [CreatedDate] [datetime] NOT NULL CONSTRAINT [DF_Users_CreatedDate] DEFAULT (GETDATE()),
    [ModifiedDate] [datetime] NULL,
    [LastLoginDate] [datetime] NULL
) ON [PRIMARY]

-- Stored procedure with timezone usage
CREATE PROCEDURE [dbo].[sp_GetRecentUsers]
AS
BEGIN
    SELECT * 
    FROM [dbo].[Users]
    WHERE CreatedDate > GETDATE()-7
    AND LastLoginDate < GETDATE()
END
GO

-- Function using SYSDATETIME
CREATE FUNCTION [dbo].[fn_GetCurrentMonth]()
RETURNS INT
AS
BEGIN
    RETURN MONTH(GETDATE())
END
GO

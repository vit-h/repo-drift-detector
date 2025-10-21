-- Test file with multiline comments
CREATE TABLE [dbo].[Products]
(
    [ProductId] [int] NOT NULL,
    [ProductName] [nvarchar](100) NOT NULL
    /* This is a multiline comment
       spanning multiple lines
       with important information */
)

/*
 * Large multiline comment block
 * Line 2
 * Line 3
 */
CREATE PROCEDURE [dbo].[sp_GetProducts]
AS
BEGIN
    /* Query comment */
    SELECT * FROM [dbo].[Products]
    WHERE ProductId > 0 /* Filter out invalid products */
END
GO

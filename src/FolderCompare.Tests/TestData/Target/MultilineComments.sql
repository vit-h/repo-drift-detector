-- Test file with multiline comments (modified)
CREATE TABLE [dbo].[Products]
(
    [ProductId] [int] NOT NULL,
    [ProductName] [nvarchar](100) NOT NULL
    /* This is a DIFFERENT multiline comment
       with different content
       and new information */
)

/*
 * Modified multiline comment block
 * Line 2 changed
 * Line 3 also changed
 */
CREATE PROCEDURE [dbo].[sp_GetProducts]
AS
BEGIN
    /* Updated query comment */
    SELECT * FROM [dbo].[Products]
    WHERE ProductId > 0 /* Updated filter comment */
END
GO

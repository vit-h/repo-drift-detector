-- Test file with mixed changes: comments AND code
-- Version: 1.0
CREATE TABLE [dbo].[Orders]
(
    [OrderId] [int] NOT NULL,
    [CustomerId] [int] NOT NULL,
    [OrderDate] [datetime] NOT NULL DEFAULT (GETDATE()),
    [TotalAmount] [decimal](18,2) NOT NULL
) ON [Data]

-- Process orders procedure
CREATE PROCEDURE [dbo].[sp_ProcessOrder]
    @OrderId INT
AS
BEGIN
    -- Update order status
    UPDATE [dbo].[Orders]
    SET ProcessedDate = GETDATE()
    WHERE OrderId = @OrderId
END
GO

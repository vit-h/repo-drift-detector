-- Test file with mixed changes: comments AND code (modified)
-- Version: 2.0
CREATE TABLE [dbo].[Orders]
(
    [OrderId] [int] NOT NULL,
    [CustomerId] [int] NOT NULL,
    [OrderDate] [datetime] NOT NULL DEFAULT (CAST(getdate() AT TIME ZONE 'UTC' AT TIME ZONE 'Pacific Standard Time' as datetime)),
    [TotalAmount] [decimal](18,2) NOT NULL,
    [ShippingCost] [decimal](18,2) NULL -- New column added
)

-- Updated process orders procedure
CREATE PROCEDURE [dbo].[sp_ProcessOrder]
    @OrderId INT,
    @UpdatedBy NVARCHAR(100) -- New parameter
AS
BEGIN
    -- Update order status with timezone-aware date
    UPDATE [dbo].[Orders]
    SET ProcessedDate = CAST(getdate() AT TIME ZONE 'UTC' AT TIME ZONE 'Pacific Standard Time' as datetime)
    WHERE OrderId = @OrderId
END
GO

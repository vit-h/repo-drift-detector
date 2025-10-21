-- Test file for filegroup and storage patterns
CREATE TABLE [dbo].[Transactions]
(
    [TransactionId] [bigint] IDENTITY(1,1) NOT NULL,
    [Amount] [decimal](18,2) NOT NULL,
    [TransactionDate] [datetime] NOT NULL
)

CREATE INDEX [IX_Transactions_Date] ON [dbo].[Transactions]([TransactionDate])

-- Table with PRIMARY filegroup
CREATE TABLE [dbo].[Settings]
(
    [SettingId] [int] NOT NULL,
    [SettingValue] [nvarchar](500) NOT NULL
)
GO

-- Statement formatting differences
EXEC sp_UpdateData @Id = 1

[dbo].[sp_UpdateData] TO [user_ExecSP]
GO



GO

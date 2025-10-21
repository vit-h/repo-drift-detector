-- Test file for filegroup and storage patterns
CREATE TABLE [dbo].[Transactions]
(
    [TransactionId] [bigint] IDENTITY(1,1) NOT NULL,
    [Amount] [decimal](18,2) NOT NULL,
    [TransactionDate] [datetime] NOT NULL
) ON [Data]

CREATE INDEX [IX_Transactions_Date] ON [dbo].[Transactions]([TransactionDate]) ON [Data]

-- Table with PRIMARY filegroup
CREATE TABLE [dbo].[Settings]
(
    [SettingId] [int] NOT NULL,
    [SettingValue] [nvarchar](500) NOT NULL
) ON [PRIMARY]
GO

-- Statement formatting differences
EXEC sp_UpdateData @Id = 1;

GRANT EXECUTE ON [dbo].[sp_UpdateData] TO [user_ExecSP]
GO

USE [TestDB]
GO
SET QUOTED_IDENTIFIER ON
GO

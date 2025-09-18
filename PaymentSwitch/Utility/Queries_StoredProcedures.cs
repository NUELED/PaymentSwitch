using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PaymentSwitch.Utility
{
    public static class Queries_StoredProcedures
    {

       
        public const  string    CREATE_TRANSFER_TABLE_WITH_INDEX = @"CREATE TABLE Transfers
                                                                    (
                                                                        Id BIGINT IDENTITY PRIMARY KEY,
                                                                        TransactionRef NVARCHAR(50) NOT NULL UNIQUE,
                                                                        FromAccount NVARCHAR(50) NOT NULL,
                                                                        ToAccount NVARCHAR(50) NOT NULL,
                                                                        ToBankCode NVARCHAR(10) NOT NULL,
                                                                        Amount DECIMAL(18,2) NOT NULL,
                                                                        Status NVARCHAR(30) NOT NULL, -- PendingDebitAttempt, Debited, Credited, Failed, Reversed, PendingQuery
                                                                        LastAttemptedAt DATETIME NULL,
                                                                        CreatedAt DATETIME NOT NULL DEFAULT GETUTCDATE(),
                                                                        UpdatedAt DATETIME NOT NULL DEFAULT GETUTCDATE(),
                                                                        RetryCount INT NOT NULL DEFAULT 0,
                                                                        Metadata NVARCHAR(MAX) NULL, -- store raw responses etc.
                                                                        ErrorMessage NVARCHAR(4000) NULL
                                                                    );
                                                                    CREATE INDEX IX_Transfers_Status ON Transfers(Status);";    



    }
}

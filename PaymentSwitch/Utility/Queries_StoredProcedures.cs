using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PaymentSwitch.Utility
{
    public static class Queries_StoredProcedures
    {

      #region Account Quuries

        public const string CHECK_IF_ACCOUNT_EXISTS = @"SELECT COUNT(1) FROM Accounts WHERE AccountNumber = @AccountNumber";    

        public const string FETCH_ACCOUNT = "SELECT * FROM Accounts WHERE AccountNumber = @AccountNumber";  
        
        public const string UPDATE_ACCOUNT_AFTER_TRANSACTION = @"UPDATE Accounts 
                                                                SET Balance = @Balance, LastUpdated = @LastUpdated
                                                                WHERE AccountNumber = @AccountNumber";

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


        public const string CREATE_ACCOUNT_TABLE_WITH_INDEX = @"CREATE TABLE Accounts
                                                                (
                                                                    Id NVARCHAR(50) PRIMARY KEY,
                                                                    Balance DECIMAL(18,2) NOT NULL,
                                                                    AccountNumber NVARCHAR(20) NOT NULL UNIQUE,
                                                                    FirstName NVARCHAR(100) NOT NULL,
                                                                    LastName NVARCHAR(100) NOT NULL,
                                                                    AccountName NVARCHAR(200) NOT NULL,
                                                                    PhoneNumber NVARCHAR(15) NULL,
                                                                    Email NVARCHAR(100) NULL,
                                                                    IsActive BIT NOT NULL,
                                                                    CurrentAccountBalance DECIMAL(18,2) NOT NULL,
                                                                    AccountType NVARCHAR(20) NOT NULL, -- Savings, Current
                                                                    AccountTier NVARCHAR(20) NOT NULL, -- Basic, Premium
                                                                    LastUpdated DATETIME NOT NULL DEFAULT GETUTCDATE(),
                                                                    DateCreated DATETIME NOT NULL DEFAULT GETUTCDATE()
                                                                 );
                                                                 CREATE INDEX IX_Accounts_AccountNumber ON Accounts(AccountNumber);";


                   public const string CREATE_ACCOUNT_TABLE =   @"CREATE TABLE Accounts
                                                                (
                                                                    Id NVARCHAR(50) NOT NULL PRIMARY KEY, -- Guid stored as string
                                                                    Balance DECIMAL(18,2) NOT NULL DEFAULT 0,
                                                                    AccountNumber NVARCHAR(20) NOT NULL,
                                                                    FirstName NVARCHAR(100) NOT NULL,
                                                                    LastName NVARCHAR(100) NOT NULL,
                                                                    AccountName NVARCHAR(200) NOT NULL,
                                                                    PhoneNumber NVARCHAR(20) NULL,
                                                                    Email NVARCHAR(200) NULL,
                                                                    IsActive BIT NOT NULL DEFAULT 1,
                                                                    CurrentAccountBalance DECIMAL(18,2) NOT NULL DEFAULT 0,
                                                                    AccountType INT NOT NULL,
                                                                    AccountTier INT NOT NULL,
                                                                    LastUpdated DATETIME NOT NULL DEFAULT GETUTCDATE(),
                                                                    DateCreated DATETIME NOT NULL DEFAULT GETDATE(),

                                                                    -- Unique constraint for AccountNumber
                                                                    CONSTRAINT UQ_Accounts_AccountNumber UNIQUE (AccountNumber)
                                                                );";






        #endregion


        #region Account Stored Procedures   
        public const string CREATE_DEBIT_SP = @"CREATE OR ALTER PROCEDURE sp_DebitAccount
                                                @AccountNumber NVARCHAR(20),
                                                @Amount DECIMAL(18,2),
                                                @RefId NVARCHAR(50),
                                                @Result INT OUTPUT,
                                                @Message NVARCHAR(200) OUTPUT
                                            AS
                                            BEGIN
                                                SET NOCOUNT ON;
                                                BEGIN TRY
                                                    BEGIN TRANSACTION;

                                                    DECLARE @CurrentBalance DECIMAL(18,2);

                                                    SELECT @CurrentBalance = Balance
                                                    FROM Accounts
                                                    WHERE AccountNumber = @AccountNumber;

                                                    IF @CurrentBalance IS NULL
                                                    BEGIN
                                                        SET @Result = -1;
                                                        SET @Message = 'Account not found';
                                                        ROLLBACK TRANSACTION;
                                                        RETURN;
                                                    END

                                                    IF @CurrentBalance < @Amount
                                                    BEGIN
                                                        SET @Result = -2;
                                                        SET @Message = 'Insufficient funds';
                                                        ROLLBACK TRANSACTION;
                                                        RETURN;
                                                    END

                                                    UPDATE Accounts
                                                    SET Balance = Balance - @Amount,
                                                        LastUpdated = GETUTCDATE()
                                                    WHERE AccountNumber = @AccountNumber;

                                                    SET @Result = 1;
                                                    SET @Message = CONCAT('Debited ', @Amount, ' from ', @AccountNumber);

                                                    COMMIT TRANSACTION;
                                                END TRY
                                                BEGIN CATCH
                                                    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;

                                                    SET @Result = -99;
                                                    SET @Message = ERROR_MESSAGE();
                                                END CATCH
                                            END";

        
        public const string CREATE_CREDIT_SP = @"CREATE OR ALTER PROCEDURE sp_CreditAccount
                                                    @AccountNumber NVARCHAR(20),
                                                    @Amount DECIMAL(18,2),
                                                    @RefId NVARCHAR(50),
                                                    @Result INT OUTPUT,
                                                    @Message NVARCHAR(200) OUTPUT
                                                AS
                                                BEGIN
                                                    SET NOCOUNT ON;
                                                    BEGIN TRY
                                                        BEGIN TRANSACTION;

                                                        IF NOT EXISTS (SELECT 1 FROM Accounts WHERE AccountNumber = @AccountNumber)
                                                        BEGIN
                                                            SET @Result = -1;
                                                            SET @Message = 'Account not found';
                                                            ROLLBACK TRANSACTION;
                                                            RETURN;
                                                        END

                                                        UPDATE Accounts
                                                        SET Balance = Balance + @Amount,
                                                            LastUpdated = GETUTCDATE()
                                                        WHERE AccountNumber = @AccountNumber;

                                                        SET @Result = 1;
                                                        SET @Message = CONCAT('Credited ', @Amount, ' to ', @AccountNumber);

                                                        COMMIT TRANSACTION;
                                                    END TRY
                                                    BEGIN CATCH
                                                        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;

                                                        SET @Result = -99;
                                                        SET @Message = ERROR_MESSAGE();
                                                    END CATCH
                                                END";


        #endregion

    }
}

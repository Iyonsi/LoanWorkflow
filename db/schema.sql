CREATE TABLE dbo.LoanRequest (
    Id VARCHAR(36) NOT NULL PRIMARY KEY,
    FlowType INT NOT NULL,
    Amount DECIMAL(18,2) NOT NULL,
    BorrowerId VARCHAR(36) NOT NULL,
    Status VARCHAR(30) NOT NULL,
    CurrentStage VARCHAR(40) NOT NULL,
    StageIndex INT NOT NULL,
    CreatedAt DATETIME2 NOT NULL,
    UpdatedAt DATETIME2 NOT NULL
);
GO
CREATE TABLE dbo.LoanRequestLog (
    Id VARCHAR(36) NOT NULL PRIMARY KEY,
    LoanRequestId VARCHAR(36) NOT NULL,
    Stage VARCHAR(40) NOT NULL,
    Action VARCHAR(30) NOT NULL,
    ActorUserId VARCHAR(36) NOT NULL,
    Comments NVARCHAR(500) NULL,
    CreatedAt DATETIME2 NOT NULL,
    UpdatedAt DATETIME2 NOT NULL,
    CONSTRAINT FK_LoanRequestLog_Request FOREIGN KEY (LoanRequestId) REFERENCES dbo.LoanRequest(Id)
);
GO
CREATE TABLE dbo.Loan (
    Id VARCHAR(36) NOT NULL PRIMARY KEY,
    LoanRequestId VARCHAR(36) NOT NULL UNIQUE,
    LoanNumber VARCHAR(40) NOT NULL UNIQUE,
    Principal DECIMAL(18,2) NOT NULL,
    InterestRate DECIMAL(9,4) NOT NULL,
    TermMonths INT NOT NULL,
    StartDate DATE NOT NULL,
    Status VARCHAR(20) NOT NULL,
    CreatedAt DATETIME2 NOT NULL,
    UpdatedAt DATETIME2 NOT NULL,
    CONSTRAINT FK_Loan_Request FOREIGN KEY (LoanRequestId) REFERENCES dbo.LoanRequest(Id)
);
GO
CREATE INDEX IX_LoanRequest_Status ON dbo.LoanRequest(Status);
GO
CREATE INDEX IX_LoanRequestLog_RequestId_CreatedAt ON dbo.LoanRequestLog(LoanRequestId, CreatedAt);
GO

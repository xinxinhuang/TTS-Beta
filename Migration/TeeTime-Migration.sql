-- Create the database
-- Remove this if you already have the database
CREATE DATABASE TTSDB;
GO

-- Use the database
USE TTSDB;
GO

-- 1. Roles (No 'Guest' role)
CREATE TABLE Role (
    RoleID INT PRIMARY KEY IDENTITY(1,1),
    RoleDescription VARCHAR(50) NOT NULL UNIQUE
);
GO
INSERT INTO Role (RoleDescription) VALUES
('Member'), ('Clerk'), ('Pro Shop Staff'), ('Committee Member');
GO

-- 3. Membership Categories (Gold tiers expanded)
CREATE TABLE MembershipCategory (
    MembershipCategoryID INT PRIMARY KEY IDENTITY(1,1),
    MembershipName VARCHAR(50) NOT NULL UNIQUE,
    CanSponsor BIT NOT NULL, -- BIT type replaces BOOLEAN
    CanMakeStandingTeeTime BIT NOT NULL -- BIT type replaces BOOLEAN
);
GO
INSERT INTO MembershipCategory (MembershipName, CanSponsor, CanMakeStandingTeeTime) VALUES
('Gold', 0, 0),
('Gold Shareholder', 1, 1),
('Gold Associate', 1, 1),
('Silver', 0, 0),
('Bronze', 0, 0),
('Copper', 0, 0);
GO

-- 2. Users (No Guest; default role = Member)
CREATE TABLE [User] ( -- User is a reserved keyword in some SQL dialects, so brackets are used to avoid errors
    UserID INT PRIMARY KEY IDENTITY(1,1),
    FirstName VARCHAR(50) NOT NULL,
    LastName VARCHAR(50) NOT NULL,
    Email VARCHAR(255) NOT NULL UNIQUE,
    PasswordHash VARCHAR(255) NOT NULL, -- Hashed password
    RoleID INT NOT NULL DEFAULT 1, -- Default: Member
    MembershipCategoryID INT NOT NULL, -- Initial tier (e.g., Bronze)
    FOREIGN KEY (RoleID) REFERENCES Role(RoleID),
    FOREIGN KEY (MembershipCategoryID) REFERENCES MembershipCategory(MembershipCategoryID)
);
GO

-- 4. Member Table (Active memberships)
CREATE TABLE Member (
    MemberID INT PRIMARY KEY IDENTITY(1,1),
    UserID INT NOT NULL UNIQUE,
    MembershipCategoryID INT NOT NULL,
    JoinDate DATE NOT NULL,
    [Status] VARCHAR(10) NOT NULL DEFAULT 'Active',
    MemberPhone VARCHAR(12) NULL, 
    GoodStanding BIT NOT NULL DEFAULT 1,
    FOREIGN KEY (UserID) REFERENCES [User](UserID),
    FOREIGN KEY (MembershipCategoryID) REFERENCES MembershipCategory(MembershipCategoryID),
    CONSTRAINT CHK_Member_Status CHECK ([Status] IN ('Active', 'Inactive')),
    CONSTRAINT CHK_Member_Phone CHECK (MemberPhone IS NULL OR MemberPhone LIKE '[0-9][0-9][0-9]-[0-9][0-9][0-9]-[0-9][0-9][0-9][0-9]')
);
GO

-- Schedule Golf Time table
CREATE TABLE ScheduledGolfTime (
    ScheduledGolfTimeID INT PRIMARY KEY IDENTITY(1,1),
    ScheduledDate DATE NOT NULL,
    ScheduledTime TIME NOT NULL,
    GolfSessionInterval INT NOT NULL DEFAULT 8, -- in minutes
    IsAvailable BIT NOT NULL DEFAULT 1 -- BIT type replaces BOOLEAN
);
GO

-- 5. MemberUpgrade Table (Retains full details)
CREATE TABLE MemberUpgrade (
    ApplicationID INT PRIMARY KEY IDENTITY(1,1),
    UserID INT NOT NULL,
    Address VARCHAR(255) NOT NULL,
    PostalCode VARCHAR(20) NOT NULL,
    Phone VARCHAR(20) NOT NULL,
    AlternatePhone VARCHAR(20),
    Email VARCHAR(255) NOT NULL,
    DateOfBirth DATE NOT NULL,
    Occupation VARCHAR(100),       -- For Gold upgrades
    CompanyName VARCHAR(100),
    CompanyAddress VARCHAR(255),
    CompanyPostalCode VARCHAR(20),
    CompanyPhone VARCHAR(20),
    Sponsor1MemberID INT,            -- Must be Gold-tier
    Sponsor2MemberID INT,
    DesiredMembershipCategoryID INT NOT NULL, -- e.g., Gold Shareholder
    [Status] VARCHAR(10) NOT NULL DEFAULT 'Pending',
    ApprovalDate DATE,
    ApprovalByUserID INT,            -- Must be Committee Member
    FOREIGN KEY (UserID) REFERENCES [User](UserID),
    FOREIGN KEY (DesiredMembershipCategoryID) REFERENCES MembershipCategory(MembershipCategoryID),
    FOREIGN KEY (Sponsor1MemberID) REFERENCES Member(MemberID),
    FOREIGN KEY (Sponsor2MemberID) REFERENCES Member(MemberID),
    FOREIGN KEY (ApprovalByUserID) REFERENCES [User](UserID),
    CONSTRAINT CHK_MemberUpgrade_Status CHECK ([Status] IN ('Pending', 'Approved', 'Rejected'))
);
GO

CREATE TABLE Reservation (
    ReservationID INT PRIMARY KEY IDENTITY(1,1),
    MemberID INT NOT NULL,
    ScheduledGolfTimeID INT NOT NULL,
    ReservationMadeDate DATETIME NOT NULL DEFAULT GETDATE(),
    ReservationStatus VARCHAR(20) NOT NULL DEFAULT 'Confirmed',
    NumberOfPlayers INT NOT NULL CHECK (NumberOfPlayers BETWEEN 1 AND 4),
    NumberOfCarts INT NOT NULL CHECK (NumberOfCarts BETWEEN 0 AND 4),
    FOREIGN KEY (MemberID) REFERENCES Member(MemberID),
    FOREIGN KEY (ScheduledGolfTimeID) REFERENCES ScheduledGolfTime(ScheduledGolfTimeID),
    CONSTRAINT CK_Reservation_NumberOfCarts CHECK (NumberOfCarts <= NumberOfPlayers)
);
GO

CREATE TABLE StandingTeeTimeRequest (
    RequestID INT PRIMARY KEY IDENTITY(1,1),
    MemberID INT NOT NULL,
    DayOfWeek VARCHAR(10) NOT NULL,
    StartDate DATE NOT NULL,
    EndDate DATE NOT NULL,
    DesiredTeeTime TIME NOT NULL,
    PriorityNumber INT,
    ApprovedTeeTime TIME,
    ApprovedByUserID INT,
    ApprovedDate DATE,
    FOREIGN KEY (MemberID) REFERENCES Member(MemberID),
    FOREIGN KEY (ApprovedByUserID) REFERENCES [User](UserID),
    CONSTRAINT CHK_DayOfWeek CHECK (DayOfWeek IN ('Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday', 'Sunday'))
);
GO

-- 6. Triggers for Validation (fixed for SQL Server 2016 and later)
-- Ensure sponsors are Gold-tier
CREATE TRIGGER ValidateSponsorsBeforeUpgrade
ON MemberUpgrade
AFTER INSERT
AS
BEGIN
    DECLARE @ErrorMsg NVARCHAR(2048);
    
    IF EXISTS (
        SELECT 1
        FROM inserted i
        LEFT JOIN Member m1 ON i.Sponsor1MemberID = m1.MemberID
        LEFT JOIN MembershipCategory mc1 ON m1.MembershipCategoryID = mc1.MembershipCategoryID
        WHERE i.Sponsor1MemberID IS NOT NULL AND mc1.MembershipName NOT LIKE 'Gold%'
    )
    BEGIN
        SET @ErrorMsg = 'Sponsor1 must be a Gold-tier member';
        RAISERROR(@ErrorMsg, 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END

    IF EXISTS (
        SELECT 1
        FROM inserted i
        LEFT JOIN Member m2 ON i.Sponsor2MemberID = m2.MemberID
        LEFT JOIN MembershipCategory mc2 ON m2.MembershipCategoryID = mc2.MembershipCategoryID
        WHERE i.Sponsor2MemberID IS NOT NULL AND mc2.MembershipName NOT LIKE 'Gold%'
    )
    BEGIN
        SET @ErrorMsg = 'Sponsor2 must be a Gold-tier member';
        RAISERROR(@ErrorMsg, 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END
END;
GO

-- Ensure approver is a Committee Member
CREATE TRIGGER ValidateApproverBeforeUpgrade
ON MemberUpgrade
AFTER INSERT
AS
BEGIN
    DECLARE @ErrorMsg NVARCHAR(2048);
    
    IF EXISTS (
        SELECT 1
        FROM inserted i
        LEFT JOIN [User] u ON i.ApprovalByUserID = u.UserID
        LEFT JOIN Role r ON u.RoleID = r.RoleID
        WHERE i.ApprovalByUserID IS NOT NULL AND r.RoleDescription != 'Committee Member'
    )
    BEGIN
        SET @ErrorMsg = 'Approver must be a Committee Member';
        RAISERROR(@ErrorMsg, 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END
END;
GO

CREATE TRIGGER ValidateStandingTeeTimeEligibility
ON StandingTeeTimeRequest
AFTER INSERT
AS
BEGIN
    DECLARE @ErrorMsg NVARCHAR(2048);
    
    IF EXISTS (
        SELECT 1
        FROM inserted i
        JOIN Member m ON i.MemberID = m.MemberID
        JOIN MembershipCategory mc ON m.MembershipCategoryID = mc.MembershipCategoryID
        WHERE mc.CanMakeStandingTeeTime = 0
    )
    BEGIN
        SET @ErrorMsg = 'Member is not eligible to request standing tee times';
        RAISERROR(@ErrorMsg, 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END
END;
GO

-- Create stored procedure for booking tee times
CREATE PROCEDURE BookTeeTimeReservation 
    @p_MemberID INT,
    @p_ScheduledGolfTimeID INT,
    @p_NumberOfPlayers INT,
    @p_NumberOfCarts INT,
    @p_ReservationStatus VARCHAR(20),
    @p_ReturnCode INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @ErrorMsg NVARCHAR(2048);

    BEGIN TRY
        BEGIN TRANSACTION;

        -- Validate Member is in Good Standing
        IF NOT EXISTS (
            SELECT 1 FROM Member
            WHERE MemberID = @p_MemberID
            AND GoodStanding = 1
        )
        BEGIN
            SET @p_ReturnCode = 1;
            ROLLBACK TRANSACTION;
            RETURN;
        END

        -- Validate Tee Time Availability
        IF NOT EXISTS (
            SELECT 1 FROM ScheduledGolfTime
            WHERE ScheduledGolfTimeID = @p_ScheduledGolfTimeID
            AND IsAvailable = 1
        )
        BEGIN
            SET @p_ReturnCode = 2;
            ROLLBACK TRANSACTION;
            RETURN;
        END

        -- Insert Reservation
        INSERT INTO Reservation (
            MemberID,
            ScheduledGolfTimeID,
            ReservationStatus,
            NumberOfPlayers,
            NumberOfCarts
        )
        VALUES (
            @p_MemberID,
            @p_ScheduledGolfTimeID,
            COALESCE(@p_ReservationStatus, 'Confirmed'),
            @p_NumberOfPlayers,
            @p_NumberOfCarts
        );

        -- Update Availability
        UPDATE ScheduledGolfTime
        SET IsAvailable = 0
        WHERE ScheduledGolfTimeID = @p_ScheduledGolfTimeID;

        COMMIT TRANSACTION;
        SET @p_ReturnCode = 0;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        SET @p_ReturnCode = 99;
    END CATCH;
END;
GO

-- Insert test data
-- Add a few users
INSERT INTO [User] (FirstName, LastName, Email, PasswordHash, RoleID, MembershipCategoryID)
VALUES 
('John', 'Doe', 'john.doe@email.com', 'hashed_password_1', 1, 2),  -- Gold Shareholder
('Jane', 'Smith', 'jane.smith@email.com', 'hashed_password_2', 1, 1),  -- Gold
('Bob', 'Johnson', 'bob.johnson@email.com', 'hashed_password_3', 1, 4),  -- Silver
('Alice', 'Williams', 'alice.williams@email.com', 'hashed_password_4', 1, 5),  -- Bronze
('Charlie', 'Brown', 'charlie.brown@email.com', 'hashed_password_5', 4, 1);  -- Committee Member (Gold)
GO

-- Add members
INSERT INTO Member (UserID, MembershipCategoryID, JoinDate, [Status], MemberPhone, GoodStanding)
VALUES 
(1, 2, '2020-01-15', 'Active', '123-456-7890', 1),  -- John Doe (Gold Shareholder)
(2, 1, '2020-02-20', 'Active', '234-567-8901', 1),  -- Jane Smith (Gold)
(3, 4, '2020-03-25', 'Active', '345-678-9012', 1),  -- Bob Johnson (Silver)
(4, 5, '2020-04-30', 'Active', '456-789-0123', 1),  -- Alice Williams (Bronze)
(5, 1, '2019-12-10', 'Active', '567-890-1234', 1);  -- Charlie Brown (Committee Member, Gold)
GO

-- Add scheduled golf times for the next 7 days
DECLARE @StartDate DATE = GETDATE();
DECLARE @EndDate DATE = DATEADD(DAY, 7, GETDATE());
DECLARE @CurrentDate DATE = @StartDate;

WHILE @CurrentDate <= @EndDate
BEGIN
    -- Add times from 7 AM to 6 PM in 10-minute intervals
    DECLARE @CurrentTime TIME = '07:00:00';
    DECLARE @EndTime TIME = '18:00:00';
    
    WHILE @CurrentTime <= @EndTime
    BEGIN
        INSERT INTO ScheduledGolfTime (ScheduledDate, ScheduledTime, GolfSessionInterval, IsAvailable)
        VALUES (@CurrentDate, @CurrentTime, 10, 1);
        
        SET @CurrentTime = DATEADD(MINUTE, 10, @CurrentTime);
    END
    
    SET @CurrentDate = DATEADD(DAY, 1, @CurrentDate);
END;
GO 
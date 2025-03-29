-- Check if Player IDs columns exist in StandingTeeTimeRequest and add them if they don't
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'StandingTeeTimeRequests' AND COLUMN_NAME = 'Player2ID')
BEGIN
    ALTER TABLE StandingTeeTimeRequests
    ADD Player2ID INT NOT NULL DEFAULT 0;
END

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'StandingTeeTimeRequests' AND COLUMN_NAME = 'Player3ID')
BEGIN
    ALTER TABLE StandingTeeTimeRequests
    ADD Player3ID INT NOT NULL DEFAULT 0;
END

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'StandingTeeTimeRequests' AND COLUMN_NAME = 'Player4ID')
BEGIN
    ALTER TABLE StandingTeeTimeRequests
    ADD Player4ID INT NOT NULL DEFAULT 0;
END

-- Check if standing tee time related columns exist in Reservations and add them if they don't
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Reservations' AND COLUMN_NAME = 'IsStandingTeeTime')
BEGIN
    ALTER TABLE Reservations
    ADD IsStandingTeeTime BIT NOT NULL DEFAULT 0;
END

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Reservations' AND COLUMN_NAME = 'StandingRequestID')
BEGIN
    ALTER TABLE Reservations
    ADD StandingRequestID INT NULL;
END

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Reservations' AND COLUMN_NAME = 'Notes')
BEGIN
    ALTER TABLE Reservations
    ADD Notes NVARCHAR(200) NULL;
END

-- Add foreign key constraints if they don't exist
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS WHERE CONSTRAINT_NAME = 'FK_Reservations_StandingTeeTimeRequests')
BEGIN
    ALTER TABLE Reservations
    ADD CONSTRAINT FK_Reservations_StandingTeeTimeRequests
    FOREIGN KEY (StandingRequestID)
    REFERENCES StandingTeeTimeRequests(RequestID);
END

-- Add foreign keys for players in StandingTeeTimeRequests
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS WHERE CONSTRAINT_NAME = 'FK_StandingTeeTimeRequests_Members_Player2')
BEGIN
    ALTER TABLE StandingTeeTimeRequests
    ADD CONSTRAINT FK_StandingTeeTimeRequests_Members_Player2
    FOREIGN KEY (Player2ID)
    REFERENCES Members(MemberID);
END

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS WHERE CONSTRAINT_NAME = 'FK_StandingTeeTimeRequests_Members_Player3')
BEGIN
    ALTER TABLE StandingTeeTimeRequests
    ADD CONSTRAINT FK_StandingTeeTimeRequests_Members_Player3
    FOREIGN KEY (Player3ID)
    REFERENCES Members(MemberID);
END

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS WHERE CONSTRAINT_NAME = 'FK_StandingTeeTimeRequests_Members_Player4')
BEGIN
    ALTER TABLE StandingTeeTimeRequests
    ADD CONSTRAINT FK_StandingTeeTimeRequests_Members_Player4
    FOREIGN KEY (Player4ID)
    REFERENCES Members(MemberID);
END

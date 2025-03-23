-- SQL script to remove the Email field from MemberUpgrades table
IF EXISTS (
    SELECT 1
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'MemberUpgrades'
    AND COLUMN_NAME = 'Email'
)
BEGIN
    ALTER TABLE MemberUpgrades
    DROP COLUMN Email;
    
    PRINT 'Email column successfully removed from MemberUpgrades table';
END
ELSE
BEGIN
    PRINT 'Email column does not exist in MemberUpgrades table';
END

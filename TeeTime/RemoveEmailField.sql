-- SQL script to remove the Email field from MemberUpgrades table
IF COL_LENGTH('MemberUpgrades', 'Email') IS NOT NULL
BEGIN
    EXEC('ALTER TABLE MemberUpgrades DROP COLUMN Email');
    PRINT 'Email column successfully removed from MemberUpgrades table';
END
ELSE
BEGIN
    PRINT 'Email column does not exist in MemberUpgrades table';
END

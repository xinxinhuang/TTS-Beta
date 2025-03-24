using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeeTime.Migrations
{
    /// <inheritdoc />
    public partial class AddEventRelationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Check if EventID column exists
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE Name = N'EventID' 
                               AND Object_ID = Object_ID(N'ScheduledGolfTimes'))
                BEGIN
                    ALTER TABLE ScheduledGolfTimes ADD EventID INT NULL;
                END");
            
            // Create index if it doesn't exist
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ScheduledGolfTimes_EventID' 
                               AND object_id = OBJECT_ID('ScheduledGolfTimes'))
                BEGIN
                    CREATE INDEX IX_ScheduledGolfTimes_EventID ON ScheduledGolfTimes(EventID);
                END");
            
            // Add foreign key constraint
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_ScheduledGolfTimes_Events_EventID' 
                               AND parent_object_id = OBJECT_ID('ScheduledGolfTimes'))
                BEGIN
                    ALTER TABLE ScheduledGolfTimes  
                    ADD CONSTRAINT FK_ScheduledGolfTimes_Events_EventID FOREIGN KEY (EventID)  
                    REFERENCES Events (EventID)
                    ON DELETE SET NULL;
                END");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop foreign key constraint if it exists
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_ScheduledGolfTimes_Events_EventID' 
                           AND parent_object_id = OBJECT_ID('ScheduledGolfTimes'))
                BEGIN
                    ALTER TABLE ScheduledGolfTimes DROP CONSTRAINT FK_ScheduledGolfTimes_Events_EventID;
                END");
            
            // Drop index if it exists
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ScheduledGolfTimes_EventID' 
                           AND object_id = OBJECT_ID('ScheduledGolfTimes'))
                BEGIN
                    DROP INDEX IX_ScheduledGolfTimes_EventID ON ScheduledGolfTimes;
                END");
            
            // Drop EventID column if it exists
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.columns WHERE Name = N'EventID' 
                           AND Object_ID = Object_ID(N'ScheduledGolfTimes'))
                BEGIN
                    ALTER TABLE ScheduledGolfTimes DROP COLUMN EventID;
                END");
        }
    }
}

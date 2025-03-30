-- SQL script to fix any existing standing tee time requests with approved times but incorrect status
-- This will ensure they are properly processed by the tee sheet generation

-- Update all standing tee time requests that have ApprovedTeeTime set but Status is not "Approved"
UPDATE StandingTeeTimeRequests
SET Status = 'Approved'
WHERE ApprovedTeeTime IS NOT NULL 
  AND (Status IS NULL OR Status != 'Approved')
  AND PriorityNumber IS NOT NULL;

-- Optional: Update any standing tee time requests that don't have ApprovedTeeTime but have a Status that's not "Pending"
UPDATE StandingTeeTimeRequests
SET Status = 'Pending'
WHERE ApprovedTeeTime IS NULL 
  AND (Status IS NULL OR Status != 'Pending');

-- Return the count of records updated for verification
SELECT 'Records with ApprovedTeeTime set to Approved status: ' + CAST(@@ROWCOUNT AS VARCHAR(10)) AS Message;

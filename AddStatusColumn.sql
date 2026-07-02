-- ========================================
-- ADD STATUS COLUMN TO USERMASTER TABLE
-- Run this in pgAdmin on your UserManagementDB
-- ========================================

-- Step 1: Add the status column
ALTER TABLE usermaster ADD COLUMN IF NOT EXISTS status VARCHAR(20) DEFAULT 'Pending';

-- Step 2: Set ALL existing users to 'Approved' (they were already registered before this feature)
UPDATE usermaster SET status = 'Approved';

-- Done! Now new registrations will be 'Pending' until admin approves them.

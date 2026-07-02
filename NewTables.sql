-- ============================================================
-- 🔥 NEW TABLES — Run this in pgAdmin on UserManagementDB
-- ============================================================

-- ✅ 1. ROLE MASTER TABLE
CREATE TABLE IF NOT EXISTS rolemaster (
    roleid SERIAL PRIMARY KEY,
    rolename VARCHAR(50) NOT NULL UNIQUE
);

-- Seed default roles (skip if already exist)
INSERT INTO rolemaster (roleid, rolename) VALUES (1, 'Admin') ON CONFLICT (roleid) DO NOTHING;
INSERT INTO rolemaster (roleid, rolename) VALUES (2, 'User') ON CONFLICT (roleid) DO NOTHING;

-- ✅ 2. USER DOCUMENTS TABLE
CREATE TABLE IF NOT EXISTS userdocuments (
    documentid SERIAL PRIMARY KEY,
    userid INT NOT NULL,
    documenttype VARCHAR(50) NOT NULL,
    filename VARCHAR(255) NOT NULL,
    filepath VARCHAR(500) NOT NULL,
    uploaddate TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    status VARCHAR(20) DEFAULT 'Pending'
);

-- ============================================================
-- ✅ VERIFY: Run these to check tables were created
-- ============================================================
-- SELECT * FROM rolemaster;
-- SELECT * FROM userdocuments;

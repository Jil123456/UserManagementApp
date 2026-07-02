-- ============================================================
-- 🔥 FULL DATABASE SCHEMA FOR USER MANAGEMENT APP (POSTGRESQL)
-- Run this single file in pgAdmin to create all necessary tables.
-- ============================================================

CREATE TABLE IF NOT EXISTS rolemaster (
    roleid SERIAL PRIMARY KEY,
    rolename VARCHAR(50) NOT NULL UNIQUE
);

CREATE TABLE IF NOT EXISTS usermaster (
    userid SERIAL PRIMARY KEY,
    fullname VARCHAR(100) NOT NULL,
    username VARCHAR(100) NOT NULL UNIQUE,
    password VARCHAR(255) NOT NULL,
    email VARCHAR(100) NOT NULL UNIQUE,
    mobile VARCHAR(20) NOT NULL,
    dob TIMESTAMP,
    roleid INT REFERENCES rolemaster(roleid),
    createddate TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    status VARCHAR(20) DEFAULT 'Pending',
    rejectreason TEXT,
    has_unread_approval BOOLEAN DEFAULT false,
    actionbyadmin VARCHAR(100),
    is_active BOOLEAN DEFAULT true,
    is_deleted BOOLEAN DEFAULT false,
    doc_status VARCHAR(20) DEFAULT 'pending',
    upload_attempts INT DEFAULT 0
);

CREATE TABLE IF NOT EXISTS userdocuments (
    documentid SERIAL PRIMARY KEY,
    userid INT REFERENCES usermaster(userid),
    documenttype VARCHAR(50) NOT NULL,
    filename VARCHAR(255) NOT NULL,
    filepath VARCHAR(500) NOT NULL,
    uploaddate TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    status VARCHAR(20) DEFAULT 'Pending'
);

CREATE TABLE IF NOT EXISTS auditlogs (
    logid SERIAL PRIMARY KEY,
    actiontype VARCHAR(100) NOT NULL,
    performedby VARCHAR(100) NOT NULL,
    entitytype VARCHAR(100) NOT NULL,
    entityid INT,
    timestamp TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    details TEXT,
    ipaddress VARCHAR(50),
    useragent VARCHAR(500),
    severity VARCHAR(20) DEFAULT 'Info'
);

CREATE TABLE IF NOT EXISTS userappeals (
    appealid SERIAL PRIMARY KEY,
    userid INT REFERENCES usermaster(userid),
    message TEXT NOT NULL,
    sentby VARCHAR(50) DEFAULT 'User',
    sentdate TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Seed default roles
INSERT INTO rolemaster (roleid, rolename) VALUES (1, 'Admin') ON CONFLICT (roleid) DO NOTHING;
INSERT INTO rolemaster (roleid, rolename) VALUES (2, 'User') ON CONFLICT (roleid) DO NOTHING;

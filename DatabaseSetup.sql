-- ============================================================
-- 🔥 UserManagementApp - PostgreSQL Stored Functions Setup
-- ============================================================
-- Run this ENTIRE script in PostgreSQL (pgAdmin or psql)
-- Database: UserManagementDB
-- ============================================================

-- ✅ 1. GET ALL USERS
CREATE OR REPLACE FUNCTION get_all_users()
RETURNS TABLE (
    userid INT,
    fullname VARCHAR,
    username VARCHAR,
    password VARCHAR,
    email VARCHAR,
    mobile VARCHAR,
    dob TIMESTAMP,
    roleid INT,
    createddate TIMESTAMP
)
AS $$
BEGIN
    RETURN QUERY SELECT u.userid, u.fullname, u.username, u.password, u.email, u.mobile, u.dob, u.roleid, u.createddate FROM usermaster u ORDER BY u.userid;
END;
$$ LANGUAGE plpgsql;

-- ✅ 2. GET USER BY ID
CREATE OR REPLACE FUNCTION get_user_by_id(p_userid INT)
RETURNS TABLE (
    userid INT,
    fullname VARCHAR,
    username VARCHAR,
    password VARCHAR,
    email VARCHAR,
    mobile VARCHAR,
    dob TIMESTAMP,
    roleid INT,
    createddate TIMESTAMP
)
AS $$
BEGIN
    RETURN QUERY SELECT u.userid, u.fullname, u.username, u.password, u.email, u.mobile, u.dob, u.roleid, u.createddate FROM usermaster u WHERE u.userid = p_userid;
END;
$$ LANGUAGE plpgsql;

-- ✅ 3. GET USER BY USERNAME
CREATE OR REPLACE FUNCTION get_user_by_username(p_username VARCHAR)
RETURNS TABLE (
    userid INT,
    fullname VARCHAR,
    username VARCHAR,
    password VARCHAR,
    email VARCHAR,
    mobile VARCHAR,
    dob TIMESTAMP,
    roleid INT,
    createddate TIMESTAMP
)
AS $$
BEGIN
    RETURN QUERY SELECT u.userid, u.fullname, u.username, u.password, u.email, u.mobile, u.dob, u.roleid, u.createddate FROM usermaster u WHERE u.username = p_username;
END;
$$ LANGUAGE plpgsql;

-- ✅ 4. ADD USER (with duplicate username check)
CREATE OR REPLACE FUNCTION add_user(
    p_fullname VARCHAR,
    p_username VARCHAR,
    p_password VARCHAR,
    p_email VARCHAR,
    p_mobile VARCHAR,
    p_dob TIMESTAMP,
    p_roleid INT
)
RETURNS VOID AS $$
BEGIN
    -- Check if username already exists
    IF EXISTS (SELECT 1 FROM usermaster WHERE username = p_username) THEN
        RAISE EXCEPTION 'Username % already exists', p_username;
    END IF;

    INSERT INTO usermaster (fullname, username, password, email, mobile, dob, roleid, createddate)
    VALUES (p_fullname, p_username, p_password, p_email, p_mobile, p_dob, p_roleid, CURRENT_TIMESTAMP);
END;
$$ LANGUAGE plpgsql;

-- ✅ 5. UPDATE USER (with duplicate username check for other users)
CREATE OR REPLACE FUNCTION update_user(
    p_userid INT,
    p_fullname VARCHAR,
    p_username VARCHAR,
    p_email VARCHAR,
    p_mobile VARCHAR
)
RETURNS VOID AS $$
BEGIN
    -- Check if username is taken by another user
    IF EXISTS (SELECT 1 FROM usermaster WHERE username = p_username AND userid != p_userid) THEN
        RAISE EXCEPTION 'Username % is already taken by another user', p_username;
    END IF;

    UPDATE usermaster
    SET fullname = p_fullname,
        username = p_username,
        email = p_email,
        mobile = p_mobile
    WHERE userid = p_userid;
END;
$$ LANGUAGE plpgsql;

-- ✅ 6. DELETE USER (prevents deleting the last admin)
CREATE OR REPLACE FUNCTION delete_user(p_userid INT)
RETURNS VOID AS $$
DECLARE
    v_roleid INT;
    v_admin_count INT;
BEGIN
    -- Get the role of user being deleted
    SELECT roleid INTO v_roleid FROM usermaster WHERE userid = p_userid;

    -- If user is admin, check if they are the last one
    IF v_roleid = 1 THEN
        SELECT COUNT(*) INTO v_admin_count FROM usermaster WHERE roleid = 1;
        IF v_admin_count <= 1 THEN
            RAISE EXCEPTION 'Cannot delete the last admin user!';
        END IF;
    END IF;

    DELETE FROM usermaster WHERE userid = p_userid;
END;
$$ LANGUAGE plpgsql;

-- ✅ 7. GET DASHBOARD STATS (NEW)
CREATE OR REPLACE FUNCTION get_dashboard_stats()
RETURNS TABLE (
    totalusers INT,
    totaladmins INT,
    totalstandardusers INT
)
AS $$
BEGIN
    RETURN QUERY
    SELECT
        COUNT(*)::INT AS totalusers,
        COUNT(*) FILTER (WHERE roleid = 1)::INT AS totaladmins,
        COUNT(*) FILTER (WHERE roleid = 2)::INT AS totalstandardusers
    FROM usermaster;
END;
$$ LANGUAGE plpgsql;

-- ============================================================
-- ✅ VERIFICATION: Test the functions
-- ============================================================
-- SELECT * FROM get_all_users();
-- SELECT * FROM get_dashboard_stats();
-- SELECT * FROM get_user_by_username('admin');

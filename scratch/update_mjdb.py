with open('Database/MJdatabase.cs', 'r', encoding='utf-8') as f:
    content = f.read()

target = """                cmd.CommandText = @"INSERT INTO Users (Username, Password, Role, FullName, ContactNumber, IsActive, CreatedAt, SecurityQuestion, SecurityAnswer, LastName, FirstName, MiddleName)

                                    VALUES (@u, @p, @r, @f, @c, 1, @now, @sq, @sa, @ln, @fn, @mn)";

                cmd.Parameters.AddWithValue("@u", user.Username.Trim());

                cmd.Parameters.AddWithValue("@p", HashPassword(user.Password));

                cmd.Parameters.AddWithValue("@r", "Office Staff");"""

replacement = """                cmd.CommandText = @"INSERT INTO Users (Username, Password, Role, FullName, ContactNumber, IsActive, CreatedAt, SecurityQuestion, SecurityAnswer, LastName, FirstName, MiddleName)

                                    VALUES (@u, @p, @r, @f, @c, @active, @now, @sq, @sa, @ln, @fn, @mn)";

                cmd.Parameters.AddWithValue("@u", user.Username.Trim());

                cmd.Parameters.AddWithValue("@p", HashPassword(user.Password));

                cmd.Parameters.AddWithValue("@r", string.IsNullOrWhiteSpace(user.Role) ? "Office Staff" : user.Role.Trim());

                cmd.Parameters.AddWithValue("@active", user.IsActive ? 1 : 0);"""

assert target in content, 'target not found in MJdatabase.cs'
content = content.replace(target, replacement)

target_log = 'LogSecurityEvent(user.Username, "ACCOUNT_REGISTERED", $"Staff self-registration: {user.FullName} ({user.ContactNumber})");'
replacement_log = 'LogSecurityEvent(user.Username, "ACCOUNT_REGISTERED", $"Staff user registered ({user.Role}): {user.FullName} ({user.ContactNumber})");'
assert target_log in content, 'target_log not found in MJdatabase.cs'
content = content.replace(target_log, replacement_log)

with open('Database/MJdatabase.cs', 'w', encoding='utf-8', newline='\r\n') as f:
    f.write(content)
print('Updated Database/MJdatabase.cs successfully')

using TDMS.Verify;

// Test login for a walkthrough on a COPY of the data: dotnet run --project Verify -- --make-test-user <copy db file> <output file>
// Refuses the shared live data folder. The generated password is written only to the output file.
if (args.Length >= 3 && args[0] == "--make-test-user")
{
    string db = Path.GetFullPath(args[1]);
    if (db.Contains(@"\ProgramData\MJ Billones Transport\", StringComparison.OrdinalIgnoreCase)) { Console.WriteLine("refused: that is the live data folder"); return 2; }
    Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database.MJdatabase.UseDatabaseFile(db);
    string pw = "Walk-" + Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(6));
    Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database.MJdatabase.SaveUser(new Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models.User
    { Username = "walkthrough", Password = pw, Role = "Admin", FullName = "Walkthrough Test", FirstName = "Walkthrough", LastName = "Test", IsActive = true });
    File.WriteAllText(args[2], "walkthrough\n" + pw + "\n");
    Console.WriteLine("test user created in " + db);
    return 0;
}

// Read-only query helper for investigations: dotnet run --project Verify -- --sql <db file> "<SELECT ...>"
if (args.Length >= 3 && args[0] == "--sql")
{
    using var con = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={args[1]};Mode=ReadOnly;Pooling=False");
    con.Open();
    foreach (var sql in args.Skip(2))
    {
        Console.WriteLine("-- " + sql);
        using var cmd = con.CreateCommand(); cmd.CommandText = sql;
        using var r = cmd.ExecuteReader();
        while (r.Read()) Console.WriteLine(string.Join(" | ", Enumerable.Range(0, r.FieldCount).Select(i => r.GetName(i) + "=" + r.GetValue(i))));
    }
    return 0;
}

// Usage: dotnet run --project Verify [-- <path to a real mj_billones.db to copy>] [-- filter]
// Exit code = number of failed checks. The real database is only ever copied, never opened for writing.
string? realDb = args.FirstOrDefault(a => a.EndsWith(".db", StringComparison.OrdinalIgnoreCase));
string? filter = args.FirstOrDefault(a => !a.StartsWith("-") && !a.EndsWith(".db", StringComparison.OrdinalIgnoreCase));
Checks.RealDb = realDb;
// Same start-up as the app (ApplicationConfiguration.Initialize), so screens are tested as users see them.
Application.EnableVisualStyles();
Application.SetCompatibleTextRenderingDefault(false);
Application.SetHighDpiMode(HighDpiMode.SystemAware);
if (args.Length >= 1 && args[0] == "--shot") return Checks.Shot(args);
int failed = Checks.RunAll(filter);
Console.WriteLine(failed == 0 ? "ALL PASS" : $"{failed} FAILED");
return failed;

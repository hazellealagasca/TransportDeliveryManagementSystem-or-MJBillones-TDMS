using TDMS.Verify;

// Usage: dotnet run --project Verify [-- <path to a real mj_billones.db to copy>] [-- filter]
// Exit code = number of failed checks. The real database is only ever copied, never opened for writing.
string? realDb = args.FirstOrDefault(a => a.EndsWith(".db", StringComparison.OrdinalIgnoreCase));
string? filter = args.FirstOrDefault(a => !a.StartsWith("-") && !a.EndsWith(".db", StringComparison.OrdinalIgnoreCase));
Checks.RealDb = realDb;
// Same start-up as the app (ApplicationConfiguration.Initialize), so screens are tested as users see them.
Application.EnableVisualStyles();
Application.SetCompatibleTextRenderingDefault(false);
Application.SetHighDpiMode(HighDpiMode.SystemAware);
int failed = Checks.RunAll(filter);
Console.WriteLine(failed == 0 ? "ALL PASS" : $"{failed} FAILED");
return failed;

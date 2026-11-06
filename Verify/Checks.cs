using System.Reflection;
using Microsoft.Data.Sqlite;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;

namespace TDMS.Verify;

/// <summary>Marks a check. Each check gets its own fresh database (or a copy of the real one when it asks).</summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class CheckAttribute : Attribute { }

public static partial class Checks
{
    public static string? RealDb;
    private static readonly string Root = Path.Combine(Path.GetTempPath(), "tdms_verify");

    /// <summary>A new empty database with the current schema.</summary>
    public static string Fresh()
    {
        Directory.CreateDirectory(Root);
        string p = Path.Combine(Root, $"fresh_{Guid.NewGuid():N}.db");
        MJdatabase.UseDatabaseFile(p);
        MJdatabase.InitializeDatabase();
        return p;
    }

    /// <summary>A copy of the real database (SQLite backup, so the -wal is included). Null when no real database was given.</summary>
    public static string? FromCopy()
    {
        if (RealDb == null || !File.Exists(RealDb)) return null;
        Directory.CreateDirectory(Root);
        string p = Path.Combine(Root, $"copy_{Guid.NewGuid():N}.db");
        using (var src = new SqliteConnection($"Data Source={RealDb};Mode=ReadOnly;Pooling=False;"))
        using (var dst = new SqliteConnection($"Data Source={p};Pooling=False;"))
        { src.Open(); dst.Open(); src.BackupDatabase(dst); }
        MJdatabase.UseDatabaseFile(p);
        return p;
    }

    // ---- small SQL helpers on the database the data layer currently uses
    public static SqliteConnection Open() { var c = new SqliteConnection($"Data Source={MJdatabase.DbFilePath};Pooling=False;"); c.Open(); return c; }
    public static object? Scalar(string sql, params (string, object)[] ps)
    {
        using var c = Open(); using var cmd = c.CreateCommand(); cmd.CommandText = sql;
        foreach (var (n, v) in ps) cmd.Parameters.AddWithValue(n, v);
        return cmd.ExecuteScalar();
    }
    public static long Long(string sql, params (string, object)[] ps) => Convert.ToInt64(Scalar(sql, ps) ?? 0L);
    public static string Str(string sql, params (string, object)[] ps) => Convert.ToString(Scalar(sql, ps)) ?? "";
    public static long Exec(string sql, params (string, object)[] ps)
    {
        using var c = Open(); using var cmd = c.CreateCommand(); cmd.CommandText = sql;
        foreach (var (n, v) in ps) cmd.Parameters.AddWithValue(n, v);
        cmd.ExecuteNonQuery();
        cmd.CommandText = "SELECT last_insert_rowid()"; cmd.Parameters.Clear();
        return Convert.ToInt64(cmd.ExecuteScalar() ?? 0L);
    }

    public static int RunAll(string? filter)
    {
        int failed = 0;
        var checks = typeof(Checks).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => m.GetCustomAttribute<CheckAttribute>() != null && (filter == null || m.Name.Contains(filter, StringComparison.OrdinalIgnoreCase)))
            .OrderBy(m => m.Name).ToList();
        Console.WriteLine($"RUN {checks.Count} check(s){(filter == null ? "" : " matching '" + filter + "'")}; real db: {(RealDb ?? "(none)")}");
        foreach (var m in checks)
        {
            string result;
            try
            {
                object? r = m.Invoke(null, null);
                result = r is bool b ? (b ? "PASS" : "FAIL") : r is string s ? (s.Length == 0 ? "PASS" : "FAIL (" + s + ")") : "PASS";
            }
            catch (TargetInvocationException ex) { result = "FAIL (" + ex.InnerException?.GetType().Name + ": " + ex.InnerException?.Message + ")"; }
            if (result.StartsWith("FAIL")) failed++;
            Console.WriteLine($"{result.Split(' ')[0]} {m.Name}{(result.Length > 4 ? " " + result.Substring(5) : "")}");
        }
        return failed;
    }

    [Check]
    public static bool SmokeOpensFreshDb()
    {
        Fresh();
        return Long("SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='TripReports'") == 1;
    }
}

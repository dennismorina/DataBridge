namespace DataBridge.Cli;

internal static class CliOptionsParser
{
    public static bool TryParse(
        string[] args,
        out CliOptions? options,
        out string? error)
    {
        options = null;
        error = null;

        if (args.Length == 0 || IsHelp(args[0]))
        {
            return false;
        }

        if (!string.Equals(args[0], "import", StringComparison.OrdinalIgnoreCase))
        {
            error = $"Unknown command '{args[0]}'.";
            return false;
        }

        string? filePath = null;
        string? rejectFilePath = null;
        string? connectionString = null;
        var dryRun = false;
        var force = false;
        var batchSize = 1_000;

        for (var index = 1; index < args.Length; index++)
        {
            switch (args[index])
            {
                case "--file":
                    if (!TryReadValue(args, ref index, out filePath))
                    {
                        error = "--file requires a value.";
                        return false;
                    }
                    break;

                case "--reject-file":
                    if (!TryReadValue(args, ref index, out rejectFilePath))
                    {
                        error = "--reject-file requires a value.";
                        return false;
                    }
                    break;

                case "--connection":
                    if (!TryReadValue(args, ref index, out connectionString))
                    {
                        error = "--connection requires a value.";
                        return false;
                    }
                    break;

                case "--batch-size":
                    if (!TryReadValue(args, ref index, out var batchSizeValue)
                        || !int.TryParse(batchSizeValue, out batchSize)
                        || batchSize is < 1 or > 50_000)
                    {
                        error = "--batch-size must be an integer between 1 and 50,000.";
                        return false;
                    }
                    break;

                case "--dry-run":
                    dryRun = true;
                    break;

                case "--force":
                    force = true;
                    break;

                case "--help":
                case "-h":
                    return false;

                default:
                    error = $"Unknown option '{args[index]}'.";
                    return false;
            }
        }

        if (string.IsNullOrWhiteSpace(filePath))
        {
            error = "--file is required.";
            return false;
        }

        rejectFilePath ??= Path.Combine(
            "output",
            $"rejected-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv");

        connectionString ??= Environment.GetEnvironmentVariable(
            "DATABRIDGE_CONNECTION_STRING");

        if (!dryRun && string.IsNullOrWhiteSpace(connectionString))
        {
            error = "A PostgreSQL connection string is required. Use --connection or DATABRIDGE_CONNECTION_STRING.";
            return false;
        }

        options = new CliOptions(
            filePath,
            rejectFilePath,
            connectionString,
            dryRun,
            force,
            batchSize);

        return true;
    }

    public static void PrintHelp()
    {
        Console.WriteLine(
            """
            DataBridge - production-oriented CSV/XLSX import pipeline

            Usage:
              DataBridge import --file <path> [options]

            Options:
              --file <path>          CSV or XLSX source file (required)
              --reject-file <path>   CSV report for rejected rows
              --connection <value>   PostgreSQL connection string
              --batch-size <n>       Upsert batch size, default: 1000
              --dry-run              Parse and validate without database writes
              --force                Re-import an already processed source
              --help, -h             Show help

            Environment:
              DATABRIDGE_CONNECTION_STRING
            """);
    }

    private static bool TryReadValue(
        string[] args,
        ref int index,
        out string? value)
    {
        value = null;

        if (index + 1 >= args.Length)
        {
            return false;
        }

        index++;
        value = args[index];
        return true;
    }

    private static bool IsHelp(string value) =>
        string.Equals(value, "--help", StringComparison.OrdinalIgnoreCase)
        || string.Equals(value, "-h", StringComparison.OrdinalIgnoreCase)
        || string.Equals(value, "help", StringComparison.OrdinalIgnoreCase);
}

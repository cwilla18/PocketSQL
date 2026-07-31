using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PocketSQL.Data;
using PocketSQL.Interfaces;
using System;
using System.CommandLine;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace PocketSQL;

class Program
{
    static void Main(string[] args)
    {
        try
        {
            using var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder
                    .AddFilter("Microsoft", LogLevel.Warning)
                    .AddFilter("System", LogLevel.Warning)
                    .AddFilter("PocketSQL", LogLevel.Debug)
                    .AddConsole();
            });

            Option<FileInfo> fileOption = new("--file", "-f")
            {
                Description = "The Json or CSV file to read into the console"
            };

            RootCommand rootCommand = new("PocketSQL - A simple SQL engine for JSON and CSV files")
            {
                fileOption
            };

            ILogger logger = loggerFactory.CreateLogger<Program>();

            ParseResult parseResult = rootCommand.Parse(args);

            if (parseResult.Errors.Count > 0)
            {
                var errors = string.Join("; ", parseResult.Errors.Select(e => e.Message));
                throw new ArgumentException($"Issue parsing arguments: {errors}");
            }

            var filePath = parseResult.GetValue<FileInfo>(fileOption);

            if (!ValidateFile(filePath, logger))
            {
                logger.LogError($"Invalid file: {filePath?.FullName}");
                return;
            }

            logger.LogInformation($"Reading file: {filePath.FullName}");
            
            GenerateTable generateTable = new(logger);
            var dataTable = generateTable.PopulateTableData(filePath);

            if(dataTable is null)
            {
                logger.LogError("Failed to populate data table from the file.");
                return;
            }

            QueryData queryData = new(dataTable, logger);
            queryData.Query();

        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
        }

    }

    private static bool ValidateFile([NotNullWhen(true)] FileInfo? file, ILogger logger)
    {
        if (file == null || !file.Exists)
        {
            logger.LogError($"The file '{file?.FullName}' does not exist.");
            return false;
        }
        var extension = Path.GetExtension(file.FullName).ToLowerInvariant();
        if (extension != ".json" && extension != ".csv")
        {
            logger.LogError($"Error: The file '{file.FullName}' is not a valid JSON or CSV file.");
            return false;
        }
        return true;
    }
}

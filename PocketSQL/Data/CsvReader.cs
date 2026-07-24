using Microsoft.Extensions.Logging;
using PocketSQL.Interfaces;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace PocketSQL.Data
{
    public class CsvReader : IFileProcessor
    {
        private string[] _fileContent = Array.Empty<string>();
        private readonly ILogger _logger;

        public Table DataTable { get; set; }

        public CsvReader(Table table, ILogger logger)
        {
            DataTable = table;
            _logger = logger;
        }

        public Table ProcessFile(FileInfo filePath)
        {
            _logger.LogInformation($"Processing CSV file: {filePath.FullName}");
            _fileContent = File.ReadAllLines(filePath.FullName);

            if (_fileContent.Length == 0)
            {
                _logger.LogError("Unable to parse CSV: file is empty.");
                throw new InvalidDataException("Unable to parse CSV: file is empty.");
            }

            var headers = ParseLine(_fileContent[0]).Select(h => h.Trim()).ToList();

            // Parse every data row up front so column types can be inferred from the values.
            var dataRows = new List<List<string>>();
            for (int i = 1; i < _fileContent.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(_fileContent[i]))
                {
                    continue;
                }
                dataRows.Add(ParseLine(_fileContent[i]));
            }

            var types = new List<Type?>(headers.Count);
            for (int col = 0; col < headers.Count; col++)
            {
                var columnValues = dataRows
                    .Where(r => col < r.Count)
                    .Select(r => r[col]);
                types.Add(InferColumnType(columnValues));
            }

            DataTable.AddColumn(headers, types);

            foreach (var values in dataRows)
            {
                var row = DataTable.NewRow();
                for (int col = 0; col < headers.Count; col++)
                {
                    var raw = col < values.Count ? values[col] : null;
                    row[headers[col]] = ConvertValue(raw, DataTable.DataTable.Columns[col].DataType);
                }
                DataTable.AddRow(row);
            }

            _logger.LogInformation($"CSV file processed successfully: {filePath.FullName}");
            return DataTable;
        }

        /// <summary>
        /// Splits a single CSV line, honouring double-quoted fields (which may contain
        /// commas) and escaped quotes ("") within them. Embedded newlines are not
        /// supported because the file is read line-by-line.
        /// </summary>
        private static List<string> ParseLine(string line)
        {
            var fields = new List<string>();
            var current = new StringBuilder();
            var inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                var c = line[i];
                if (inQuotes)
                {
                    if (c == '"')
                    {
                        if (i + 1 < line.Length && line[i + 1] == '"')
                        {
                            current.Append('"');
                            i++;
                        }
                        else
                        {
                            inQuotes = false;
                        }
                    }
                    else
                    {
                        current.Append(c);
                    }
                }
                else if (c == '"')
                {
                    inQuotes = true;
                }
                else if (c == ',')
                {
                    fields.Add(current.ToString());
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }

            fields.Add(current.ToString());
            return fields;
        }

        /// <summary>
        /// Picks the narrowest type that fits every non-empty value in the column,
        /// falling back to string when the values are mixed or empty.
        /// </summary>
        private static Type InferColumnType(IEnumerable<string> values)
        {
            var nonEmpty = values
                .Select(v => v?.Trim() ?? string.Empty)
                .Where(v => v.Length > 0)
                .ToList();

            if (nonEmpty.Count == 0)
            {
                return typeof(string);
            }
            if (nonEmpty.All(v => long.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out _)))
            {
                return typeof(long);
            }
            if (nonEmpty.All(v => double.TryParse(v, NumberStyles.Any, CultureInfo.InvariantCulture, out _)))
            {
                return typeof(double);
            }
            if (nonEmpty.All(v => bool.TryParse(v, out _)))
            {
                return typeof(bool);
            }
            if (nonEmpty.All(v => DateTime.TryParse(v, CultureInfo.InvariantCulture, DateTimeStyles.None, out _)))
            {
                return typeof(DateTime);
            }
            return typeof(string);
        }

        private static object ConvertValue(string? raw, Type type)
        {
            var trimmed = raw?.Trim();
            if (string.IsNullOrEmpty(trimmed))
            {
                return DBNull.Value;
            }
            if (type == typeof(long))
            {
                return long.Parse(trimmed, CultureInfo.InvariantCulture);
            }
            if (type == typeof(double))
            {
                return double.Parse(trimmed, NumberStyles.Any, CultureInfo.InvariantCulture);
            }
            if (type == typeof(bool))
            {
                return bool.Parse(trimmed);
            }
            if (type == typeof(DateTime))
            {
                return DateTime.Parse(trimmed, CultureInfo.InvariantCulture);
            }
            return trimmed;
        }
    }
}

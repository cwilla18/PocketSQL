using Microsoft.Extensions.Logging;
using PocketSQL.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PocketSQL.Data
{
    public sealed class GenerateTable
    {
        private readonly ILogger _logger;
        public GenerateTable(ILogger logger)
        {
            _logger = logger;
        }

        public Table PopulateTableData(FileInfo filePath)
        {
            Table dataTable = new();

            IFileProcessor reader = Path.GetExtension(filePath.FullName).ToLowerInvariant() switch
            {
                ".json" => new JsonReader(dataTable, _logger),
                ".csv" => new CsvReader(dataTable, _logger),
                _ => throw new NotSupportedException($"File extension '{Path.GetExtension(filePath.FullName)}' is not supported.")
            };

            dataTable = reader.ProcessFile(filePath);

            return dataTable;
        }
    }
}

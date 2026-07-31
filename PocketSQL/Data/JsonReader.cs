using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PocketSQL.Interfaces;
using System;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.IO;
using System.Linq;

namespace PocketSQL.Data
{
    public class JsonReader : IFileProcessor
    {
        private string _fileContent = string.Empty;
        private readonly ILogger _logger;
        public Table DataTable { get; set; }

        public JsonReader(Table table, ILogger logger)
        {
            DataTable = table;
            _logger = logger;
        }

        public Table ProcessFile(FileInfo filePath)
        {
            _logger.LogInformation($"Processing JSON file: {filePath.FullName}");
            _fileContent = File.ReadAllText(filePath.FullName);

            if (string.IsNullOrEmpty(_fileContent))
            {
                _logger.LogError("Unable to parse Json file: File content is empty or null.");
                throw new InvalidDataException("Unable to parse Json file: file content is empty.");
            }

            var jsonList = JsonConvert.DeserializeObject<List<ExpandoObject>>(_fileContent);

            if (jsonList is null)
            {
                _logger.LogError("Unable to parse Json file: deserialization returned null.");
                throw new InvalidDataException("Unable to parse Json file: deserialization returned null.");
            }

            var rows = jsonList.Where(item => item is not null).Select(item => (IDictionary<string, object?>)item!).ToList();

            // Column order follows first appearance across all rows.
            var headers = rows.SelectMany(item => item.Keys).Distinct().ToList();
            var types = headers.Select(key => (Type?)InferColumnType(rows, key)).ToList();

            DataTable.AddColumn(headers, types);

            foreach (var item in rows)
            {
                var row = DataTable.NewRow();
                foreach (var keyValuePair in item)
                {
                    row[keyValuePair.Key] = keyValuePair.Value ?? DBNull.Value;
                }
                DataTable.AddRow(row);
            }

            _logger.LogInformation($"Successfully processed JSON file: {filePath.FullName}");
            return DataTable;

        }

        private static Type InferColumnType(IEnumerable<IDictionary<string, object?>> rows, string key)
        {
            var distinctTypes = rows.Where(r => r.TryGetValue(key, out var value) && value is not null).Select(r => r[key]!.GetType()).Distinct().ToList();

            return distinctTypes.Count == 1 ? distinctTypes[0] : typeof(object);
        }
    }
}

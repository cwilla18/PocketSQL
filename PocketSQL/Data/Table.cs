using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PocketSQL.Data
{
    public class Table
    {
        public DataTable DataTable { get; init; } = new();

        public void AddColumn(List<string> headers, List<Type?> types)
        {
            if (headers.Count != types.Count)
            {
                throw new ArgumentException($"Header/type count mismatch: {headers.Count} headers but {types.Count} types.");
            }

            for (int i = 0; i < headers.Count; i++)
            {
                // Fall back to string for columns whose type could not be inferred.
                DataTable.Columns.Add(headers[i].Trim(), types[i] ?? typeof(string));
            }
        }

        public DataRow NewRow()
        {
            return DataTable.NewRow();
        }

        public void AddRow(DataRow row)
        {
            DataTable.Rows.Add(row);
        }

    }
}

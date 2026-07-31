using Microsoft.Extensions.Logging;
using PocketSQL.Data;
using PocketSQL.Lexing;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PocketSQL
{
    public class QueryData
    {
        private readonly Table _dataTable;
        private ILogger _logger;

        public QueryData(Table dataTable, ILogger logger)
        {
            _dataTable = dataTable;
            _logger = logger;
        }

        public void Query()
        {
            var continueQuerying = true;

            while (continueQuerying)
            {
                Console.WriteLine("Enter a SQL query (or type 'exit' to quit):");
                var query = Console.ReadLine();
                
                if (query?.Trim().ToLower() == "exit")
                {
                    continueQuerying = false;
                    break;
                }
                try
                {
                    Lexer lexer = new (query);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error executing query: {ex.Message}");
                    Console.WriteLine($"Error executing query: {ex.Message}");
                }
            }
        }
    }
}

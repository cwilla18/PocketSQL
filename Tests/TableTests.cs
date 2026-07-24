using System;
using System.Data;
using System.IO;
using Microsoft.Extensions.Logging.Abstractions;
using PocketSQL.Data;

namespace Tests
{
    public class TableTests
    {
        [Fact]
        public void AddColumn_Throws_WhenHeaderAndTypeCountsDiffer()
        {
            var table = new Table();

            Assert.Throws<ArgumentException>(() =>
                table.AddColumn(new() { "a", "b" }, new() { typeof(string) }));
        }

        [Fact]
        public void AddColumn_FallsBackToString_WhenTypeIsNull()
        {
            var table = new Table();

            table.AddColumn(new() { "a" }, new() { (Type?)null });

            Assert.Equal(typeof(string), table.DataTable.Columns["a"]!.DataType);
        }
    }

    public class CsvReaderTests
    {
        [Fact]
        public void ProcessFile_InfersColumnTypesFromValues()
        {
            using var file = new TempFile(
                "id,name,score",
                "1,Alice,9.5",
                "2,Bob,8.0");

            var table = ReadCsv(file.Path);
            var columns = table.DataTable.Columns;

            Assert.Equal(typeof(long), columns["id"]!.DataType);
            Assert.Equal(typeof(string), columns["name"]!.DataType);
            Assert.Equal(typeof(double), columns["score"]!.DataType);
            Assert.Equal(2, table.DataTable.Rows.Count);
            Assert.Equal(1L, table.DataTable.Rows[0]["id"]);
            Assert.Equal(9.5d, table.DataTable.Rows[0]["score"]);
        }

        [Fact]
        public void ProcessFile_HonoursQuotedFieldsContainingCommas()
        {
            using var file = new TempFile(
                "id,name",
                "1,\"Smith, John\"");

            var table = ReadCsv(file.Path);

            Assert.Equal("Smith, John", table.DataTable.Rows[0]["name"]);
        }

        [Fact]
        public void ProcessFile_TreatsEmptyCellsAsDbNull()
        {
            using var file = new TempFile(
                "id,name",
                "1,");

            var table = ReadCsv(file.Path);

            Assert.Equal(DBNull.Value, table.DataTable.Rows[0]["name"]);
        }

        [Fact]
        public void ProcessFile_HeaderOnly_ProducesColumnsButNoRows()
        {
            using var file = new TempFile("id,name");

            var table = ReadCsv(file.Path);

            Assert.Equal(2, table.DataTable.Columns.Count);
            Assert.Empty(table.DataTable.Rows);
        }

        [Fact]
        public void ProcessFile_Throws_WhenFileIsEmpty()
        {
            using var file = new TempFile();

            Assert.Throws<InvalidDataException>(() => ReadCsv(file.Path));
        }

        private static Table ReadCsv(string path)
        {
            var reader = new CsvReader(new Table(), NullLogger.Instance);
            return reader.ProcessFile(new FileInfo(path));
        }
    }

    public class JsonReaderTests
    {
        [Fact]
        public void ProcessFile_InfersColumnTypesPerKey()
        {
            using var file = new TempFile(
                "[{\"id\":1,\"name\":\"Alice\"},{\"id\":2,\"name\":\"Bob\"}]");

            var table = ReadJson(file.Path);

            Assert.Equal(typeof(long), table.DataTable.Columns["id"]!.DataType);
            Assert.Equal(typeof(string), table.DataTable.Columns["name"]!.DataType);
            Assert.Equal(2, table.DataTable.Rows.Count);
        }

        [Fact]
        public void ProcessFile_FallsBackToObject_WhenKeyHasMixedTypes()
        {
            using var file = new TempFile(
                "[{\"v\":1},{\"v\":\"text\"}]");

            var table = ReadJson(file.Path);

            Assert.Equal(typeof(object), table.DataTable.Columns["v"]!.DataType);
        }

        [Fact]
        public void ProcessFile_FillsMissingKeysWithDbNull()
        {
            using var file = new TempFile(
                "[{\"id\":1,\"name\":\"Alice\"},{\"id\":2}]");

            var table = ReadJson(file.Path);

            Assert.Equal(DBNull.Value, table.DataTable.Rows[1]["name"]);
        }

        [Fact]
        public void ProcessFile_Throws_WhenFileIsEmpty()
        {
            using var file = new TempFile();

            Assert.Throws<InvalidDataException>(() => ReadJson(file.Path));
        }

        private static Table ReadJson(string path)
        {
            var reader = new JsonReader(new Table(), NullLogger.Instance);
            return reader.ProcessFile(new FileInfo(path));
        }
    }

    /// <summary>Writes the given lines to a temp file and deletes it on dispose.</summary>
    internal sealed class TempFile : IDisposable
    {
        public string Path { get; }

        public TempFile(params string[] lines)
        {
            Path = System.IO.Path.GetTempFileName();
            File.WriteAllLines(Path, lines);
        }

        public void Dispose()
        {
            if (File.Exists(Path))
            {
                File.Delete(Path);
            }
        }
    }
}

using PocketSQL.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PocketSQL.Interfaces
{
    public interface IFileProcessor
    {
        public Table ProcessFile(FileInfo filePath);
    }
}

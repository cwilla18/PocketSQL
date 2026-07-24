using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PocketSQL.Lexing
{
    internal enum TokenTypes
    {
        SELECT,
        FROM,
        WHERE,
        GROUPBY,
        ORDERBY,
        LIMIT
    }
}

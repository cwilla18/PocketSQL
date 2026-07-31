using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PocketSQL.Lexing
{
    public enum TokenTypes
    {
        // Special
        EOF,
        Unknown,

        // Identifiers and literals
        Identifier,
        StringLiteral,
        IntegerLiteral,
        FloatLiteral,

        // Punctuation
        Comma,          // ,
        Dot,            // .
        Star,           // *
        LeftParen,      // (
        RightParen,     // )
        Semicolon,      // ;

        // Operators
        Plus,           // +
        Minus,          // -
        Asterisk,       // *  (alias of Star if you prefer)
        Slash,          // /
        Percent,        // %
        Equal,          // =
        NotEqual,       // != or <>
        LessThan,       // <
        LessThanOrEqual,// <=
        GreaterThan,    // >
        GreaterThanOrEqual, // >=

        // SQL Keywords (lower/upper canonicalized by lexer)
        SELECT,
        FROM,
        WHERE,
        GROUP,
        BY,
        ORDER,
        LIMIT,
        DISTINCT,
        AND,
        OR,
        NOT,
        IS,
        NULL,
        LIKE,
        IN,
        BETWEEN,
        EXISTS
    }
}

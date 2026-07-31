using System;

namespace PocketSQL.Lexing
{
    internal readonly struct Token
    {
        public TokenTypes Type { get; }
        public int StartIndex { get; }
        public int Length { get; }
        public int Line { get; }
        public int Column { get; }

        public Token(TokenTypes type, int startIndex, int length, int line, int column)
        {
            Type = type;
            StartIndex = startIndex;
            Length = length;
            Line = line;
            Column = column;
        }

        public string GetText(ReadOnlyMemory<char> source)
        {
            return (StartIndex < 0 || Length <= 0) ? string.Empty : source.Slice(StartIndex, Length).ToString();
        }

        public override string ToString() => $"{Type} @({Line},{Column}) [{StartIndex}:{Length}]";
    }
}

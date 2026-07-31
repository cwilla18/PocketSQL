using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PocketSQL.Lexing
{
    internal sealed class Lexer
    {
        private readonly ReadOnlyMemory<char> _input;
        private ReadOnlySpan<char> _inputSpan => _input.Span;

        private int _position;
        private int _line = 1;
        private int _column = 1;

        public Lexer(string input)
        {
            _input = input.AsMemory();
            _position = 0;
        }

        public Token NextToken() => NextTokenInternal();

        public Token Peek(int lookahead = 0)
        {
            var saved = (_position, _line, _column);
            Token token = default;

            for (int i = 0; i <= lookahead; i++)
            { 
                token = NextTokenInternal(); 
            }

            (_position, _line, _column) = saved;
            
            return token;
        }

        private Token NextTokenInternal()
        {
            SkipWhitespaceAndComments();

            if (_position >= _inputSpan.Length)
            {
                return new Token(TokenTypes.EOF, _position, 0, _line, _column);
            }

            int start = _position;
            int line = _line;
            int col = _column;
            char c = _inputSpan[_position];

            // Identifiers / keywords or bare names
            if (IsIdentifierStart(c))
            { 
                return ReadIdentifierOrKeyword(start, line, col); 
            }

            // Numbers
            if (char.IsDigit(c))
            {
                return ReadNumber(start, line, col);
            }

            // String literal
            if (c == '\'' || c == '"')
            {
                return ReadString(start, line, col);
            }

            // Punctuation & operators
            return ReadOperatorOrPunctuator(start, line, col);
        }

        private void SkipWhitespaceAndComments()
        {
            while (_position < _inputSpan.Length)
            {
                char character = _inputSpan[_position];

                if (char.IsWhiteSpace(character))
                {
                    AdvanceChar(character);
                    continue;
                }

                // Line comment: --
                if (character == '-' && _position + 1 < _inputSpan.Length && _inputSpan[_position + 1] == '-')
                {
                    _position += 2;
                    _column += 2;

                    while (_position < _inputSpan.Length)
                    {
                        char nc = _inputSpan[_position++];
                        if (nc == '\n') { _line++; _column = 1; break; }
                        _column++;
                    }

                    continue;
                }

                // Block comment: /* ... */
                if (character == '/' && _position + 1 < _inputSpan.Length && _inputSpan[_position + 1] == '*')
                {
                    _position += 2;
                    _column += 2;

                    while (_position + 1 < _inputSpan.Length)
                    {
                        char newcharacter = _inputSpan[_position++];

                        if (newcharacter == '\n') 
                        { 
                            _line++; _column = 1; 
                            continue; 
                        }

                        _column++;
                        
                        if (newcharacter == '*' && _inputSpan[_position] == '/')
                        {
                            _position++; _column++;
                            break;
                        }
                    }
                    continue;
                }

                break;
            }
        }

        private Token ReadIdentifierOrKeyword(int start, int line, int col)
        {
            int p = _position;

            while (p < _inputSpan.Length && IsIdentifierPart(_inputSpan[p])) 
            { 
                p++; 
            }

            int len = p - start;
            var text = _inputSpan.Slice(start, len);
            // Advance position
            AdvanceBy(len);

            // Map to keyword if matches (case-insensitive)
            var upper = text.ToString().ToUpperInvariant();
            
            if (KeywordMap.TryGetValue(upper, out var kind))
            {
                return new Token(kind, start, len, line, col); 
            }

            return new Token(TokenTypes.Identifier, start, len, line, col);
        }

        private Token ReadNumber(int start, int line, int col)
        {
            int pos = _position;
            bool seenDot = false;

            while (pos < _inputSpan.Length)
            {
                char character = _inputSpan[pos];

                if (char.IsDigit(character)) 
                { 
                    pos++; 
                    continue; 
                }
                
                if (!seenDot && character == '.')
                {
                    seenDot = true;
                    pos++;
                    continue;
                }
                break;
            }

            int len = pos - start;
            AdvanceBy(len);

            return new Token(seenDot ? TokenTypes.FloatLiteral : TokenTypes.IntegerLiteral, start, len, line, col);
        }

        private Token ReadString(int start, int line, int col)
        {
            char quote = _inputSpan[_position];
            int pos = _position + 1;

            while (pos < _inputSpan.Length)
            {
                char character = _inputSpan[pos++];
                if (character == quote)
                {
                    // handle doubled quote as escape: ''
                    if (pos < _inputSpan.Length && _inputSpan[pos] == quote)
                    {
                        pos++; // consume escaped quote
                        continue;
                    }
                    break;
                }
                if (character == '\n') 
                { 
                    _line++; 
                    _column = 1; 
                }
            }

            int len = pos - start;
            AdvanceBy(len);

            return new Token(TokenTypes.StringLiteral, start, len, line, col);
        }

        private Token ReadOperatorOrPunctuator(int start, int line, int col)
        {
            char character = _inputSpan[_position];

            // Two-char operators
            if (_position + 1 < _inputSpan.Length)
            {
                char n = _inputSpan[_position + 1];
                // <=, >=, !=, <>
                if (character == '<' && n == '=') 
                { 
                    return MakeAndAdvance(TokenTypes.LessThanOrEqual, start, 2, line, col); 
                }
                if (character == '>' && n == '=') 
                { 
                    return MakeAndAdvance(TokenTypes.GreaterThanOrEqual, start, 2, line, col); 
                }
                if (character == '!' && n == '=') 
                { 
                    return MakeAndAdvance(TokenTypes.NotEqual, start, 2, line, col); 
                }
                if (character == '<' && n == '>') 
                { 
                    return MakeAndAdvance(TokenTypes.NotEqual, start, 2, line, col); 
                }
            }

            // Single-char mapping


            switch (character)
            {
                case ',': 
                    return MakeAndAdvance(TokenTypes.Comma, start, 1, line, col);
                case '.': 
                    return MakeAndAdvance(TokenTypes.Dot, start, 1, line, col);
                case '*': 
                    return MakeAndAdvance(TokenTypes.Asterisk, start, 1, line, col);
                case '(': 
                    return MakeAndAdvance(TokenTypes.LeftParen, start, 1, line, col);
                case ')': 
                    return MakeAndAdvance(TokenTypes.RightParen, start, 1, line, col);
                case ';': 
                    return MakeAndAdvance(TokenTypes.Semicolon, start, 1, line, col);
                case '+': 
                    return MakeAndAdvance(TokenTypes.Plus, start, 1, line, col);
                case '-': 
                    return MakeAndAdvance(TokenTypes.Minus, start, 1, line, col);
                case '/': 
                    return MakeAndAdvance(TokenTypes.Slash, start, 1, line, col);
                case '%': 
                    return MakeAndAdvance(TokenTypes.Percent, start, 1, line, col);
                case '=': 
                    return MakeAndAdvance(TokenTypes.Equal, start, 1, line, col);
                case '<': 
                    return MakeAndAdvance(TokenTypes.LessThan, start, 1, line, col);
                case '>': 
                    return MakeAndAdvance(TokenTypes.GreaterThan, start, 1, line, col);
                default:
                    AdvanceBy(1);
                    return new Token(TokenTypes.Unknown, start, 1, line, col);
            }
        }

        private Token MakeAndAdvance(TokenTypes type, int start, int length, int line, int col)
        {
            AdvanceBy(length);

            return new Token(type, start, length, line, col);
        }

        private void AdvanceBy(int n)
        {
            for (int i = 0; i < n && _position < _inputSpan.Length; i++)
            { 
                AdvanceChar(_inputSpan[_position]); 
            
            }
            _position += Math.Max(0, n - 0); // already advanced char counts in AdvanceChar
        }

        private void AdvanceChar(char c)
        {
            _position++;

            if (c == '\n') 
            { 
                _line++; 
                _column = 1; 
            }

            else _column++;
        }

        private static bool IsIdentifierStart(char c) => char.IsLetter(c) || c == '_' || c == '@';
        private static bool IsIdentifierPart(char c) => char.IsLetterOrDigit(c) || c == '_' || c == '@' || c == '$';

        private static readonly Dictionary<string, TokenTypes> KeywordMap = CreateKeywordMap();

        private static Dictionary<string, TokenTypes> CreateKeywordMap()
        {
            return new Dictionary<string, TokenTypes>(StringComparer.OrdinalIgnoreCase)
            {
                ["SELECT"] = TokenTypes.SELECT,
                ["FROM"] = TokenTypes.FROM,
                ["WHERE"] = TokenTypes.WHERE,
                ["GROUP"] = TokenTypes.GROUP,
                ["BY"] = TokenTypes.BY,
                ["ORDER"] = TokenTypes.ORDER,
                ["LIMIT"] = TokenTypes.LIMIT,
                ["DISTINCT"] = TokenTypes.DISTINCT,
                ["AND"] = TokenTypes.AND,
                ["OR"] = TokenTypes.OR,
                ["NOT"] = TokenTypes.NOT,
                ["IS"] = TokenTypes.IS,
                ["NULL"] = TokenTypes.NULL,
                ["LIKE"] = TokenTypes.LIKE,
                ["IN"] = TokenTypes.IN,
                ["BETWEEN"] = TokenTypes.BETWEEN,
                ["EXISTS"] = TokenTypes.EXISTS,
            };
        }
    }
}


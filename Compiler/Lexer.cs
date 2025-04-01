namespace Compiler;

public class Lexer {
    private readonly string _source;
    private int _position = 0;
    private int _line = 1;
    private int _column = 1;

    private static readonly HashSet<string> Keywords = new() {
        "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked", "class", "const", "continue",
        "decimal", "default", "delegate", "do", "double", "else", "enum", "event", "explicit", "extern", "false", "finally",
        "fixed", "float", "for", "foreach", "goto", "if", "implicit", "in", "int", "interface", "internal", "is", "lock",
        "long", "namespace", "new", "null", "object", "operator", "out", "override", "params", "private", "protected", "public",
        "readonly", "ref", "return", "sbyte", "sealed", "short", "sizeof", "stackalloc", "static", "string", "struct",
        "switch", "this", "throw", "true", "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort", "using",
        "virtual", "void", "volatile", "while", "yield", "record", "init", "var", "dynamic", "global", "required", "scoped",
        "nint", "nuint", "with", "when"
    };

    private static readonly HashSet<char> Punctuators = new() { '{', '}', '(', ')', '[', ']', ';', ':', ',', '#', '@' };

    private static readonly HashSet<string> Operators = new() {
        "+", "-", "*", "/", "%", "&", "|", "^", "<", ">", "!", "=", "~", "?", ":", ".", ",", "#", "+=", "-=", "*=", "/=", "%=", "&=", "|=", "^=", "<=", ">=", "!=", "?.", "??", "&&", "||", "++", "--", "<<", ">>", "->", "<<=", ">>=", ">>>"
    };

    public Lexer(string source) {
        _source = source;
    }

    public List<Token> Tokenize() {
        List<Token> tokens = new();

        while (_position < _source.Length) {
            char current = _source[_position];
            
            if (char.IsWhiteSpace(current)) {
                tokens.Add(ReadWhitespace());
            } else if (char.IsLetter(current) || current == '_') {
                tokens.Add(ReadIdentifier());
            } else if (char.IsDigit(current)) {
                tokens.Add(ReadNumber());
            } else if (current == '"') {
                tokens.Add(ReadString());
            } else if (current == '/' && Peek(1) == '/') {
                tokens.Add(ReadSingleLineComment());
            } else if (current == '/' && Peek(1) == '*') {
                tokens.Add(ReadMultiLineComment());
            } else if (Punctuators.Contains(current)) {
                tokens.Add(ReadPunctuation());
            } else if (IsOperatorStart(current)) {
                tokens.Add(ReadOperator());
            } else {
                tokens.Add(new Token(TokenType.Error, current.ToString(), _line, _column));
                Advance();
            }
        }

        tokens.Add(new Token(TokenType.EOF, "", _line, _column));
        return tokens;
    }

    private Token ReadWhitespace() {
        int start = _position;
        while (_position < _source.Length && char.IsWhiteSpace(_source[_position])) {
            if (_source[_position] == '\n') {
                _line++;
                _column = -1;
            }
            Advance();
        }
        return new Token(TokenType.Whitespace, _source[start.._position], _line, _column);
    }

    private Token ReadIdentifier() {
        int start = _position;
        while (_position < _source.Length && (char.IsLetterOrDigit(_source[_position]) || _source[_position] == '_')) {
            Advance();
        }
        string value = _source[start.._position];
        TokenType type = Keywords.Contains(value) ? TokenType.Keyword : TokenType.Identifier;
        return new Token(type, value, _line, _column);
    }

    private Token ReadString() {
        int start = _position;
        Advance();
        while (_position < _source.Length && _source[_position] != '"') {
            Advance();
        }
        if (_position >= _source.Length) {
            return new Token(TokenType.Error, "Unterminated string", _line, _column);
        }
        Advance();
        return new Token(TokenType.String, _source[start.._position], _line, _column);
    }

    private Token ReadPunctuation() {
        char current = _source[_position];
        Advance();
        return new Token(TokenType.Punctuation, current.ToString(), _line, _column);
    }

    private Token ReadOperator() {
        int start = _position;
        string op = _source[_position].ToString();
        Advance();
        while (_position < _source.Length) {
            string extendedOp = op + _source[_position];
            if (Operators.Contains(extendedOp)) {
                op = extendedOp;
                Advance();
            } else {
                break;
            }
        }
        return new Token(TokenType.Operator, op, _line, _column);
    }
    
    private Token ReadNumber() {
        int start = _position;
        bool isFloat = false;
        bool isHex = false;
        bool isBinary = false;
        bool isExponential = false;

        if (_source[_position] == '0' && (Peek(1) == 'x' || Peek(1) == 'X')) {
            isHex = true;
            Advance();
            Advance();
        } else if (_source[_position] == '0' && (Peek(1) == 'b' || Peek(1) == 'B')) {
            isBinary = true;
            Advance();
            Advance();
        }

        while (_position < _source.Length && (char.IsDigit(_source[_position]) || 
                                              (isHex && "abcdefABCDEF".Contains(_source[_position])) ||
                                              (_source[_position] == '.' && !isFloat && !isHex && !isBinary))) {
            if (_source[_position] == '.') isFloat = true;
            Advance();
        }

        // Обработка экспоненциальной записи (например, 1.23e+10)
        if (!isHex && !isBinary && (_position < _source.Length && (_source[_position] == 'e' || _source[_position] == 'E'))) {
            isExponential = true;
            Advance();
            
            // Возможный знак перед показателем степени (+ или -)
            if (_position < _source.Length && (_source[_position] == '+' || _source[_position] == '-')) {
                Advance();
            }
            
            // Чтение показателя степени
            while (_position < _source.Length && char.IsDigit(_source[_position])) {
                Advance();
            }
        }
        return new Token(TokenType.Number, _source[start.._position], _line, _column);
    }

    private Token ReadSingleLineComment() {
        int start = _position;
        while (_position < _source.Length && _source[_position] != '\n') {
            Advance();
        }
        return new Token(TokenType.Comment, _source[start.._position], _line, _column);
    }

    private Token ReadMultiLineComment() {
        int start = _position;
        Advance();
        Advance();
        while (_position < _source.Length - 1 && !(_source[_position] == '*' && _source[_position + 1] == '/')) {
            Advance();
        }
        if (_position >= _source.Length - 1) {
            return new Token(TokenType.Error, "Unterminated comment", _line, _column);
        }
        Advance();
        Advance();
        return new Token(TokenType.Comment, _source[start.._position], _line, _column);
    }

    private char Peek(int offset) {
        return _position + offset < _source.Length ? _source[_position + offset] : '\0';
    }

    private bool IsOperatorStart(char c) {
        return Operators.Contains(c.ToString());
    }

    private void Advance() {
        _position++;
        _column++;
    }
}

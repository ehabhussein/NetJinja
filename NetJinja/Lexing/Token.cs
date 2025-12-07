namespace NetJinja.Lexing;

/// <summary>
/// Token types recognized by the Jinja lexer.
/// </summary>
public enum TokenType
{
    // Literals
    Text,           // Raw text outside of any tags
    Integer,        // Integer literal
    Float,          // Floating point literal
    String,         // String literal

    // Identifiers and keywords
    Name,           // Variable/function name

    // Jinja delimiters
    VariableStart,  // {{
    VariableEnd,    // }}
    BlockStart,     // {%
    BlockEnd,       // %}
    CommentStart,   // {#
    CommentEnd,     // #}

    // Operators
    Pipe,           // |
    Dot,            // .
    Comma,          // ,
    Colon,          // :
    Tilde,          // ~
    LeftParen,      // (
    RightParen,     // )
    LeftBracket,    // [
    RightBracket,   // ]
    LeftBrace,      // {
    RightBrace,     // }
    Assign,         // =

    // Comparison operators
    Equal,          // ==
    NotEqual,       // !=
    LessThan,       // <
    LessThanOrEqual,// <=
    GreaterThan,    // >
    GreaterThanOrEqual, // >=

    // Arithmetic operators
    Plus,           // +
    Minus,          // -
    Multiply,       // *
    Divide,         // /
    FloorDivide,    // //
    Modulo,         // %
    Power,          // **

    // Keywords
    If,
    Elif,
    Else,
    Endif,
    For,
    Endfor,
    In,
    Not,
    And,
    Or,
    Is,
    Block,
    Endblock,
    Extends,
    Include,
    Import,
    From,
    As,
    Macro,
    Endmacro,
    Call,
    Endcall,
    Set,
    Endset,
    With,
    Endwith,
    Autoescape,
    Endautoescape,
    Raw,
    Endraw,
    True,
    False,
    None,
    Recursive,
    Continue,
    Break,

    // Special
    Eof,
    Whitespace,
}

/// <summary>
/// Represents a token in the template source.
/// </summary>
/// <param name="Type">The token type.</param>
/// <param name="Value">The string value of the token.</param>
/// <param name="Line">Line number (1-based).</param>
/// <param name="Column">Column number (1-based).</param>
public readonly record struct Token(TokenType Type, string Value, int Line, int Column)
{
    public override string ToString() => $"{Type}({Value}) at {Line}:{Column}";

    public bool IsKeyword => Type >= TokenType.If && Type <= TokenType.Break;
}

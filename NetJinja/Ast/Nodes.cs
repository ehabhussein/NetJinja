namespace NetJinja.Ast;

/// <summary>
/// Base class for all AST nodes.
/// </summary>
public abstract record Node(int Line, int Column);

/// <summary>
/// Base class for expression nodes (produce values).
/// </summary>
public abstract record Expression(int Line, int Column) : Node(Line, Column);

/// <summary>
/// Base class for statement nodes (control flow, output).
/// </summary>
public abstract record Statement(int Line, int Column) : Node(Line, Column);

#region Expressions

/// <summary>
/// Literal value: string, number, boolean, null.
/// </summary>
public sealed record LiteralExpression(object? Value, int Line, int Column) : Expression(Line, Column);

/// <summary>
/// Variable reference: {{ name }}
/// </summary>
public sealed record NameExpression(string Name, int Line, int Column) : Expression(Line, Column);

/// <summary>
/// Attribute access: obj.attr
/// </summary>
public sealed record GetAttrExpression(Expression Object, string Attribute, int Line, int Column) : Expression(Line, Column);

/// <summary>
/// Item access: obj[key]
/// </summary>
public sealed record GetItemExpression(Expression Object, Expression Key, int Line, int Column) : Expression(Line, Column);

/// <summary>
/// Function/filter call: func(args) or value | filter(args)
/// </summary>
public sealed record CallExpression(
    Expression Callee,
    IReadOnlyList<Expression> Arguments,
    IReadOnlyDictionary<string, Expression> KeywordArguments,
    int Line,
    int Column) : Expression(Line, Column);

/// <summary>
/// Filter application: value | filter
/// </summary>
public sealed record FilterExpression(
    Expression Value,
    string FilterName,
    IReadOnlyList<Expression> Arguments,
    IReadOnlyDictionary<string, Expression> KeywordArguments,
    int Line,
    int Column) : Expression(Line, Column);

/// <summary>
/// Test expression: value is test
/// </summary>
public sealed record TestExpression(
    Expression Value,
    string TestName,
    IReadOnlyList<Expression> Arguments,
    bool Negated,
    int Line,
    int Column) : Expression(Line, Column);

/// <summary>
/// Binary operation: a + b, a and b, etc.
/// </summary>
public sealed record BinaryExpression(Expression Left, BinaryOperator Operator, Expression Right, int Line, int Column) : Expression(Line, Column);

/// <summary>
/// Unary operation: not x, -x
/// </summary>
public sealed record UnaryExpression(UnaryOperator Operator, Expression Operand, int Line, int Column) : Expression(Line, Column);

/// <summary>
/// Conditional expression: a if cond else b
/// </summary>
public sealed record ConditionalExpression(Expression TrueExpr, Expression Condition, Expression FalseExpr, int Line, int Column) : Expression(Line, Column);

/// <summary>
/// List literal: [1, 2, 3]
/// </summary>
public sealed record ListExpression(IReadOnlyList<Expression> Items, int Line, int Column) : Expression(Line, Column);

/// <summary>
/// Dictionary literal: {"a": 1, "b": 2}
/// </summary>
public sealed record DictExpression(IReadOnlyList<(Expression Key, Expression Value)> Items, int Line, int Column) : Expression(Line, Column);

/// <summary>
/// Tuple expression: (a, b, c)
/// </summary>
public sealed record TupleExpression(IReadOnlyList<Expression> Items, int Line, int Column) : Expression(Line, Column);

/// <summary>
/// String concatenation: "hello " ~ name
/// </summary>
public sealed record ConcatExpression(Expression Left, Expression Right, int Line, int Column) : Expression(Line, Column);

/// <summary>
/// Comparison chain: a &lt; b &lt; c
/// </summary>
public sealed record CompareExpression(
    Expression Left,
    IReadOnlyList<(CompareOperator Op, Expression Expr)> Comparisons,
    int Line,
    int Column) : Expression(Line, Column);

#endregion

#region Statements

/// <summary>
/// Raw text output.
/// </summary>
public sealed record TextStatement(string Text, int Line, int Column) : Statement(Line, Column);

/// <summary>
/// Variable output: {{ expr }}
/// </summary>
public sealed record OutputStatement(Expression Expression, int Line, int Column) : Statement(Line, Column);

/// <summary>
/// If statement with optional elif/else branches.
/// </summary>
public sealed record IfStatement(
    Expression Condition,
    IReadOnlyList<Statement> ThenBody,
    IReadOnlyList<(Expression Condition, IReadOnlyList<Statement> Body)> ElifBranches,
    IReadOnlyList<Statement>? ElseBody,
    int Line,
    int Column) : Statement(Line, Column);

/// <summary>
/// For loop statement.
/// </summary>
public sealed record ForStatement(
    IReadOnlyList<string> TargetNames,
    Expression Iterable,
    IReadOnlyList<Statement> Body,
    IReadOnlyList<Statement>? ElseBody,
    Expression? Filter,
    bool Recursive,
    int Line,
    int Column) : Statement(Line, Column);

/// <summary>
/// Block definition for template inheritance.
/// </summary>
public sealed record BlockStatement(
    string Name,
    IReadOnlyList<Statement> Body,
    bool Scoped,
    int Line,
    int Column) : Statement(Line, Column);

/// <summary>
/// Extends statement for template inheritance.
/// </summary>
public sealed record ExtendsStatement(Expression Template, int Line, int Column) : Statement(Line, Column);

/// <summary>
/// Include statement.
/// </summary>
public sealed record IncludeStatement(
    Expression Template,
    bool WithContext,
    bool IgnoreMissing,
    int Line,
    int Column) : Statement(Line, Column);

/// <summary>
/// Set statement for variable assignment.
/// </summary>
public sealed record SetStatement(
    IReadOnlyList<string> Names,
    Expression? Value,
    IReadOnlyList<Statement>? Body,
    int Line,
    int Column) : Statement(Line, Column);

/// <summary>
/// Macro definition.
/// </summary>
public sealed record MacroStatement(
    string Name,
    IReadOnlyList<(string Name, Expression? Default)> Parameters,
    IReadOnlyList<Statement> Body,
    int Line,
    int Column) : Statement(Line, Column);

/// <summary>
/// Macro call block.
/// </summary>
public sealed record CallStatement(
    Expression Call,
    IReadOnlyList<(string Name, Expression? Default)> Parameters,
    IReadOnlyList<Statement> Body,
    int Line,
    int Column) : Statement(Line, Column);

/// <summary>
/// With statement for scoped context.
/// </summary>
public sealed record WithStatement(
    IReadOnlyList<(string Name, Expression Value)> Assignments,
    IReadOnlyList<Statement> Body,
    int Line,
    int Column) : Statement(Line, Column);

/// <summary>
/// Autoescape control.
/// </summary>
public sealed record AutoescapeStatement(
    bool Enabled,
    IReadOnlyList<Statement> Body,
    int Line,
    int Column) : Statement(Line, Column);

/// <summary>
/// Continue statement in for loop.
/// </summary>
public sealed record ContinueStatement(int Line, int Column) : Statement(Line, Column);

/// <summary>
/// Break statement in for loop.
/// </summary>
public sealed record BreakStatement(int Line, int Column) : Statement(Line, Column);

/// <summary>
/// Template root containing all statements.
/// </summary>
public sealed record TemplateNode(IReadOnlyList<Statement> Body, int Line = 1, int Column = 1) : Node(Line, Column);

#endregion

#region Operators

public enum BinaryOperator
{
    Add,        // +
    Subtract,   // -
    Multiply,   // *
    Divide,     // /
    FloorDivide,// //
    Modulo,     // %
    Power,      // **
    And,        // and
    Or,         // or
    In,         // in
    NotIn,      // not in
}

public enum UnaryOperator
{
    Not,        // not
    Negative,   // -
    Positive,   // +
}

public enum CompareOperator
{
    Equal,          // ==
    NotEqual,       // !=
    LessThan,       // <
    LessThanOrEqual,// <=
    GreaterThan,    // >
    GreaterThanOrEqual, // >=
}

#endregion

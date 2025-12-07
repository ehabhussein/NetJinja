namespace NetJinja.Exceptions;

/// <summary>
/// Base exception for all Jinja-related errors.
/// </summary>
public class JinjaException : Exception
{
    public int Line { get; }
    public int Column { get; }
    public string? TemplateName { get; }

    public JinjaException(string message, int line = 0, int column = 0, string? templateName = null)
        : base(FormatMessage(message, line, column, templateName))
    {
        Line = line;
        Column = column;
        TemplateName = templateName;
    }

    public JinjaException(string message, Exception innerException, int line = 0, int column = 0, string? templateName = null)
        : base(FormatMessage(message, line, column, templateName), innerException)
    {
        Line = line;
        Column = column;
        TemplateName = templateName;
    }

    private static string FormatMessage(string message, int line, int column, string? templateName)
    {
        if (line > 0)
        {
            var location = templateName != null ? $"{templateName}:{line}:{column}" : $"line {line}, column {column}";
            return $"{message} at {location}";
        }
        return templateName != null ? $"{message} in {templateName}" : message;
    }
}

/// <summary>
/// Exception thrown during template lexing/tokenization.
/// </summary>
public class LexerException : JinjaException
{
    public LexerException(string message, int line, int column, string? templateName = null)
        : base(message, line, column, templateName) { }
}

/// <summary>
/// Exception thrown during template parsing.
/// </summary>
public class ParserException : JinjaException
{
    public ParserException(string message, int line, int column, string? templateName = null)
        : base(message, line, column, templateName) { }
}

/// <summary>
/// Exception thrown during template rendering.
/// </summary>
public class RenderException : JinjaException
{
    public RenderException(string message, int line = 0, int column = 0, string? templateName = null)
        : base(message, line, column, templateName) { }

    public RenderException(string message, Exception innerException, int line = 0, int column = 0, string? templateName = null)
        : base(message, innerException, line, column, templateName) { }
}

/// <summary>
/// Exception thrown when a variable or function is not found.
/// </summary>
public class UndefinedVariableException : RenderException
{
    public string VariableName { get; }

    public UndefinedVariableException(string variableName, int line = 0, int column = 0, string? templateName = null)
        : base($"Undefined variable: '{variableName}'", line, column, templateName)
    {
        VariableName = variableName;
    }
}

/// <summary>
/// Exception thrown when a filter is not found.
/// </summary>
public class UndefinedFilterException : RenderException
{
    public string FilterName { get; }

    public UndefinedFilterException(string filterName, int line = 0, int column = 0, string? templateName = null)
        : base($"Undefined filter: '{filterName}'", line, column, templateName)
    {
        FilterName = filterName;
    }
}

/// <summary>
/// Exception thrown when a template is not found.
/// </summary>
public class TemplateNotFoundException : JinjaException
{
    public TemplateNotFoundException(string templateName)
        : base($"Template not found: '{templateName}'", templateName: templateName) { }
}

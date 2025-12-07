# NetJinja

A high-performance, native Jinja2-compatible template engine for .NET 10. Fast, stable, and feature-complete.

[![NuGet](https://img.shields.io/nuget/v/NetJinja.svg)](https://www.nuget.org/packages/NetJinja/)
[![GitHub Release](https://img.shields.io/github/v/release/ehabhussein/NetJinja)](https://github.com/ehabhussein/NetJinja/releases)
[![Build](https://github.com/ehabhussein/NetJinja/actions/workflows/build.yml/badge.svg)](https://github.com/ehabhussein/NetJinja/actions/workflows/build.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/)

## Requirements

- .NET 10.0 or later

## Features

- **Full Jinja2 Syntax Support** - Variables, filters, control flow, inheritance, macros, and more
- **High Performance** - Optimized lexer, parser, and renderer with template caching
- **Zero Dependencies** - Pure .NET implementation with no external dependencies
- **Type Safe** - Full nullable reference type support
- **Extensible** - Custom filters, tests, and template loaders
- **LLM Ready** - Perfect for generating AI/LLM prompts with dynamic content

## Installation

```bash
dotnet add package NetJinja
```

Or via Package Manager:
```powershell
Install-Package NetJinja
```

## Quick Start

```csharp
using NetJinja;

// Simple variable substitution
var result = Jinja.Render("Hello, {{ name }}!", new { name = "World" });
// Output: Hello, World!

// Using filters
var upper = Jinja.Render("{{ message | upper }}", new { message = "hello" });
// Output: HELLO

// Control flow
var template = @"
{% for item in items %}
  - {{ item }}
{% endfor %}";
var list = Jinja.Render(template, new { items = new[] { "Apple", "Banana", "Cherry" } });
```

## Table of Contents

- [Variables](#variables)
- [Filters](#filters)
- [Control Flow](#control-flow)
- [Template Inheritance](#template-inheritance)
- [Includes](#includes)
- [Macros](#macros)
- [Tests](#tests)
- [Expressions](#expressions)
- [Comments](#comments)
- [Whitespace Control](#whitespace-control)
- [Set Statement](#set-statement)
- [With Statement](#with-statement)
- [Auto-escaping](#auto-escaping)
- [Custom Filters and Tests](#custom-filters-and-tests)
- [Environment Configuration](#environment-configuration)
- [Template Loaders](#template-loaders)
- [LLM Prompt Templates](#llm-prompt-templates)
- [API Reference](#api-reference)
- [Performance](#performance)

---

## Variables

Variables are accessed using double curly braces `{{ }}`.

### Basic Variable Access

```csharp
Jinja.Render("{{ name }}", new { name = "Alice" });
// Output: Alice
```

### Object Properties

```csharp
Jinja.Render("{{ user.name }} ({{ user.age }})", new {
    user = new { name = "Bob", age = 30 }
});
// Output: Bob (30)
```

### Dictionary Access

```csharp
Jinja.Render("{{ data.key }}", new Dictionary<string, object> {
    ["data"] = new Dictionary<string, object> { ["key"] = "value" }
});
// Output: value
```

### List/Array Indexing

```csharp
// Positive indexing
Jinja.Render("{{ items[0] }}", new { items = new[] { "first", "second" } });
// Output: first

// Negative indexing (from end)
Jinja.Render("{{ items[-1] }}", new { items = new[] { "first", "second", "last" } });
// Output: last
```

### Attribute vs Subscript Access

```csharp
// Both are equivalent for object properties
Jinja.Render("{{ user.name }}", new { user = new { name = "Alice" } });
Jinja.Render("{{ user['name'] }}", new { user = new { name = "Alice" } });
```

---

## Filters

Filters transform values using the pipe `|` operator. Multiple filters can be chained.

### String Filters

```csharp
// upper - Convert to uppercase
Jinja.Render("{{ 'hello' | upper }}");  // HELLO

// lower - Convert to lowercase
Jinja.Render("{{ 'HELLO' | lower }}");  // hello

// capitalize - Capitalize first character
Jinja.Render("{{ 'hello world' | capitalize }}");  // Hello world

// title - Title case
Jinja.Render("{{ 'hello world' | title }}");  // Hello World

// trim - Remove whitespace
Jinja.Render("{{ '  hello  ' | trim }}");  // hello

// replace - Replace text
Jinja.Render("{{ 'hello' | replace('l', 'L') }}");  // heLLo
Jinja.Render("{{ 'aaa' | replace('a', 'b', 2) }}");  // bba (limit replacements)

// truncate - Shorten text with ellipsis
Jinja.Render("{{ 'Hello World' | truncate(8) }}");  // Hello...
Jinja.Render("{{ 'Hello World' | truncate(8, true) }}");  // Hello... (killwords)
Jinja.Render("{{ 'Hello World' | truncate(8, true, '---') }}");  // Hello--- (custom end)

// wordwrap - Wrap text at width
Jinja.Render("{{ text | wordwrap(20) }}", new { text = "..." });

// center - Center text in width
Jinja.Render("{{ 'ab' | center(6) }}");  // "  ab  "

// indent - Indent lines
Jinja.Render("{{ 'a\nb' | indent(4) }}");  // "a\n    b"
Jinja.Render("{{ 'a\nb' | indent(4, first=true) }}");  // "    a\n    b"

// striptags - Remove HTML tags
Jinja.Render("{{ '<p>Hello</p>' | striptags }}");  // Hello

// escape (or e) - HTML escape
Jinja.Render("{{ '<script>' | escape }}");  // &lt;script&gt;

// safe - Mark as safe (no escaping)
// Used with AutoEscape enabled
env.AutoEscape = true;
env.FromString("{{ html | safe }}").Render(new { html = "<b>bold</b>" });

// urlencode - URL encode
Jinja.Render("{{ 'hello world' | urlencode }}");  // hello%20world

// wordcount - Count words
Jinja.Render("{{ 'hello world' | wordcount }}");  // 2
```

### List/Sequence Filters

```csharp
// length - Get length
Jinja.Render("{{ items | length }}", new { items = new[] { 1, 2, 3 } });  // 3
Jinja.Render("{{ 'hello' | length }}");  // 5

// first - Get first element
Jinja.Render("{{ items | first }}", new { items = new[] { "a", "b" } });  // a

// last - Get last element
Jinja.Render("{{ items | last }}", new { items = new[] { "a", "b" } });  // b

// join - Join elements
Jinja.Render("{{ items | join(', ') }}", new { items = new[] { "a", "b" } });  // a, b
Jinja.Render("{{ items | join(', ', attribute='name') }}",
    new { items = new[] { new { name = "A" }, new { name = "B" } } });  // A, B

// sort - Sort elements
Jinja.Render("{{ items | sort | join }}", new { items = new[] { 3, 1, 2 } });  // 123
Jinja.Render("{{ items | sort(reverse=true) | join }}", new { items = new[] { 1, 2, 3 } });  // 321
Jinja.Render("{{ items | sort(attribute='name') | map(attribute='name') | join }}",
    new { items = new[] { new { name = "B" }, new { name = "A" } } });  // AB

// reverse - Reverse sequence
Jinja.Render("{{ items | reverse | join }}", new { items = new[] { 1, 2, 3 } });  // 321
Jinja.Render("{{ 'hello' | reverse }}");  // olleh

// unique - Remove duplicates
Jinja.Render("{{ items | unique | join }}", new { items = new[] { 1, 2, 2, 3 } });  // 123

// list - Convert to list
Jinja.Render("{{ 'abc' | list | join('-') }}");  // a-b-c

// batch - Group into batches
var template = "{% for batch in items | batch(2) %}[{{ batch | join }}]{% endfor %}";
Jinja.Render(template, new { items = new[] { 1, 2, 3, 4, 5 } });  // [12][34][5]
Jinja.Render(template.Replace("batch(2)", "batch(2, 'x')"),
    new { items = new[] { 1, 2, 3 } });  // [12][3x] (with fill value)

// slice - Slice into groups
var template = "{% for group in items | slice(2) %}[{{ group | join }}]{% endfor %}";
Jinja.Render(template, new { items = new[] { 1, 2, 3, 4 } });  // [12][34]

// map - Extract attribute or apply filter
Jinja.Render("{{ items | map(attribute='name') | join }}",
    new { items = new[] { new { name = "A" }, new { name = "B" } } });  // AB

// select - Filter truthy values
Jinja.Render("{{ items | select | join }}", new { items = new object[] { 0, 1, "", 2 } });  // 12

// reject - Filter falsy values
Jinja.Render("{{ items | reject | join }}", new { items = new object[] { 0, 1, "", 2 } });  // 0

// selectattr - Filter by attribute truthiness
Jinja.Render("{{ items | selectattr('active') | map(attribute='name') | join }}",
    new { items = new[] {
        new { name = "A", active = true },
        new { name = "B", active = false }
    } });  // A

// rejectattr - Filter by attribute falsiness
Jinja.Render("{{ items | rejectattr('active') | map(attribute='name') | join }}",
    new { items = new[] {
        new { name = "A", active = true },
        new { name = "B", active = false }
    } });  // B

// groupby - Group by attribute
var template = @"{% for group in items | groupby('type') %}
{{ group.grouper }}: {{ group.list | length }}
{% endfor %}";
Jinja.Render(template, new { items = new[] {
    new { type = "fruit", name = "apple" },
    new { type = "fruit", name = "banana" },
    new { type = "veggie", name = "carrot" }
} });
// fruit: 2
// veggie: 1
```

### Numeric Filters

```csharp
// abs - Absolute value
Jinja.Render("{{ x | abs }}", new { x = -5 });  // 5

// round - Round number
Jinja.Render("{{ 2.7 | round }}");  // 3
Jinja.Render("{{ 2.345 | round(2) }}");  // 2.35
Jinja.Render("{{ 2.1 | round(method='ceil') }}");  // 3
Jinja.Render("{{ 2.9 | round(method='floor') }}");  // 2

// int - Convert to integer
Jinja.Render("{{ 42.7 | int }}");  // 42
Jinja.Render("{{ 'invalid' | int(default=0) }}");  // 0

// float - Convert to float
Jinja.Render("{{ 42 | float }}");  // 42

// sum - Sum elements
Jinja.Render("{{ items | sum }}", new { items = new[] { 1, 2, 3 } });  // 6
Jinja.Render("{{ items | sum(start=10) }}", new { items = new[] { 1, 2, 3 } });  // 16
Jinja.Render("{{ items | sum(attribute='value') }}",
    new { items = new[] { new { value = 1 }, new { value = 2 } } });  // 3

// min - Minimum value
Jinja.Render("{{ items | min }}", new { items = new[] { 3, 1, 2 } });  // 1

// max - Maximum value
Jinja.Render("{{ items | max }}", new { items = new[] { 3, 1, 2 } });  // 3

// filesizeformat - Format bytes as human-readable
Jinja.Render("{{ 1000 | filesizeformat }}");  // 1.0 KB
Jinja.Render("{{ 1000000 | filesizeformat }}");  // 1.0 MB
Jinja.Render("{{ 1000000000 | filesizeformat }}");  // 1.0 GB
```

### Dictionary Filters

```csharp
// items - Get key-value pairs
var template = "{% for k, v in data | items %}{{ k }}={{ v }};{% endfor %}";
Jinja.Render(template, new { data = new Dictionary<string, int> { ["a"] = 1, ["b"] = 2 } });
// a=1;b=2;

// keys - Get keys
Jinja.Render("{{ data | keys | join }}",
    new { data = new Dictionary<string, int> { ["a"] = 1, ["b"] = 2 } });  // ab

// values - Get values
Jinja.Render("{{ data | values | join }}",
    new { data = new Dictionary<string, int> { ["a"] = 1, ["b"] = 2 } });  // 12
```

### Other Filters

```csharp
// default (or d) - Default value for undefined/falsy
Jinja.Render("{{ undefined_var | default('N/A') }}");  // N/A
Jinja.Render("{{ name | default('Anonymous') }}", new { name = "Alice" });  // Alice
Jinja.Render("{{ '' | default('empty', true) }}");  // empty (boolean=true checks falsy)

// tojson - Convert to JSON
Jinja.Render("{{ data | tojson }}", new { data = new { a = 1, b = "text" } });
// {"a":1,"b":"text"}

// pprint - Pretty print (formatted output)
Jinja.Render("{{ data | pprint }}", new { data = new { a = 1 } });
```

### Filter Chaining

```csharp
// Multiple filters can be chained
Jinja.Render("{{ items | sort | reverse | join(', ') }}",
    new { items = new[] { 3, 1, 2 } });  // 3, 2, 1

Jinja.Render("{{ text | trim | upper | truncate(10) }}",
    new { text = "  hello world  " });  // HELLO W...
```

---

## Control Flow

### If Statement

```csharp
// Basic if
Jinja.Render("{% if active %}Active{% endif %}", new { active = true });
// Active

// If-else
Jinja.Render("{% if active %}Yes{% else %}No{% endif %}", new { active = false });
// No

// If-elif-else
var template = @"{% if score >= 90 %}A
{% elif score >= 80 %}B
{% elif score >= 70 %}C
{% else %}F{% endif %}";
Jinja.Render(template, new { score = 85 });  // B
```

### Truthiness

```csharp
// Truthy values: non-zero numbers, non-empty strings, non-empty collections
Jinja.Render("{% if 1 %}yes{% endif %}");  // yes
Jinja.Render("{% if 'text' %}yes{% endif %}");  // yes
Jinja.Render("{% if [1,2,3] %}yes{% endif %}");  // yes

// Falsy values: 0, empty string, empty collections, null, false
Jinja.Render("{% if 0 %}yes{% endif %}");  // (empty)
Jinja.Render("{% if '' %}yes{% endif %}");  // (empty)
Jinja.Render("{% if [] %}yes{% endif %}");  // (empty)
```

### For Loop

```csharp
// Basic loop
var template = "{% for item in items %}{{ item }}{% endfor %}";
Jinja.Render(template, new { items = new[] { "a", "b", "c" } });  // abc

// With separator
var template = "{% for item in items %}{{ item }}{% if not loop.last %}, {% endif %}{% endfor %}";
Jinja.Render(template, new { items = new[] { "a", "b", "c" } });  // a, b, c

// Tuple unpacking
var template = "{% for key, value in items %}{{ key }}={{ value }};{% endfor %}";
Jinja.Render(template, new {
    items = new[] { new object[] { "a", 1 }, new object[] { "b", 2 } }
});  // a=1;b=2;

// For-else (runs when iterable is empty)
var template = "{% for item in items %}{{ item }}{% else %}No items{% endfor %}";
Jinja.Render(template, new { items = Array.Empty<string>() });  // No items

// Loop with filter
var template = "{% for item in items if item > 0 %}{{ item }}{% endfor %}";
Jinja.Render(template, new { items = new[] { -1, 0, 1, 2 } });  // 12

// Iterating over strings
Jinja.Render("{% for c in text %}{{ c }}-{% endfor %}", new { text = "abc" });
// a-b-c-

// Iterating over dictionaries
var template = "{% for key in data %}{{ key }}{% endfor %}";
Jinja.Render(template, new { data = new Dictionary<string, int> { ["a"] = 1, ["b"] = 2 } });
// ab
```

### Loop Variables

Inside a for loop, the special `loop` variable provides iteration metadata:

```csharp
var template = @"{% for item in items %}
{{ loop.index }}: {{ item }} ({{ loop.index0 }})
{% endfor %}";

// loop.index     - Current iteration (1-indexed)
// loop.index0    - Current iteration (0-indexed)
// loop.revindex  - Iterations remaining (1-indexed)
// loop.revindex0 - Iterations remaining (0-indexed)
// loop.first     - True if first iteration
// loop.last      - True if last iteration
// loop.length    - Total number of items
// loop.depth     - Nesting level (starts at 1)
// loop.depth0    - Nesting level (starts at 0)
// loop.cycle()   - Cycle through values
```

Example with loop variables:

```csharp
var template = @"{% for item in items %}
{% if loop.first %}First: {% endif %}
{{ loop.index }}/{{ loop.length }}: {{ item }}
{% if loop.last %} (Last!){% endif %}
{% endfor %}";

Jinja.Render(template, new { items = new[] { "a", "b", "c" } });
// First: 1/3: a
// 2/3: b
// 3/3: c (Last!)
```

### Loop Cycle

```csharp
var template = @"{% for item in items %}
<tr class=""{{ loop.cycle('odd', 'even') }}"">{{ item }}</tr>
{% endfor %}";

Jinja.Render(template, new { items = new[] { "a", "b", "c" } });
// <tr class="odd">a</tr>
// <tr class="even">b</tr>
// <tr class="odd">c</tr>
```

### Break and Continue

```csharp
// Break - exit loop early
var template = "{% for i in items %}{% if i == 3 %}{% break %}{% endif %}{{ i }}{% endfor %}";
Jinja.Render(template, new { items = new[] { 1, 2, 3, 4, 5 } });  // 12

// Continue - skip iteration
var template = "{% for i in items %}{% if i == 2 %}{% continue %}{% endif %}{{ i }}{% endfor %}";
Jinja.Render(template, new { items = new[] { 1, 2, 3 } });  // 13
```

---

## Template Inheritance

Template inheritance allows you to build a base template with blocks that child templates can override.

### Base Template (base.html)

```html
<!DOCTYPE html>
<html>
<head>
    <title>{% block title %}Default Title{% endblock %}</title>
</head>
<body>
    <header>{% block header %}Default Header{% endblock %}</header>
    <main>{% block content %}{% endblock %}</main>
    <footer>{% block footer %}Copyright 2024{% endblock %}</footer>
</body>
</html>
```

### Child Template (page.html)

```html
{% extends "base.html" %}

{% block title %}My Page{% endblock %}

{% block content %}
<h1>Welcome!</h1>
<p>This is my page content.</p>
{% endblock %}
```

### Using Template Inheritance

```csharp
var env = Jinja.CreateEnvironment();
env.Loader = new DictLoader(new Dictionary<string, string>
{
    ["base.html"] = baseTemplate,
    ["page.html"] = childTemplate
});

var result = env.GetTemplate("page.html").Render();
```

### Super Blocks

Call the parent block's content using `{{ super() }}`:

```html
{% extends "base.html" %}

{% block header %}
{{ super() }}
<nav>Navigation here</nav>
{% endblock %}
```

---

## Includes

Include other templates within a template:

```csharp
// header.html
<header>Welcome, {{ username }}</header>

// page.html
{% include "header.html" %}
<main>Content here</main>

// Usage
var env = Jinja.CreateEnvironment();
env.Loader = new DictLoader(new Dictionary<string, string>
{
    ["header.html"] = headerTemplate,
    ["page.html"] = pageTemplate
});

var result = env.GetTemplate("page.html").Render(new { username = "Alice" });
```

### Include with Context

```html
{% include "widget.html" %}                    {# Includes with current context #}
{% include "widget.html" with context %}       {# Explicitly include context #}
{% include "widget.html" without context %}    {# Include without context #}
{% include "widget.html" ignore missing %}     {# Don't error if missing #}
```

---

## Macros

Macros are reusable template functions:

```csharp
var template = @"
{% macro input(name, value='', type='text') %}
<input type=""{{ type }}"" name=""{{ name }}"" value=""{{ value }}"">
{% endmacro %}

{{ input('username') }}
{{ input('password', type='password') }}
{{ input('email', 'test@example.com', 'email') }}
";

Jinja.Render(template);
// <input type="text" name="username" value="">
// <input type="password" name="password" value="">
// <input type="email" name="email" value="test@example.com">
```

### Macro with Default Values

```csharp
var template = @"
{% macro button(text, type='button', size='medium') %}
<button type=""{{ type }}"" class=""btn btn-{{ size }}"">{{ text }}</button>
{% endmacro %}

{{ button('Click me') }}
{{ button('Submit', 'submit', 'large') }}
";
```

### Call Block

Use `{% call %}` to pass content to a macro:

```csharp
var template = @"
{% macro dialog(title) %}
<div class=""dialog"">
    <h2>{{ title }}</h2>
    <div class=""body"">{{ caller() }}</div>
</div>
{% endmacro %}

{% call dialog('Warning') %}
<p>Are you sure you want to continue?</p>
{% endcall %}
";
```

---

## Tests

Tests check conditions using the `is` keyword:

### Type Tests

```jinja
{% if x is defined %}...{% endif %}      {# Variable is defined #}
{% if x is undefined %}...{% endif %}    {# Variable is not defined #}
{% if x is none %}...{% endif %}         {# Value is null #}
{% if x is boolean %}...{% endif %}      {# Value is boolean #}
{% if x is integer %}...{% endif %}      {# Value is integer #}
{% if x is float %}...{% endif %}        {# Value is floating point #}
{% if x is number %}...{% endif %}       {# Value is numeric #}
{% if x is string %}...{% endif %}       {# Value is string #}
{% if x is mapping %}...{% endif %}      {# Value is dictionary #}
{% if x is iterable %}...{% endif %}     {# Value is iterable #}
{% if x is sequence %}...{% endif %}     {# Value is list/array #}
{% if x is callable %}...{% endif %}     {# Value is callable #}
```

### Value Tests

```jinja
{% if x is true %}...{% endif %}         {# Value is True (strictly) #}
{% if x is false %}...{% endif %}        {# Value is False (strictly) #}
{% if x is sameas(y) %}...{% endif %}    {# Same reference as y #}
{% if x is empty %}...{% endif %}        {# Empty string/collection #}
```

### Comparison Tests

```jinja
{% if x is eq(1) %}...{% endif %}        {# x == 1 #}
{% if x is equalto(1) %}...{% endif %}   {# x == 1 (alias) #}
{% if x is ne(1) %}...{% endif %}        {# x != 1 #}
{% if x is lt(1) %}...{% endif %}        {# x < 1 #}
{% if x is lessthan(1) %}...{% endif %}  {# x < 1 (alias) #}
{% if x is le(1) %}...{% endif %}        {# x <= 1 #}
{% if x is gt(1) %}...{% endif %}        {# x > 1 #}
{% if x is greaterthan(1) %}...{% endif %}  {# x > 1 (alias) #}
{% if x is ge(1) %}...{% endif %}        {# x >= 1 #}
```

### Numeric Tests

```jinja
{% if x is odd %}...{% endif %}          {# x is odd number #}
{% if x is even %}...{% endif %}         {# x is even number #}
{% if x is divisibleby(3) %}...{% endif %}  {# x is divisible by 3 #}
```

### String Tests

```jinja
{% if s is lower %}...{% endif %}        {# s is lowercase #}
{% if s is upper %}...{% endif %}        {# s is uppercase #}
```

### Containment Tests

```jinja
{% if x is in(items) %}...{% endif %}    {# x is in items #}
```

### Negated Tests

```jinja
{% if x is not defined %}...{% endif %}
{% if x is not none %}...{% endif %}
{% if x is not odd %}...{% endif %}
{% if x is not in(items) %}...{% endif %}
```

---

## Expressions

### Literals

```jinja
{{ "string" }}              {# String (double quotes) #}
{{ 'string' }}              {# String (single quotes) #}
{{ 42 }}                    {# Integer #}
{{ 3.14 }}                  {# Float #}
{{ 1.5e10 }}                {# Scientific notation #}
{{ true }}                  {# Boolean true #}
{{ false }}                 {# Boolean false #}
{{ none }}                  {# Null #}
{{ [1, 2, 3] }}             {# List #}
{{ {'a': 1, 'b': 2} }}      {# Dictionary #}
```

### Arithmetic Operators

```jinja
{{ 1 + 2 }}                 {# Addition: 3 #}
{{ 5 - 2 }}                 {# Subtraction: 3 #}
{{ 2 * 3 }}                 {# Multiplication: 6 #}
{{ 7 / 2 }}                 {# Division: 3.5 #}
{{ 7 // 2 }}                {# Floor division: 3 #}
{{ 7 % 2 }}                 {# Modulo: 1 #}
{{ 2 ** 3 }}                {# Power: 8 #}
{{ -x }}                    {# Negation #}
{{ +x }}                    {# Positive (no-op) #}
```

### Comparison Operators

```jinja
{{ 1 == 1 }}                {# Equal: True #}
{{ 1 != 2 }}                {# Not equal: True #}
{{ 1 < 2 }}                 {# Less than: True #}
{{ 2 > 1 }}                 {# Greater than: True #}
{{ 1 <= 1 }}                {# Less than or equal: True #}
{{ 2 >= 2 }}                {# Greater than or equal: True #}
```

### Logical Operators

```jinja
{{ true and false }}        {# Logical AND: False #}
{{ true or false }}         {# Logical OR: True #}
{{ not true }}              {# Logical NOT: False #}
```

### String Operators

```jinja
{{ 'hello' ~ ' ' ~ 'world' }}   {# Concatenation: hello world #}
{{ 'ab' * 3 }}                   {# Repetition: ababab #}
```

### Membership Operators

```jinja
{{ 2 in [1, 2, 3] }}        {# True #}
{{ 4 not in [1, 2, 3] }}    {# True #}
{{ 'a' in 'abc' }}          {# True (substring) #}
```

### Conditional Expression

```jinja
{{ 'yes' if condition else 'no' }}
{{ value if value else 'default' }}
{{ items[0] if items else 'empty' }}
```

### Parentheses

```jinja
{{ (1 + 2) * 3 }}           {# 9 #}
{{ 1 + (2 * 3) }}           {# 7 #}
```

---

## Comments

Comments are not included in the output:

```html
{# This is a comment #}

{#
   Multi-line
   comment
#}

{# TODO: Fix this later #}
```

---

## Whitespace Control

Control whitespace around tags using `-`:

```html
{%- if true -%}           {# Strip whitespace on both sides #}
{{- variable -}}          {# Strip whitespace around variable #}

{% if items -%}           {# Strip after opening tag #}
{%- endif %}              {# Strip before closing tag #}
```

Example:

```csharp
var template = @"
    {%- for item in items -%}
        {{ item }}
    {%- endfor -%}
";
Jinja.Render(template, new { items = new[] { "a", "b" } });
// ab (no extra whitespace)
```

---

## Set Statement

Create and modify variables:

```jinja
{% set name = 'Alice' %}
Hello, {{ name }}!

{% set items = [1, 2, 3] %}
{{ items | join(', ') }}

{% set greeting = 'Hello, ' ~ name ~ '!' %}
{{ greeting }}
```

### Block Set

Capture content as a variable:

```jinja
{% set navigation %}
<nav>
    <a href="/">Home</a>
    <a href="/about">About</a>
</nav>
{% endset %}

{{ navigation }}
```

---

## With Statement

Create a scoped context with local variables:

```jinja
{% with %}
    {% set x = 42 %}
    {{ x }}
{% endwith %}
{# x is not defined here #}

{% with x = 42, y = 'hello' %}
    {{ x }} - {{ y }}
{% endwith %}
```

---

## Auto-escaping

Enable automatic HTML escaping for security:

```csharp
var env = Jinja.CreateEnvironment();
env.AutoEscape = true;

var result = env.FromString("{{ content }}")
    .Render(new { content = "<script>alert('xss')</script>" });
// Output: &lt;script&gt;alert('xss')&lt;/script&gt;
```

### Autoescape Block

Control auto-escaping within templates:

```html
{% autoescape true %}
    {{ unsafe_html }}  {# Will be escaped #}
{% endautoescape %}

{% autoescape false %}
    {{ trusted_html }}  {# Will NOT be escaped #}
{% endautoescape %}
```

### Safe Filter

Mark content as safe (pre-escaped):

```jinja
{{ trusted_html | safe }}
```

---

## Custom Filters and Tests

### Custom Filters

```csharp
var env = Jinja.CreateEnvironment();

// Simple filter (value only)
env.AddFilter("double", (value) => Convert.ToInt32(value) * 2);

// Full filter with arguments
env.AddFilter("repeat", (value, args, kwargs, ctx) =>
{
    var text = value?.ToString() ?? "";
    var count = args.Length > 0 ? Convert.ToInt32(args[0]) : 2;
    return string.Concat(Enumerable.Repeat(text, count));
});

var result = env.FromString("{{ 21 | double }}").Render();  // 42
var result2 = env.FromString("{{ 'ab' | repeat(3) }}").Render();  // ababab
```

### Custom Tests

```csharp
var env = Jinja.CreateEnvironment();

env.AddTest("palindrome", (value, args, ctx) =>
{
    var s = value?.ToString() ?? "";
    return s.SequenceEqual(s.Reverse());
});

var result = env.FromString("{% if word is palindrome %}Yes{% endif %}")
    .Render(new { word = "radar" });  // Yes
```

---

## Environment Configuration

The `JinjaEnvironment` class provides configuration options:

```csharp
var env = Jinja.CreateEnvironment();

// Auto-escape HTML (default: false)
env.AutoEscape = true;

// Throw on undefined variables (default: false)
env.StrictUndefined = true;

// Custom undefined value (default: null)
env.UndefinedValue = "";

// Trim blocks - remove first newline after block tags
env.TrimBlocks = true;

// Strip leading whitespace before block tags
env.LstripBlocks = true;

// Keep trailing newline when loading templates
env.KeepTrailingNewline = true;

// Custom delimiters
env.LexerOptions = new LexerOptions
{
    VariableStart = "${",
    VariableEnd = "}",
    BlockStart = "<%",
    BlockEnd = "%>",
    CommentStart = "/*",
    CommentEnd = "*/"
};

// Global variables available to all templates
env.AddGlobal("app_name", "MyApp");
env.AddGlobal("version", "1.0.0");
```

---

## Template Loaders

### File System Loader

Load templates from the file system:

```csharp
var env = Jinja.CreateEnvironment();
env.Loader = new FileSystemLoader("./templates", "./shared");

var template = env.GetTemplate("page.html");
var result = template.Render(new { title = "Hello" });
```

### Dictionary Loader

Load templates from a dictionary (useful for testing):

```csharp
var env = Jinja.CreateEnvironment();
env.Loader = new DictLoader(new Dictionary<string, string>
{
    ["base.html"] = "<!DOCTYPE html><html>{% block body %}{% endblock %}</html>",
    ["page.html"] = "{% extends 'base.html' %}{% block body %}Hello{% endblock %}"
});

var template = env.GetTemplate("page.html");
```

### Custom Loader

Implement `ITemplateLoader` for custom loading:

```csharp
public class DatabaseLoader : ITemplateLoader
{
    private readonly IDatabase _db;

    public DatabaseLoader(IDatabase db) => _db = db;

    public string? GetSource(string name)
    {
        return _db.GetTemplate(name)?.Content;
    }

    public bool Exists(string name)
    {
        return _db.TemplateExists(name);
    }
}
```

---

## LLM Prompt Templates

NetJinja is perfect for generating dynamic LLM prompts:

### System Prompt with Role

```csharp
var template = @"You are a {{ role }} assistant.
Your task is to {{ task }}.
Always respond in a {{ tone }} manner.";

var prompt = Jinja.Render(template, new
{
    role = "helpful coding",
    task = "help users write clean, efficient code",
    tone = "professional and friendly"
});
```

### Chat Message Template

```csharp
var template = @"<|system|>
{{ system_message }}
<|user|>
{{ user_message }}
<|assistant|>";

var prompt = Jinja.Render(template, new
{
    system_message = "You are a helpful assistant.",
    user_message = "How do I sort a list in Python?"
});
```

### Multi-turn Conversation

```csharp
var template = @"{% for message in messages %}
<|{{ message.role }}|>
{{ message.content }}
{% endfor %}
<|assistant|>";

var prompt = Jinja.Render(template, new
{
    messages = new[]
    {
        new { role = "system", content = "You are an AI assistant." },
        new { role = "user", content = "Hello!" },
        new { role = "assistant", content = "Hi there! How can I help?" },
        new { role = "user", content = "What's the weather like?" }
    }
});
```

### Few-shot Examples

```csharp
var template = @"Classify the sentiment as positive, negative, or neutral.

{% for example in examples %}
Text: {{ example.text }}
Sentiment: {{ example.sentiment }}

{% endfor %}
Text: {{ input_text }}
Sentiment:";

var prompt = Jinja.Render(template, new
{
    examples = new[]
    {
        new { text = "I love this!", sentiment = "positive" },
        new { text = "This is terrible.", sentiment = "negative" },
        new { text = "It's okay.", sentiment = "neutral" }
    },
    input_text = "The movie was fantastic!"
});
```

### Tool/Function Definitions

```csharp
var template = @"You have access to the following tools:

{% for tool in tools %}
### {{ tool.name }}
{{ tool.description }}
Parameters:
{% for param in tool.parameters %}
- {{ param.name }} ({{ param.type }}): {{ param.description }}
{% endfor %}

{% endfor %}
Use these tools to answer: {{ query }}";

var prompt = Jinja.Render(template, new
{
    tools = new[]
    {
        new
        {
            name = "search",
            description = "Search the web",
            parameters = new[]
            {
                new { name = "query", type = "string", description = "Search query" }
            }
        },
        new
        {
            name = "calculator",
            description = "Calculate math expressions",
            parameters = new[]
            {
                new { name = "expression", type = "string", description = "Math expression" }
            }
        }
    },
    query = "What is the population of Tokyo times 2?"
});
```

### RAG Context Template

```csharp
var template = @"Answer based ONLY on the following context:

{% for doc in documents %}
---
Source: {{ doc.source }}
{{ doc.content }}
{% endfor %}
---

Question: {{ question }}

If the answer cannot be found, say ""I don't have enough information.""";

var prompt = Jinja.Render(template, new
{
    documents = new[]
    {
        new { source = "doc1.pdf", content = "Python was created by Guido van Rossum." },
        new { source = "doc2.pdf", content = "Python 3.0 was released in 2008." }
    },
    question = "Who created Python?"
});
```

### Conditional Instructions

```csharp
var template = @"{% if use_cot %}Think step by step.

{% endif %}{% if include_sources %}Include sources for claims.

{% endif %}{{ question }}";

var prompt = Jinja.Render(template, new
{
    use_cot = true,
    include_sources = false,
    question = "What is machine learning?"
});
```

### Chain of Thought Prompting

```csharp
var template = @"Problem: {{ problem }}

Let's solve this step by step:
{% for step in steps %}
Step {{ loop.index }}: {{ step }}
{% endfor %}

Based on these steps, provide your final answer.";

var prompt = Jinja.Render(template, new
{
    problem = "Calculate 15% of 80",
    steps = new[]
    {
        "Convert 15% to decimal: 15/100 = 0.15",
        "Multiply: 0.15 * 80",
        "Calculate the result"
    }
});
```

### JSON Output Formatting

```csharp
var template = @"Extract the following fields from the text:
{% for field in fields %}
- {{ field.name }}: {{ field.description }}
{% endfor %}

Text: {{ text }}

Return ONLY valid JSON with the extracted fields.";

var prompt = Jinja.Render(template, new
{
    fields = new[]
    {
        new { name = "name", description = "Person's full name" },
        new { name = "email", description = "Email address" }
    },
    text = "Contact John Smith at john@email.com"
});
```

### Persona/Role-Play

```csharp
var template = @"You are {{ persona.name }}, {{ persona.description }}.

Traits:
{% for trait in persona.traits %}
- {{ trait }}
{% endfor %}

Speaking style: {{ persona.speaking_style }}

Now respond to: {{ user_input }}";

var prompt = Jinja.Render(template, new
{
    persona = new
    {
        name = "Professor Oak",
        description = "a renowned Pokemon researcher",
        traits = new[] { "Knowledgeable", "Patient", "Enthusiastic" },
        speaking_style = "Academic but approachable"
    },
    user_input = "Tell me about Pikachu"
});
```

---

## API Reference

### Static Helper Class

```csharp
// Quick rendering
string result = Jinja.Render(template, variables);
string result = Jinja.Render(template, model);
string result = Jinja.Render(template);

// Create environment
JinjaEnvironment env = Jinja.CreateEnvironment();
```

### JinjaEnvironment

```csharp
var env = new JinjaEnvironment();

// Configuration
env.AutoEscape = true;
env.StrictUndefined = true;
env.Loader = new FileSystemLoader("./templates");

// Create template from string
Template template = env.FromString(source);

// Get template by name (requires loader)
Template template = env.GetTemplate("page.html");

// Custom filters and tests
env.AddFilter("name", filterFunc);
env.AddTest("name", testFunc);
env.AddGlobal("name", value);

// Cache management
env.ClearCache();
```

### Template

```csharp
// From environment
var template = env.FromString("Hello {{ name }}");
var template = env.GetTemplate("page.html");

// Direct creation
var template = new Template(source, environment);

// Rendering
string result = template.Render();
string result = template.Render(new { name = "World" });
string result = template.Render(new Dictionary<string, object?> { ["name"] = "World" });
```

---

## Performance

NetJinja is designed for high performance:

- **Optimized Lexer** - Uses span-based parsing and minimal allocations
- **Template Caching** - Compiled templates are cached by name
- **Lazy Evaluation** - Expressions are evaluated on demand
- **Pooled Builders** - String builders are reused where possible

### Best Practices

```csharp
// Reuse environment instance
var env = Jinja.CreateEnvironment();
env.Loader = new FileSystemLoader("./templates");

// Templates are automatically cached
var template = env.GetTemplate("page.html");  // Compiled once
var result1 = template.Render(new { data = data1 });
var result2 = template.Render(new { data = data2 });

// Clear cache if templates change
env.ClearCache();
```

---

## Error Handling

NetJinja provides detailed error messages:

```csharp
try
{
    var result = Jinja.Render("{{ undefined_var }}");
}
catch (UndefinedVariableException ex)
{
    Console.WriteLine($"Variable not found: {ex.VariableName}");
}
catch (TemplateNotFoundException ex)
{
    Console.WriteLine($"Template not found: {ex.TemplateName}");
}
catch (LexerException ex)
{
    Console.WriteLine($"Syntax error at line {ex.Line}, column {ex.Column}: {ex.Message}");
}
catch (ParserException ex)
{
    Console.WriteLine($"Parse error at line {ex.Line}, column {ex.Column}: {ex.Message}");
}
catch (RenderException ex)
{
    Console.WriteLine($"Render error at line {ex.Line}, column {ex.Column}: {ex.Message}");
}
```

---

## Complete Filter Reference

### String Filters
| Filter | Description | Example |
|--------|-------------|---------|
| `upper` | Convert to uppercase | `{{ "hello" \| upper }}` → `HELLO` |
| `lower` | Convert to lowercase | `{{ "HELLO" \| lower }}` → `hello` |
| `capitalize` | Capitalize first char | `{{ "hello" \| capitalize }}` → `Hello` |
| `title` | Title case | `{{ "hello world" \| title }}` → `Hello World` |
| `trim` | Remove whitespace | `{{ " hi " \| trim }}` → `hi` |
| `replace` | Replace text | `{{ "hello" \| replace("l", "x") }}` → `hexxo` |
| `truncate` | Shorten with ellipsis | `{{ "hello world" \| truncate(8) }}` → `hello...` |
| `wordwrap` | Wrap at width | `{{ text \| wordwrap(20) }}` |
| `center` | Center in width | `{{ "ab" \| center(6) }}` → `  ab  ` |
| `indent` | Indent lines | `{{ text \| indent(4) }}` |
| `striptags` | Remove HTML | `{{ "<b>hi</b>" \| striptags }}` → `hi` |
| `escape` | HTML escape | `{{ "<" \| escape }}` → `&lt;` |
| `safe` | Mark as safe | `{{ html \| safe }}` |
| `urlencode` | URL encode | `{{ "a b" \| urlencode }}` → `a%20b` |
| `wordcount` | Count words | `{{ "hello world" \| wordcount }}` → `2` |

### Collection Filters
| Filter | Description | Example |
|--------|-------------|---------|
| `length` | Get length | `{{ [1,2,3] \| length }}` → `3` |
| `first` | First element | `{{ [1,2,3] \| first }}` → `1` |
| `last` | Last element | `{{ [1,2,3] \| last }}` → `3` |
| `join` | Join elements | `{{ [1,2] \| join("-") }}` → `1-2` |
| `sort` | Sort elements | `{{ [3,1,2] \| sort }}` → `[1,2,3]` |
| `reverse` | Reverse order | `{{ [1,2,3] \| reverse }}` → `[3,2,1]` |
| `unique` | Remove duplicates | `{{ [1,1,2] \| unique }}` → `[1,2]` |
| `list` | Convert to list | `{{ "abc" \| list }}` → `["a","b","c"]` |
| `batch` | Group into batches | `{{ [1,2,3,4] \| batch(2) }}` |
| `slice` | Slice into groups | `{{ items \| slice(3) }}` |
| `map` | Extract attribute | `{{ items \| map(attribute='name') }}` |
| `select` | Filter truthy | `{{ [0,1,2] \| select }}` → `[1,2]` |
| `reject` | Filter falsy | `{{ [0,1,2] \| reject }}` → `[0]` |
| `selectattr` | Filter by attr | `{{ items \| selectattr('active') }}` |
| `rejectattr` | Filter by !attr | `{{ items \| rejectattr('active') }}` |
| `groupby` | Group by attr | `{{ items \| groupby('type') }}` |

### Numeric Filters
| Filter | Description | Example |
|--------|-------------|---------|
| `abs` | Absolute value | `{{ -5 \| abs }}` → `5` |
| `round` | Round number | `{{ 2.7 \| round }}` → `3` |
| `int` | Convert to int | `{{ 3.9 \| int }}` → `3` |
| `float` | Convert to float | `{{ 3 \| float }}` → `3.0` |
| `sum` | Sum elements | `{{ [1,2,3] \| sum }}` → `6` |
| `min` | Minimum value | `{{ [3,1,2] \| min }}` → `1` |
| `max` | Maximum value | `{{ [3,1,2] \| max }}` → `3` |
| `filesizeformat` | Format bytes | `{{ 1000 \| filesizeformat }}` → `1.0 KB` |

### Other Filters
| Filter | Description | Example |
|--------|-------------|---------|
| `default` | Default value | `{{ x \| default("N/A") }}` |
| `tojson` | Convert to JSON | `{{ obj \| tojson }}` |
| `items` | Dict to pairs | `{{ dict \| items }}` |
| `keys` | Dict keys | `{{ dict \| keys }}` |
| `values` | Dict values | `{{ dict \| values }}` |

---

## Complete Test Reference

| Test | Description | Example |
|------|-------------|---------|
| `defined` | Variable exists | `{% if x is defined %}` |
| `undefined` | Variable missing | `{% if x is undefined %}` |
| `none` | Value is null | `{% if x is none %}` |
| `boolean` | Is boolean | `{% if x is boolean %}` |
| `integer` | Is integer | `{% if x is integer %}` |
| `float` | Is float | `{% if x is float %}` |
| `number` | Is numeric | `{% if x is number %}` |
| `string` | Is string | `{% if x is string %}` |
| `mapping` | Is dictionary | `{% if x is mapping %}` |
| `iterable` | Is iterable | `{% if x is iterable %}` |
| `sequence` | Is list/array | `{% if x is sequence %}` |
| `callable` | Is callable | `{% if x is callable %}` |
| `odd` | Is odd number | `{% if x is odd %}` |
| `even` | Is even number | `{% if x is even %}` |
| `divisibleby(n)` | Divisible by n | `{% if x is divisibleby(3) %}` |
| `lower` | Is lowercase | `{% if s is lower %}` |
| `upper` | Is uppercase | `{% if s is upper %}` |
| `empty` | Is empty | `{% if x is empty %}` |
| `true` | Is True | `{% if x is true %}` |
| `false` | Is False | `{% if x is false %}` |
| `eq(v)` | Equals v | `{% if x is eq(1) %}` |
| `ne(v)` | Not equals v | `{% if x is ne(1) %}` |
| `lt(v)` | Less than v | `{% if x is lt(5) %}` |
| `le(v)` | Less or equal | `{% if x is le(5) %}` |
| `gt(v)` | Greater than v | `{% if x is gt(5) %}` |
| `ge(v)` | Greater or equal | `{% if x is ge(5) %}` |
| `in(list)` | In collection | `{% if x is in([1,2,3]) %}` |
| `sameas(v)` | Same reference | `{% if x is sameas(y) %}` |

---

## License

MIT License - see [LICENSE](LICENSE) for details.

---

## Contributing

Contributions are welcome! Please feel free to submit issues and pull requests.

---

## Acknowledgments

This library is inspired by the [Jinja2](https://jinja.palletsprojects.com/) template engine for Python.

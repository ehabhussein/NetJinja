namespace NetJinja.Tests;

/// <summary>
/// Tests simulating common LLM prompt template patterns.
/// </summary>
public class LlmPromptTests
{
    [Fact]
    public void SystemPrompt_WithRole_RendersCorrectly()
    {
        var template = @"You are a {{ role }} assistant. Your task is to {{ task }}.
Always respond in a {{ tone }} manner.";

        var result = Jinja.Render(template, new
        {
            role = "helpful coding",
            task = "help users write clean, efficient code",
            tone = "professional and friendly"
        });

        Assert.Contains("helpful coding assistant", result);
        Assert.Contains("help users write clean, efficient code", result);
        Assert.Contains("professional and friendly", result);
    }

    [Fact]
    public void ChatMessage_WithUserInput_RendersCorrectly()
    {
        var template = @"<|system|>
You are a helpful assistant.
<|user|>
{{ user_message }}
<|assistant|>";

        var result = Jinja.Render(template, new
        {
            user_message = "How do I sort a list in Python?"
        });

        Assert.Contains("<|system|>", result);
        Assert.Contains("You are a helpful assistant.", result);
        Assert.Contains("How do I sort a list in Python?", result);
        Assert.Contains("<|assistant|>", result);
    }

    [Fact]
    public void MultiTurnConversation_RendersAllMessages()
    {
        var template = @"{% for message in messages %}
<|{{ message.role }}|>
{{ message.content }}
{% endfor %}";

        var result = Jinja.Render(template, new
        {
            messages = new[]
            {
                new { role = "system", content = "You are an AI assistant." },
                new { role = "user", content = "Hello!" },
                new { role = "assistant", content = "Hi there! How can I help you today?" },
                new { role = "user", content = "What's the weather like?" }
            }
        });

        Assert.Contains("<|system|>", result);
        Assert.Contains("You are an AI assistant.", result);
        Assert.Contains("<|user|>", result);
        Assert.Contains("Hello!", result);
        Assert.Contains("<|assistant|>", result);
        Assert.Contains("Hi there! How can I help you today?", result);
        Assert.Contains("What's the weather like?", result);
    }

    [Fact]
    public void FewShotExamples_RendersExamplesCorrectly()
    {
        var template = @"Classify the sentiment of the following text as positive, negative, or neutral.

{% for example in examples %}
Text: {{ example.text }}
Sentiment: {{ example.sentiment }}

{% endfor %}
Text: {{ input_text }}
Sentiment:";

        var result = Jinja.Render(template, new
        {
            examples = new[]
            {
                new { text = "I love this product!", sentiment = "positive" },
                new { text = "This is terrible.", sentiment = "negative" },
                new { text = "It's okay, nothing special.", sentiment = "neutral" }
            },
            input_text = "The movie was absolutely fantastic!"
        });

        Assert.Contains("I love this product!", result);
        Assert.Contains("positive", result);
        Assert.Contains("This is terrible.", result);
        Assert.Contains("negative", result);
        Assert.Contains("The movie was absolutely fantastic!", result);
    }

    [Fact]
    public void ConditionalInstructions_BasedOnContext()
    {
        var template = @"{% if use_cot %}Think step by step before answering.

{% endif %}{% if include_sources %}Include sources for your claims.

{% endif %}Question: {{ question }}";

        var result1 = Jinja.Render(template, new
        {
            use_cot = true,
            include_sources = false,
            question = "What is machine learning?"
        });

        Assert.Contains("Think step by step", result1);
        Assert.DoesNotContain("Include sources", result1);

        var result2 = Jinja.Render(template, new
        {
            use_cot = false,
            include_sources = true,
            question = "What is machine learning?"
        });

        Assert.DoesNotContain("Think step by step", result2);
        Assert.Contains("Include sources", result2);
    }

    [Fact]
    public void ChainOfThought_PromptTemplate()
    {
        var template = @"You are a problem-solving assistant.

Problem: {{ problem }}

Let's solve this step by step:
{% for step in steps %}
Step {{ loop.index }}: {{ step }}
{% endfor %}

Based on these steps, provide your final answer.";

        var result = Jinja.Render(template, new
        {
            problem = "Calculate 15% of 80",
            steps = new[]
            {
                "Convert 15% to decimal: 15/100 = 0.15",
                "Multiply: 0.15 × 80",
                "Calculate the result"
            }
        });

        Assert.Contains("Calculate 15% of 80", result);
        Assert.Contains("Step 1:", result);
        Assert.Contains("Step 2:", result);
        Assert.Contains("Step 3:", result);
        Assert.Contains("Convert 15% to decimal", result);
    }

    [Fact]
    public void ToolUse_PromptWithFunctionDefinitions()
    {
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

        var result = Jinja.Render(template, new
        {
            tools = new[]
            {
                new
                {
                    name = "search",
                    description = "Search the web for information",
                    parameters = new[]
                    {
                        new { name = "query", type = "string", description = "The search query" }
                    }
                },
                new
                {
                    name = "calculator",
                    description = "Perform mathematical calculations",
                    parameters = new[]
                    {
                        new { name = "expression", type = "string", description = "The math expression to evaluate" }
                    }
                }
            },
            query = "What is the population of Tokyo multiplied by 2?"
        });

        Assert.Contains("### search", result);
        Assert.Contains("### calculator", result);
        Assert.Contains("Search the web for information", result);
        Assert.Contains("Perform mathematical calculations", result);
        Assert.Contains("What is the population of Tokyo multiplied by 2?", result);
    }

    [Fact]
    public void JsonOutput_InstructionPrompt()
    {
        var template = @"Extract the following information from the text and return as JSON:

Fields to extract:
{% for field in fields %}
- {{ field.name }}: {{ field.description }}
{% endfor %}

Text: {{ text }}

Return ONLY valid JSON with the extracted fields.";

        var result = Jinja.Render(template, new
        {
            fields = new[]
            {
                new { name = "name", description = "The person's full name" },
                new { name = "email", description = "The person's email address" },
                new { name = "phone", description = "The person's phone number" }
            },
            text = "Contact John Smith at john@email.com or call 555-1234."
        });

        Assert.Contains("name: The person's full name", result);
        Assert.Contains("email: The person's email address", result);
        Assert.Contains("Contact John Smith", result);
    }

    [Fact]
    public void RolePlay_PersonaPrompt()
    {
        var template = @"You are {{ persona.name }}, {{ persona.description }}.

Traits:
{% for trait in persona.traits %}
- {{ trait }}
{% endfor %}

Speaking style: {{ persona.speaking_style }}

Now respond to: {{ user_input }}";

        var result = Jinja.Render(template, new
        {
            persona = new
            {
                name = "Professor Oak",
                description = "a renowned Pokemon researcher",
                traits = new[] { "Knowledgeable", "Patient", "Enthusiastic about Pokemon" },
                speaking_style = "Academic but approachable, often uses Pokemon analogies"
            },
            user_input = "Tell me about Pikachu"
        });

        Assert.Contains("Professor Oak", result);
        Assert.Contains("Pokemon researcher", result);
        Assert.Contains("Knowledgeable", result);
        Assert.Contains("Tell me about Pikachu", result);
    }

    [Fact]
    public void ContextualRetrieval_RAGPrompt()
    {
        var template = @"Answer the question based ONLY on the following context:

{% for doc in documents %}
---
Source: {{ doc.source }}
Content: {{ doc.content }}
{% endfor %}
---

Question: {{ question }}

If the answer cannot be found in the context, say ""I don't have enough information to answer this question.""";

        var result = Jinja.Render(template, new
        {
            documents = new[]
            {
                new { source = "doc1.pdf", content = "Python was created by Guido van Rossum." },
                new { source = "doc2.pdf", content = "Python 3.0 was released in 2008." }
            },
            question = "Who created Python?"
        });

        Assert.Contains("Source: doc1.pdf", result);
        Assert.Contains("Guido van Rossum", result);
        Assert.Contains("Who created Python?", result);
        Assert.Contains("I don't have enough information", result);
    }

    [Fact]
    public void OutputFormat_WithStructuredResponse()
    {
        var template = @"Analyze the following code and provide feedback:

```{{ language }}
{{ code }}
```

Provide your analysis in the following format:
- Summary: [brief description]
- Issues: [list any problems]
- Suggestions: [improvements]
- Rating: [1-10]";

        var result = Jinja.Render(template, new
        {
            language = "python",
            code = "def add(a, b):\n    return a + b"
        });

        Assert.Contains("```python", result);
        Assert.Contains("def add(a, b):", result);
        Assert.Contains("Summary:", result);
        Assert.Contains("Rating:", result);
    }

    [Fact]
    public void ConditionalLanguage_MultilingualPrompt()
    {
        var template = @"{% if language == 'en' %}
Please respond in English.
{% elif language == 'es' %}
Por favor responde en español.
{% elif language == 'fr' %}
Veuillez répondre en français.
{% else %}
Please respond in {{ language }}.
{% endif %}

{{ message }}";

        var resultEn = Jinja.Render(template, new { language = "en", message = "Hello!" });
        Assert.Contains("Please respond in English", resultEn);

        var resultEs = Jinja.Render(template, new { language = "es", message = "¡Hola!" });
        Assert.Contains("Por favor responde en español", resultEs);
    }

    [Fact]
    public void SafetyGuardrails_PromptWithConstraints()
    {
        var template = @"You are a helpful assistant with the following constraints:

{% if safety_level == 'high' %}
STRICT RULES:
- Never provide harmful information
- Refuse requests for illegal activities
- Always recommend professional help for sensitive topics
{% endif %}

{% if allowed_topics %}
You may ONLY discuss these topics:
{% for topic in allowed_topics %}
- {{ topic }}
{% endfor %}
{% endif %}

User request: {{ request }}";

        var result = Jinja.Render(template, new
        {
            safety_level = "high",
            allowed_topics = new[] { "Programming", "Mathematics", "Science" },
            request = "Help me with Python"
        });

        Assert.Contains("STRICT RULES", result);
        Assert.Contains("Never provide harmful information", result);
        Assert.Contains("Programming", result);
        Assert.Contains("Help me with Python", result);
    }

    [Fact]
    public void TokenLimit_AwarePrompt()
    {
        var template = @"{% if context | length > max_context_length %}
[Context truncated due to length]
{{ context | truncate(max_context_length) }}
{% else %}
{{ context }}
{% endif %}

Question: {{ question }}";

        var longContext = new string('x', 1000);
        var result = Jinja.Render(template, new
        {
            context = longContext,
            max_context_length = 100,
            question = "What is this about?"
        });

        Assert.Contains("[Context truncated due to length]", result);
        Assert.Contains("...", result); // truncate adds ellipsis
    }

    [Fact]
    public void DynamicExamples_BasedOnCategory()
    {
        // Use a simple loop with if condition to filter examples
        var template = @"Here are some relevant examples for {{ task_category }}:

{% for ex in examples %}{% if ex.category == task_category %}
Input: {{ ex.input }}
Output: {{ ex.output }}
{% endif %}{% endfor %}
Now process: {{ user_input }}";

        var result = Jinja.Render(template, new
        {
            task_category = "translation",
            examples = new[]
            {
                new { category = "translation", input = "Hello", output = "Hola" },
                new { category = "translation", input = "Goodbye", output = "Adiós" },
                new { category = "summarization", input = "Long text...", output = "Short summary" }
            },
            user_input = "Thank you"
        });

        Assert.Contains("Hello", result);
        Assert.Contains("Hola", result);
        Assert.Contains("Thank you", result);
        // Should not contain summarization examples
        Assert.DoesNotContain("Long text", result);
    }
}

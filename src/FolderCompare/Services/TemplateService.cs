using HandlebarsDotNet;
using System;
using System.IO;
using System.Text.Json;

namespace FolderCompare.Services;

public class TemplateService
{
    private static IHandlebars? _handlebars;
    private static Dictionary<string, HandlebarsTemplate<object, object>>? _compiledTemplates;

    public static void Initialize()
    {
        _handlebars = Handlebars.Create();
        _compiledTemplates = new Dictionary<string, HandlebarsTemplate<object, object>>();

        // Register custom helpers
        RegisterHelpers();
    }

    private static void RegisterHelpers()
    {
        if (_handlebars == null) return;

        // Helper to format numbers with thousand separators
        _handlebars.RegisterHelper("formatNumber", (writer, context, parameters) =>
        {
            if (parameters.Length > 0 && parameters[0] is int number)
            {
                writer.WriteSafeString(number.ToString("N0"));
            }
        });

        // Helper to escape HTML
        _handlebars.RegisterHelper("escapeHtml", (writer, context, parameters) =>
        {
            if (parameters.Length > 0 && parameters[0] is string text)
            {
                var escaped = System.Security.SecurityElement.Escape(text);
                writer.WriteSafeString(escaped ?? string.Empty);
            }
        });

        // Helper for equality check
        _handlebars.RegisterHelper("eq", (writer, context, parameters) =>
        {
            if (parameters.Length == 2)
            {
                var result = Equals(parameters[0], parameters[1]);
                writer.WriteSafeString(result.ToString().ToLower());
            }
        });

        // Helper for AND logic
        _handlebars.RegisterHelper("and", (writer, context, parameters) =>
        {
            if (parameters.Length == 2)
            {
                var arg1 = parameters[0] as bool? ?? false;
                var arg2 = parameters[1] as bool? ?? false;
                writer.WriteSafeString((arg1 && arg2).ToString().ToLower());
            }
        });

        // Helper to convert to JSON
        _handlebars.RegisterHelper("toJson", (writer, context, parameters) =>
        {
            if (parameters.Length > 0)
            {
                var json = JsonSerializer.Serialize(parameters[0]);
                writer.WriteSafeString(json);
            }
        });
    }

    public static string RenderTemplate(string templateName, object data)
    {
        if (_handlebars == null)
        {
            Initialize();
        }

        // Use cached compiled template if available
        if (!_compiledTemplates!.ContainsKey(templateName))
        {
            var templatePath = Path.Combine("Templates", $"{templateName}.hbs");
            
            if (!File.Exists(templatePath))
            {
                throw new FileNotFoundException($"Template not found: {templatePath}");
            }

            var templateContent = File.ReadAllText(templatePath, System.Text.Encoding.UTF8);
            _compiledTemplates[templateName] = _handlebars!.Compile(templateContent);
        }
        
        return _compiledTemplates[templateName](data);
    }
}

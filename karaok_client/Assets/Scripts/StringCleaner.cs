using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;

public static class StringCleaner
{
    // List of allowed symbols (braces)
    private static readonly List<char> allowedSymbols = new List<char> { '{', '}', '[', ']', '(', ')'};

    /// <summary>
    /// Detects if the input string contains any type of braces.
    /// </summary>
    /// <param name="input">The input string to check.</param>
    /// <returns>True if the string contains any kind of braces, otherwise false.</returns>
    public static bool ContainsBraces(string input)
    {
        if (input == null)
            return false;

        // Check if the input string contains any brace character
        foreach (char c in input)
        {
            if (allowedSymbols.Contains(c))
            {
                return true;
            }
        }

        return false;
    }
    /// <summary>
    /// Removes any content inside square brackets [], curly braces {}, or parentheses () 
    /// and merges multiple spaces into a single space.
    /// </summary>
    /// <param name="input">The input string to clean.</param>
    /// <returns>The cleaned string.</returns>
    public static string RemoveContentInBracesAndMergeSpaces(string input)
    {
        if (input == null)
            return null;

        // Regular expression to match content within [], {}, or ()
        string bracePattern = @"(\[.*?\])|(\{.*?\})|(\(.*?\))";

        // Remove content inside braces
        string cleanedString = Regex.Replace(input, bracePattern, string.Empty);

        // Regular expression to merge multiple spaces into one
        string spacePattern = @"\s+";

        // Replace multiple spaces with a single space
        cleanedString = Regex.Replace(cleanedString, spacePattern, " ");

        // Optionally trim any leading/trailing spaces
        return cleanedString.Trim();
    }

    /// <summary>
    /// Removes all special symbols from a string except alphanumeric characters 
    /// and symbols stored in allowedSymbols list.
    /// </summary>
    /// <param name="input">The input string to clean.</param>
    /// <returns>The cleaned string.</returns>
    public static string RemoveSpecialSymbols(string input)
    {
        if (input == null)
            return null;

        StringBuilder cleanedString = new StringBuilder();

        foreach (char c in input)
        {
            if (char.IsLetterOrDigit(c) || IsAllowedSymbol(c) || char.IsWhiteSpace(c))
            {
                cleanedString.Append(c);
            }
        }

        return cleanedString.ToString();
    }
    /// <summary>
     /// Converts an HTML string by replacing layout tags with C# equivalent.
     /// </summary>
     /// <param name="html">The input HTML string.</param>
     /// <returns>The plain text string with HTML tags replaced.</returns>
    public static string ConvertHtmlToPlainText(string html)
    {
        if (string.IsNullOrEmpty(html))
        {
            return string.Empty;
        }

        // Replace common HTML tags with their C# equivalents
        string text = html;

        // Handle <br> and <br/> tags - Replace with new line
        text = Regex.Replace(text, @"<br\s*/?>", "\n", RegexOptions.IgnoreCase);

        // Handle <p> and </p> - Replace with new line before and after paragraphs
        text = Regex.Replace(text, @"<p\s*/?>", "\n", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"</p>", "\n", RegexOptions.IgnoreCase);

        // Handle <b>, <strong> and their closing tags - Make bold text in uppercase (or handle as preferred)
        text = Regex.Replace(text, @"<b>|<strong>", "", RegexOptions.IgnoreCase); // Optionally wrap with '**' for Markdown
        text = Regex.Replace(text, @"</b>|</strong>", "", RegexOptions.IgnoreCase);

        // Handle <i>, <em> and their closing tags - Make italic text (optionally wrap with underscores for Markdown)
        text = Regex.Replace(text, @"<i>|<em>", "", RegexOptions.IgnoreCase); // Optionally wrap with '_'
        text = Regex.Replace(text, @"</i>|</em>", "", RegexOptions.IgnoreCase);

        // Handle <h1> to <h6> - Replace with new lines before and after headers
        for (int i = 1; i <= 6; i++)
        {
            text = Regex.Replace(text, $@"<h{i}>", "\n", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, $@"</h{i}>", "\n", RegexOptions.IgnoreCase);
        }

        // Remove any remaining HTML tags (non-layout tags)
        text = Regex.Replace(text, @"<.*?>", string.Empty);

        // Optionally trim leading/trailing newlines and whitespace
        text = text.Trim();

        return text;
    }

    /// <summary>
    /// Helper method to check if a character is an allowed symbol (brace).
    /// </summary>
    /// <param name="c">The character to check.</param>
    /// <returns>True if the character is an allowed symbol, otherwise false.</returns>
    private static bool IsAllowedSymbol(char c)
    {
        return allowedSymbols.Contains(c);
    }
}
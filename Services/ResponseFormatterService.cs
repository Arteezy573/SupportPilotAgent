using System.Text;
using System.Text.RegularExpressions;

namespace SupportPilotAgent.Services
{
    public class ResponseFormatterService
    {
        public string FormatResponse(string rawResponse)
        {
            if (string.IsNullOrWhiteSpace(rawResponse))
                return rawResponse;

            var formatted = new StringBuilder();
            var lines = rawResponse.Split('\n', StringSplitOptions.None);

            bool inCodeBlock = false;
            bool inList = false;
            bool previousWasEmpty = false;

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i].TrimEnd();

                // Handle code blocks
                if (line.StartsWith("```"))
                {
                    inCodeBlock = !inCodeBlock;
                    formatted.AppendLine(line);
                    previousWasEmpty = false;
                    continue;
                }

                if (inCodeBlock)
                {
                    formatted.AppendLine(line);
                    previousWasEmpty = false;
                    continue;
                }

                // Handle empty lines - reduce excessive spacing
                if (string.IsNullOrWhiteSpace(line))
                {
                    if (!previousWasEmpty && formatted.Length > 0)
                    {
                        formatted.AppendLine();
                        previousWasEmpty = true;
                    }
                    inList = false;
                    continue;
                }

                // Format headers
                if (IsHeader(line))
                {
                    if (!previousWasEmpty && formatted.Length > 0)
                        formatted.AppendLine();

                    formatted.AppendLine(FormatHeader(line));
                    previousWasEmpty = false;
                    continue;
                }

                // Format lists
                if (IsBulletPoint(line) || IsNumberedList(line))
                {
                    if (!inList && !previousWasEmpty && formatted.Length > 0)
                        formatted.AppendLine();

                    if (IsBulletPoint(line))
                        formatted.AppendLine(FormatBulletPoint(line));
                    else
                        formatted.AppendLine(FormatNumberedList(line));

                    inList = true;
                    previousWasEmpty = false;
                    continue;
                }

                // Format sections
                if (IsSectionTitle(line))
                {
                    if (!previousWasEmpty && formatted.Length > 0)
                        formatted.AppendLine();

                    formatted.AppendLine($"**{line.Trim()}**");
                    previousWasEmpty = false;
                    continue;
                }

                // Format key-value pairs and regular text
                if (inList && !IsBulletPoint(line) && !IsNumberedList(line))
                {
                    inList = false;
                    if (!previousWasEmpty)
                        formatted.AppendLine();
                }

                // Process the line content
                line = FormatImportantTerms(line);

                if (IsKeyValuePair(line))
                    line = FormatKeyValuePair(line);

                // Combine short related lines to make more compact
                if (IsShortInfoLine(line) && formatted.Length > 0 && !previousWasEmpty)
                {
                    // Check if previous line was also short info - combine them
                    var lastLine = GetLastNonEmptyLine(formatted.ToString());
                    if (IsShortInfoLine(lastLine) && !lastLine.Contains("**"))
                    {
                        formatted.AppendLine(line);
                    }
                    else
                    {
                        formatted.AppendLine(line);
                    }
                }
                else
                {
                    formatted.AppendLine(line);
                }

                previousWasEmpty = false;
            }

            return CleanupFormatting(formatted.ToString());
        }

        private bool IsShortInfoLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line)) return false;

            // Lines that look like metadata or short facts
            return line.Length < 80 &&
                   (line.Contains(":") ||
                    line.StartsWith("**") ||
                    Regex.IsMatch(line, @"^[A-Z][^.]*:") ||
                    Regex.IsMatch(line, @"^\*\*[^*]+\*\*:"));
        }

        private string GetLastNonEmptyLine(string text)
        {
            var lines = text.Split('\n');
            for (int i = lines.Length - 1; i >= 0; i--)
            {
                var line = lines[i].Trim();
                if (!string.IsNullOrWhiteSpace(line))
                    return line;
            }
            return "";
        }

        private string CleanupFormatting(string text)
        {
            // Remove excessive line breaks
            text = Regex.Replace(text, @"\n{3,}", "\n\n");

            // Ensure proper spacing around headers
            text = Regex.Replace(text, @"\n(#{1,3}[^\n]+)\n", "\n\n$1\n");

            // Ensure proper spacing around section titles
            text = Regex.Replace(text, @"\n(\*\*[^*]+\*\*)\n", "\n\n$1\n");

            return text.Trim();
        }

        private bool IsHeader(string line)
        {
            var trimmed = line.Trim();
            return trimmed.StartsWith("===") && trimmed.EndsWith("===") ||
                   trimmed.StartsWith("---") && trimmed.EndsWith("---") ||
                   trimmed.StartsWith("# ") ||
                   trimmed.StartsWith("## ") ||
                   trimmed.StartsWith("### ");
        }

        private string FormatHeader(string line)
        {
            var trimmed = line.Trim();

            if (trimmed.StartsWith("===") && trimmed.EndsWith("==="))
            {
                var title = trimmed.Trim('=').Trim();
                return $"# {title}";
            }

            if (trimmed.StartsWith("---") && trimmed.EndsWith("---"))
            {
                var title = trimmed.Trim('-').Trim();
                return $"## {title}";
            }

            return trimmed;
        }

        private bool IsBulletPoint(string line)
        {
            var trimmed = line.TrimStart();
            return trimmed.StartsWith("- ") ||
                   trimmed.StartsWith("• ") ||
                   trimmed.StartsWith("* ") ||
                   Regex.IsMatch(trimmed, @"^[•·‣⁃]\s");
        }

        private string FormatBulletPoint(string line)
        {
            var trimmed = line.TrimStart();
            var indent = line.Length - trimmed.Length;
            var indentStr = new string(' ', Math.Max(0, indent));

            if (trimmed.StartsWith("- ") || trimmed.StartsWith("• ") || trimmed.StartsWith("* "))
                return $"{indentStr}- {trimmed.Substring(2).Trim()}";

            return $"{indentStr}- {Regex.Replace(trimmed, @"^[•·‣⁃]\s*", "")}";
        }

        private bool IsNumberedList(string line)
        {
            var trimmed = line.TrimStart();
            return Regex.IsMatch(trimmed, @"^\d+\.\s");
        }

        private string FormatNumberedList(string line)
        {
            var trimmed = line.TrimStart();
            var indent = line.Length - trimmed.Length;
            var indentStr = new string(' ', Math.Max(0, indent));

            var match = Regex.Match(trimmed, @"^(\d+)\.\s*(.*)");
            if (match.Success)
            {
                return $"{indentStr}{match.Groups[1].Value}. {match.Groups[2].Value}";
            }

            return line;
        }

        private bool IsKeyValuePair(string line)
        {
            return line.Contains(":") &&
                   !line.TrimStart().StartsWith("http") &&
                   line.IndexOf(':') < line.Length - 1 &&
                   line.IndexOf(':') > 0 &&
                   line.IndexOf(':') < 50; // Reasonable position for a key
        }

        private string FormatKeyValuePair(string line)
        {
            var colonIndex = line.IndexOf(':');
            if (colonIndex > 0 && colonIndex < line.Length - 1)
            {
                var key = line.Substring(0, colonIndex).Trim();
                var value = line.Substring(colonIndex + 1).Trim();

                if (!string.IsNullOrWhiteSpace(key) && !string.IsNullOrWhiteSpace(value))
                {
                    return $"**{key}**: {value}";
                }
            }

            return line;
        }

        private bool IsSectionTitle(string line)
        {
            var trimmed = line.Trim();
            var commonSections = new[] {
                "Key Information Extracted", "Email Summary", "Diagnostic Recommendations",
                "Suggested Queries", "Next Steps", "Summary", "Analysis", "Recommendation",
                "Ticket Categorization", "Suggested Diagnostic Steps", "Business Impact",
                "Root Cause Analysis", "Resolution Steps", "Follow-up Actions"
            };

            return commonSections.Any(section =>
                trimmed.Equals(section, StringComparison.OrdinalIgnoreCase) ||
                trimmed.EndsWith(":", StringComparison.OrdinalIgnoreCase) &&
                trimmed.Length > 3 && trimmed.Length < 50);
        }

        private string FormatImportantTerms(string line)
        {
            // Highlight important terms
            var importantTerms = new Dictionary<string, string>
            {
                { "High Priority", "**High Priority**" },
                { "Critical", "**Critical**" },
                { "Urgent", "**Urgent**" },
                { "Error", "**Error**" },
                { "Failed", "**Failed**" },
                { "Issue", "**Issue**" },
                { "Problem", "**Problem**" },
                { "Recommended", "*Recommended*" },
                { "Suggestion", "*Suggestion*" },
                { "Next Steps", "**Next Steps**" },
                { "Action Required", "**Action Required**" }
            };

            foreach (var term in importantTerms)
            {
                line = Regex.Replace(line, $@"\b{Regex.Escape(term.Key)}\b",
                    term.Value, RegexOptions.IgnoreCase);
            }

            // Format technical terms in backticks
            line = Regex.Replace(line, @"\b(API|URL|HTTP|JSON|XML|SQL|SSO|SAML|OAuth|JWT|SSL|TLS)\b",
                "`$1`", RegexOptions.IgnoreCase);

            return line;
        }
    }
}
namespace QuickTypeGenerator;

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

public partial class Program
{
    private static readonly string MinimalFilePath = "../../LDtk/LDtkJson.cs";
    private static readonly string FullFilePath = "../../LDtk.Codegen/LDtkJsonFull.cs";
    private static readonly string Version = "1.3.3";

    public static void Main()
    {
        GenerateFile();

        List<string> lines = File.ReadAllLines(MinimalFilePath).ToList();
        ProcessFile(lines);
        lines[0] = "// This file was auto generated, any changes will be lost. For LDtk " + Version + " \n" + lines[0];
        lines[0] += "#pragma warning disable CS1591, IDE1006, CA1707, CA1716";
        lines[1] += "using Microsoft.Xna.Framework;";
        File.WriteAllLines(MinimalFilePath, lines);

        Format();

        // Delete multiple blank lines in a row
        lines = File.ReadAllLines(MinimalFilePath).ToList();

        int blanks = 0;
        for (int i = lines.Count - 1; i >= 0; i--)
        {
            if (string.IsNullOrEmpty(lines[i]))
            {
                blanks++;
                if (blanks > 1)
                {
                    lines.RemoveAt(i);
                }
            }
            else
            {
                blanks = 0;
            }
        }

        File.WriteAllLines(MinimalFilePath, lines);

        File.AppendAllText(MinimalFilePath, "#pragma warning restore CS1591, IDE1006, CA1707, CA1716");
    }

    private static void Format()
    {
        Thread.Sleep(300);
        Process.Start(new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = "format ../../LDtk/",
            UseShellExecute = false,
            RedirectStandardOutput = false,
            RedirectStandardError = false,
            RedirectStandardInput = false,
            CreateNoWindow = true
        }).WaitForExit();
    }

    private static void ProcessFile(List<string> lines)
    {
        bool end = false;

        for (int i = 0; i < lines.Count; i++)
        {
            if (end)
            {
                lines[i] = "";
                continue;
            }

            if (
                lines[i].TrimStart().EndsWith("public partial class ForcedRefs") ||
                lines[i].TrimStart().EndsWith("public partial class AutoLayerRuleDefinition") ||
                lines[i].TrimStart().EndsWith("public partial class FieldDefinition")
            )
            {
                DeleteDocComment(lines, i);
                RemoveClassBody(lines, i);
                continue;
            }

            if (lines[i].TrimStart().EndsWith("public partial class AutoLayerRuleGroup"))
            {
                RemoveClassBody(lines, i);
                continue;
            }

            if (lines[i].TrimStart().StartsWith("///"))
            {
                if (lines[i].EndsWith("///"))
                {
                    lines[i] += " <br/>";
                    continue;
                }
                // Doc comment cleanup
                lines[i] = lines[i].Replace("<br/><br/>", "<br/>");
                lines[i] = lines[i].Replace("&lt;", " &lt; ").Replace("&gt;", " &gt; ");

                lines[i] = lines[i].Replace("`", "");
                lines[i] = lines[i].Replace("*", "");
                lines[i] = lines[i].Replace("IID", "Guid");
                lines[i] = MyRegex().Replace(lines[i], "$1");

                lines[i] = lines[i].Replace("Array<...> (eg. Array<Int>, Array<Point>", "<![CDATA[ Array<...> (eg. Array<Int>, Array<Point> ]]>");
                continue;
            }

            if (lines[i].TrimStart().EndsWith("public partial class World"))
            {
                lines[i] = "public partial class LDtkWorld";
                continue;
            }

            if (lines[i].TrimStart().EndsWith("public partial class Level"))
            {
                lines[i] = "public partial class LDtkLevel";
                continue;
            }

            if (lines[i].TrimStart().StartsWith("public string _Type "))
            {
                if (lines[i - 3].Contains("IntGrid, Entities, Tiles or AutoLayer"))
                {
                    lines[i] = "public LayerType _Type { get; set; }";
                    continue;
                }
            }

            if (lines[i].Contains("JsonPropertyName"))
            {
                ProcessVariables(lines, i);
            }

#pragma warning disable SYSLIB1045
            lines[i] = Regex.Replace(lines[i], "double", "float");
            lines[i] = Regex.Replace(lines[i], "long", "int");

            lines[i] = Regex.Replace(lines[i], "string Color", "Color Color");
            lines[i] = Regex.Replace(lines[i], "string BgColor", "Color BgColor");
            lines[i] = Regex.Replace(lines[i], "string _SmartColor", "Color _SmartColor");

            lines[i] = Regex.Replace(lines[i], @"int\[\] Px", "Point Px");
            lines[i] = Regex.Replace(lines[i], @"int\[\] Src", "Point Src");
            lines[i] = Regex.Replace(lines[i], @"int\[\] _Grid", "Point _Grid");
            lines[i] = Regex.Replace(lines[i], @"int\[\] TopLeftPx", "Point TopLeftPx");

            lines[i] = Regex.Replace(lines[i], @"float\[\] _Pivot", "Vector2 _Pivot");
            lines[i] = Regex.Replace(lines[i], @"float\[\] Scale", "Vector2 Scale");

            lines[i] = Regex.Replace(lines[i], @"ReferenceToAnEntityInstance", "EntityRef");

            lines[i] = Regex.Replace(lines[i], "TypeEnum Type", "LayerType Type");

            lines[i] = Regex.Replace(lines[i], "(public string )(.*)(Iid )", "public Guid $2Iid ");
#pragma warning restore SYSLIB1045

            if (lines[i].StartsWith("    internal static class Converter"))
            {
                lines[i] = "";
                end = true;
            }
        }
    }

    private static void RemoveClassBody(List<string> lines, int i)
    {
        int indent = 1;

        lines[i] = "";
        lines[i + 1] = "";

        int currentLine = i + 2;

        // Remove class body
        while (indent > 0)
        {
            indent += lines[currentLine].Count(c => c == '{');
            indent -= lines[currentLine].Count(c => c == '}');
            lines[currentLine] = "";
            currentLine++;
        }
    }

    private static void ProcessVariables(List<string> lines, int i)
    {
        string name = lines[i].Split('"')[1];

        if (name == "__FORCED_REFS" || name == "levels" || name == "levelFields")
        {
            lines[i] = "";
            lines[i + 1] = "";
            DeleteDocComment(lines, i);
        }
        else if (name == "worlds")
        {
            string[] lineParts = lines[i + 1].TrimStart().Split(' ');
            lineParts[1] = "LDtk" + lineParts[1];
            lines[i + 1] = string.Join(" ", lineParts);
        }
        else if (name.StartsWith("__"))
        {
            string[] lineParts = lines[i + 1].TrimStart().Split(' ');
            lineParts[2] = "_" + lineParts[2];
            lines[i + 1] = string.Join(" ", lineParts);
        }
    }

    private static void DeleteDocComment(List<string> lines, int i)
    {
        for (int j = i - 1; j >= 0; j--)
        {
            if (lines[j].Contains("/// <summary>"))
            {
                lines[j] = "";
                break;
            }
            lines[j] = "";
        }
    }

    private static void GenerateFile()
    {
        string[] args = new string[]
        {
            "--lang cs",
            "--src https://raw.githubusercontent.com/deepnight/ldtk/master/docs/MINIMAL_JSON_SCHEMA.json",
            "-s schema",
            "-o " + MinimalFilePath,
            "-t LDtkFile",
            "--features attributes-only",
            "--namespace LDtk",
            "--framework SystemTextJson",
            "--alphabetize-properties"
        };

        Process.Start(new ProcessStartInfo
        {
            FileName = "quicktype.cmd",
            Arguments = string.Join(" ", args),
            UseShellExecute = false,
            RedirectStandardOutput = false,
            RedirectStandardError = false,
            RedirectStandardInput = false,
            CreateNoWindow = true
        }).WaitForExit();
    }

    [GeneratedRegex("\\[(.*)\\]\\(.*\\)")]
    private static partial Regex MyRegex();
}

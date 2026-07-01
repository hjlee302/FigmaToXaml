using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml;
using FigmaToXaml.Models;

namespace FigmaToXaml.Services;

public sealed partial class FigmaXamlConverter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly Dictionary<string, int> _usedNames = [];

    public string ConvertJson(string json)
    {
        _usedNames.Clear();

        var document = JsonSerializer.Deserialize<FigmaExportDocument>(json, JsonOptions)
            ?? throw new InvalidOperationException("JSON 문서를 읽을 수 없습니다.");

        if (document.Nodes.Count == 0)
        {
            throw new InvalidOperationException("변환할 Figma 노드가 없습니다.");
        }

        var builder = new StringBuilder();
        builder.AppendLine("<UserControl");
        builder.AppendLine("    xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"");
        builder.AppendLine("    xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\">");

        if (document.Nodes.Count == 1)
        {
            AppendNode(builder, document.Nodes[0], 1, includePosition: false);
        }
        else
        {
            builder.AppendLine("    <Canvas>");
            foreach (var node in document.Nodes)
            {
                AppendNode(builder, node, 2, includePosition: true);
            }

            builder.AppendLine("    </Canvas>");
        }

        builder.AppendLine("</UserControl>");
        return builder.ToString();
    }

    private void AppendNode(StringBuilder builder, FigmaNode node, int indent, bool includePosition)
    {
        switch (node.Type)
        {
            case "FRAME":
            case "COMPONENT":
            case "GROUP":
            case "INSTANCE":
                AppendContainer(builder, node, indent, includePosition);
                break;
            case "RECTANGLE":
                AppendRectangle(builder, node, indent, includePosition);
                break;
            case "TEXT":
                AppendText(builder, node, indent, includePosition);
                break;
            case "ELLIPSE":
                AppendShape(builder, node, indent, includePosition, "Ellipse");
                break;
            case "VECTOR":
            case "LINE":
            case "POLYGON":
            case "STAR":
                AppendVectorPlaceholder(builder, node, indent, includePosition);
                break;
            default:
                AppendUnsupported(builder, node, indent, includePosition);
                break;
        }
    }

    private void AppendContainer(StringBuilder builder, FigmaNode node, int indent, bool includePosition)
    {
        var fill = FirstVisibleSolidPaint(node.Appearance?.Fills);
        var radius = ToCornerRadius(node.Appearance);
        var useBorder = fill is not null || radius is not null || HasVisibleShadow(node);

        if (useBorder)
        {
            AppendStartTag(builder, "Border", node, indent, includePosition, extraAttributes =>
            {
                if (fill is not null)
                {
                    extraAttributes.Add(("Background", fill.Color?.Hex));
                }

                if (radius is not null)
                {
                    extraAttributes.Add(("CornerRadius", radius));
                }
            }, hasChildren: true);

            AppendEffects(builder, node, indent + 1);
            Indent(builder, indent + 1).AppendLine("<Canvas>");
            AppendChildren(builder, node, indent + 2);
            Indent(builder, indent + 1).AppendLine("</Canvas>");
            Indent(builder, indent).AppendLine("</Border>");
            return;
        }

        AppendStartTag(builder, "Canvas", node, indent, includePosition, null, hasChildren: true);
        AppendChildren(builder, node, indent + 1);
        Indent(builder, indent).AppendLine("</Canvas>");
    }

    private void AppendRectangle(StringBuilder builder, FigmaNode node, int indent, bool includePosition)
    {
        var fill = FirstVisibleSolidPaint(node.Appearance?.Fills);
        var stroke = FirstVisibleSolidPaint(node.Appearance?.Strokes);

        AppendStartTag(builder, "Border", node, indent, includePosition, extraAttributes =>
        {
            extraAttributes.Add(("Background", fill?.Color?.Hex ?? "Transparent"));

            if (stroke?.Color?.Hex is not null && node.Appearance?.StrokeWeight > 0)
            {
                extraAttributes.Add(("BorderBrush", stroke.Color.Hex));
                extraAttributes.Add(("BorderThickness", Format(node.Appearance.StrokeWeight)));
            }

            var radius = ToCornerRadius(node.Appearance);
            if (radius is not null)
            {
                extraAttributes.Add(("CornerRadius", radius));
            }
        }, hasChildren: HasVisibleShadow(node));

        if (HasVisibleShadow(node))
        {
            AppendEffects(builder, node, indent + 1);
            Indent(builder, indent).AppendLine("</Border>");
        }
    }

    private void AppendText(StringBuilder builder, FigmaNode node, int indent, bool includePosition)
    {
        var fill = FirstVisibleSolidPaint(node.Appearance?.Fills);
        var text = node.Text;
        var lineHeight = ToLineHeight(text);
        var baselineOffset = ToTextBaselineOffset(text);

        AppendStartTag(builder, "Grid", node, indent, includePosition, null, hasChildren: true);

        var textAttributes = new List<(string Name, string? Value)>();
        textAttributes.Add(("Text", text?.Characters));
        textAttributes.Add(("Foreground", fill?.Color?.Hex));
        textAttributes.Add(("FontFamily", ToFontFamily(text?.FontName?.Family)));
        textAttributes.Add(("FontSize", text?.FontSize > 0 ? Format(text.FontSize) : null));
        textAttributes.Add(("FontWeight", ToFontWeight(text?.FontName?.Style)));
        textAttributes.Add(("TextAlignment", ToTextAlignment(text?.TextAlignHorizontal)));
        textAttributes.Add(("VerticalAlignment", ToVerticalAlignment(text?.TextAlignVertical)));
        textAttributes.Add(("HorizontalAlignment", "Stretch"));
        textAttributes.Add(("LineHeight", lineHeight));
        textAttributes.Add(("LineStackingStrategy", lineHeight is null ? null : "BlockLineHeight"));
        textAttributes.Add(("TextWrapping", "Wrap"));
        textAttributes.Add(("Margin", baselineOffset == 0 ? null : $"0,{Format(baselineOffset)},0,0"));
        AppendSimpleTag(builder, "TextBlock", indent + 1, textAttributes, hasChildren: false);

        Indent(builder, indent).AppendLine("</Grid>");
    }

    private void AppendShape(StringBuilder builder, FigmaNode node, int indent, bool includePosition, string shapeName)
    {
        var fill = FirstVisibleSolidPaint(node.Appearance?.Fills);
        var stroke = FirstVisibleSolidPaint(node.Appearance?.Strokes);

        AppendStartTag(builder, shapeName, node, indent, includePosition, extraAttributes =>
        {
            extraAttributes.Add(("Fill", fill?.Color?.Hex ?? "Transparent"));

            if (stroke?.Color?.Hex is not null && node.Appearance?.StrokeWeight > 0)
            {
                extraAttributes.Add(("Stroke", stroke.Color.Hex));
                extraAttributes.Add(("StrokeThickness", Format(node.Appearance.StrokeWeight)));
            }
        }, hasChildren: false);
    }

    private static void AppendSimpleTag(
        StringBuilder builder,
        string elementName,
        int indent,
        IEnumerable<(string Name, string? Value)> attributes,
        bool hasChildren)
    {
        Indent(builder, indent).Append('<').Append(elementName);

        foreach (var (name, value) in attributes)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            builder.AppendLine();
            Indent(builder, indent + 1)
                .Append(name)
                .Append("=\"")
                .Append(XmlEscape(value))
                .Append('"');
        }

        builder.AppendLine(hasChildren ? ">" : " />");
    }

    private void AppendVectorPlaceholder(StringBuilder builder, FigmaNode node, int indent, bool includePosition)
    {
        var fill = FirstVisibleSolidPaint(node.Appearance?.Fills);

        Indent(builder, indent).AppendLine($"<!-- VECTOR '{EscapeComment(node.Name)}' needs Path.Data from JsonExport for exact conversion. -->");
        AppendStartTag(builder, "Rectangle", node, indent, includePosition, extraAttributes =>
        {
            extraAttributes.Add(("Fill", fill?.Color?.Hex ?? "#33000000"));
            extraAttributes.Add(("Opacity", "0.35"));
        }, hasChildren: false);
    }

    private void AppendUnsupported(StringBuilder builder, FigmaNode node, int indent, bool includePosition)
    {
        Indent(builder, indent).AppendLine($"<!-- Unsupported Figma node type: {EscapeComment(node.Type)} / {EscapeComment(node.Name)} -->");
        AppendContainer(builder, node, indent, includePosition);
    }

    private void AppendChildren(StringBuilder builder, FigmaNode node, int indent)
    {
        foreach (var child in node.Children)
        {
            AppendNode(builder, child, indent, includePosition: true);
        }
    }

    private void AppendStartTag(
        StringBuilder builder,
        string elementName,
        FigmaNode node,
        int indent,
        bool includePosition,
        Action<List<(string Name, string? Value)>>? configureExtraAttributes,
        bool hasChildren)
    {
        var attributes = new List<(string Name, string? Value)>
        {
            ("x:Name", ToXamlName(node.Name, node.Id)),
        };

        if (includePosition)
        {
            attributes.Add(("Canvas.Left", Format(node.X)));
            attributes.Add(("Canvas.Top", Format(node.Y)));
        }

        attributes.Add(("Width", node.Width > 0 ? Format(node.Width) : null));
        attributes.Add(("Height", node.Height > 0 ? Format(node.Height) : null));
        attributes.Add(("Opacity", node.Opacity < 1 ? Format(node.Opacity) : null));
        attributes.Add(("Visibility", node.Visible ? null : "Collapsed"));

        configureExtraAttributes?.Invoke(attributes);

        Indent(builder, indent).Append('<').Append(elementName);

        foreach (var (name, value) in attributes)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            builder.AppendLine();
            Indent(builder, indent + 1)
                .Append(name)
                .Append("=\"")
                .Append(XmlEscape(value))
                .Append('"');
        }

        builder.AppendLine(hasChildren ? ">" : " />");
    }

    private void AppendEffects(StringBuilder builder, FigmaNode node, int indent)
    {
        var shadow = node.Appearance?.Effects.FirstOrDefault(effect =>
            effect.Visible && string.Equals(effect.Type, "DROP_SHADOW", StringComparison.OrdinalIgnoreCase));

        if (shadow is null)
        {
            return;
        }

        var depth = Math.Sqrt(Math.Pow(shadow.Offset?.X ?? 0, 2) + Math.Pow(shadow.Offset?.Y ?? 0, 2));
        var direction = ToShadowDirection(shadow.Offset?.X ?? 0, shadow.Offset?.Y ?? 0);

        Indent(builder, indent).AppendLine("<Border.Effect>");
        Indent(builder, indent + 1).AppendLine("<DropShadowEffect");
        Indent(builder, indent + 2).Append("Color=\"").Append(XmlEscape(ToRgbHex(shadow.Color?.Hex) ?? "#000000")).AppendLine("\"");
        Indent(builder, indent + 2).Append("Opacity=\"").Append(Format(ToAlpha(shadow.Color?.Hex))).AppendLine("\"");
        Indent(builder, indent + 2).Append("BlurRadius=\"").Append(Format(shadow.Radius)).AppendLine("\"");
        Indent(builder, indent + 2).Append("ShadowDepth=\"").Append(Format(depth)).AppendLine("\"");
        Indent(builder, indent + 2).Append("Direction=\"").Append(Format(direction)).AppendLine("\" />");
        Indent(builder, indent).AppendLine("</Border.Effect>");
    }

    private static FigmaPaint? FirstVisibleSolidPaint(IEnumerable<FigmaPaint>? paints)
    {
        return paints?.FirstOrDefault(paint =>
            paint.Visible &&
            string.Equals(paint.Type, "SOLID", StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(paint.Color?.Hex));
    }

    private static bool HasVisibleShadow(FigmaNode node)
    {
        return node.Appearance?.Effects.Any(effect =>
            effect.Visible && string.Equals(effect.Type, "DROP_SHADOW", StringComparison.OrdinalIgnoreCase)) == true;
    }

    private static string? ToCornerRadius(FigmaAppearance? appearance)
    {
        if (appearance is null)
        {
            return null;
        }

        var topLeft = appearance.TopLeftRadius ?? appearance.CornerRadius ?? 0;
        var topRight = appearance.TopRightRadius ?? appearance.CornerRadius ?? 0;
        var bottomRight = appearance.BottomRightRadius ?? appearance.CornerRadius ?? 0;
        var bottomLeft = appearance.BottomLeftRadius ?? appearance.CornerRadius ?? 0;

        if (topLeft == 0 && topRight == 0 && bottomRight == 0 && bottomLeft == 0)
        {
            return null;
        }

        if (topLeft == topRight && topRight == bottomRight && bottomRight == bottomLeft)
        {
            return Format(topLeft);
        }

        return $"{Format(topLeft)},{Format(topRight)},{Format(bottomRight)},{Format(bottomLeft)}";
    }

    private static string? ToFontWeight(string? style)
    {
        if (string.IsNullOrWhiteSpace(style))
        {
            return null;
        }

        if (style.Contains("Bold", StringComparison.OrdinalIgnoreCase))
        {
            return "Bold";
        }

        if (style.Contains("Medium", StringComparison.OrdinalIgnoreCase))
        {
            return "Medium";
        }

        if (style.Contains("Light", StringComparison.OrdinalIgnoreCase))
        {
            return "Light";
        }

        return null;
    }

    private static string? ToFontFamily(string? family)
    {
        if (string.IsNullOrWhiteSpace(family))
        {
            return null;
        }

        if (string.Equals(family, "Noto Sans CJK KR", StringComparison.OrdinalIgnoreCase))
        {
            return "./Fonts/#Noto Sans CJK KR";
        }

        return family;
    }

    private static string? ToTextAlignment(string? alignment)
    {
        return alignment switch
        {
            "CENTER" => "Center",
            "RIGHT" => "Right",
            "JUSTIFIED" => "Justify",
            _ => "Left",
        };
    }

    private static string? ToVerticalAlignment(string? alignment)
    {
        return alignment switch
        {
            "CENTER" => "Center",
            "BOTTOM" => "Bottom",
            _ => "Top",
        };
    }

    private static string? ToLineHeight(FigmaText? text)
    {
        if (text?.LineHeight?.Value is null || text.FontSize <= 0)
        {
            return null;
        }

        return text.LineHeight.Unit switch
        {
            "PIXELS" => Format(text.LineHeight.Value.Value),
            "PERCENT" => Format(text.FontSize * text.LineHeight.Value.Value / 100),
            _ => null,
        };
    }

    private static double ToTextBaselineOffset(FigmaText? text)
    {
        if (text is null || text.FontSize <= 0)
        {
            return 0;
        }

        return Math.Round(text.FontSize * -0.06, 3);
    }

    private static double ToShadowDirection(double x, double y)
    {
        if (x == 0 && y == 0)
        {
            return 315;
        }

        var radians = Math.Atan2(-y, x);
        var degrees = radians * 180 / Math.PI;
        return (degrees + 360) % 360;
    }

    private static string? ToRgbHex(string? argbHex)
    {
        if (string.IsNullOrWhiteSpace(argbHex) || argbHex.Length != 9 || argbHex[0] != '#')
        {
            return argbHex;
        }

        return $"#{argbHex[3..]}";
    }

    private static double ToAlpha(string? argbHex)
    {
        if (string.IsNullOrWhiteSpace(argbHex) || argbHex.Length != 9 || argbHex[0] != '#')
        {
            return 1;
        }

        return int.Parse(argbHex[1..3], NumberStyles.HexNumber, CultureInfo.InvariantCulture) / 255d;
    }

    private string ToXamlName(string? name, string? fallback)
    {
        var candidate = InvalidNameCharacters().Replace(name ?? string.Empty, "_").Trim('_');

        if (string.IsNullOrWhiteSpace(candidate))
        {
            candidate = $"Node_{fallback?.Replace(':', '_') ?? Guid.NewGuid().ToString("N")}";
        }

        if (!char.IsLetter(candidate[0]) && candidate[0] != '_')
        {
            candidate = $"Node_{candidate}";
        }

        if (!_usedNames.TryGetValue(candidate, out var count))
        {
            _usedNames[candidate] = 1;
            return candidate;
        }

        count++;
        _usedNames[candidate] = count;
        return $"{candidate}_{count}";
    }

    private static string Format(double value)
    {
        return Math.Round(value, 3).ToString("0.###", CultureInfo.InvariantCulture);
    }

    private static string XmlEscape(string value)
    {
        return SecurityElementEscape(value);
    }

    private static string SecurityElementEscape(string value)
    {
        using var writer = new StringWriter(CultureInfo.InvariantCulture);
        using var xmlWriter = XmlWriter.Create(writer, new XmlWriterSettings { ConformanceLevel = ConformanceLevel.Fragment });
        xmlWriter.WriteString(value);
        xmlWriter.Flush();
        return writer.ToString();
    }

    private static string EscapeComment(string? value)
    {
        return (value ?? string.Empty).Replace("--", "-", StringComparison.Ordinal);
    }

    private static StringBuilder Indent(StringBuilder builder, int indent)
    {
        return builder.Append(' ', indent * 4);
    }

    [GeneratedRegex(@"[^\p{L}\p{Nd}_]+")]
    private static partial Regex InvalidNameCharacters();
}

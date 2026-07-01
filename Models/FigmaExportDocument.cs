using System.Text.Json.Serialization;

namespace FigmaToXaml.Models;

public sealed class FigmaExportDocument
{
    [JsonPropertyName("schemaVersion")]
    public int SchemaVersion { get; set; }

    [JsonPropertyName("generator")]
    public string? Generator { get; set; }

    [JsonPropertyName("source")]
    public string? Source { get; set; }

    [JsonPropertyName("page")]
    public FigmaPage? Page { get; set; }

    [JsonPropertyName("nodes")]
    public List<FigmaNode> Nodes { get; set; } = [];
}

public sealed class FigmaPage
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }
}

public sealed class FigmaNode
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("visible")]
    public bool Visible { get; set; } = true;

    [JsonPropertyName("x")]
    public double X { get; set; }

    [JsonPropertyName("y")]
    public double Y { get; set; }

    [JsonPropertyName("width")]
    public double Width { get; set; }

    [JsonPropertyName("height")]
    public double Height { get; set; }

    [JsonPropertyName("rotation")]
    public double Rotation { get; set; }

    [JsonPropertyName("opacity")]
    public double Opacity { get; set; } = 1;

    [JsonPropertyName("appearance")]
    public FigmaAppearance? Appearance { get; set; }

    [JsonPropertyName("text")]
    public FigmaText? Text { get; set; }

    [JsonPropertyName("children")]
    public List<FigmaNode> Children { get; set; } = [];
}

public sealed class FigmaAppearance
{
    [JsonPropertyName("fills")]
    public List<FigmaPaint> Fills { get; set; } = [];

    [JsonPropertyName("strokes")]
    public List<FigmaPaint> Strokes { get; set; } = [];

    [JsonPropertyName("strokeWeight")]
    public double StrokeWeight { get; set; }

    [JsonPropertyName("cornerRadius")]
    public double? CornerRadius { get; set; }

    [JsonPropertyName("topLeftRadius")]
    public double? TopLeftRadius { get; set; }

    [JsonPropertyName("topRightRadius")]
    public double? TopRightRadius { get; set; }

    [JsonPropertyName("bottomRightRadius")]
    public double? BottomRightRadius { get; set; }

    [JsonPropertyName("bottomLeftRadius")]
    public double? BottomLeftRadius { get; set; }

    [JsonPropertyName("effects")]
    public List<FigmaEffect> Effects { get; set; } = [];
}

public sealed class FigmaPaint
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("visible")]
    public bool Visible { get; set; } = true;

    [JsonPropertyName("opacity")]
    public double Opacity { get; set; } = 1;

    [JsonPropertyName("color")]
    public FigmaColor? Color { get; set; }
}

public sealed class FigmaColor
{
    [JsonPropertyName("hex")]
    public string? Hex { get; set; }
}

public sealed class FigmaEffect
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("visible")]
    public bool Visible { get; set; } = true;

    [JsonPropertyName("radius")]
    public double Radius { get; set; }

    [JsonPropertyName("color")]
    public FigmaColor? Color { get; set; }

    [JsonPropertyName("offset")]
    public FigmaOffset? Offset { get; set; }
}

public sealed class FigmaOffset
{
    [JsonPropertyName("x")]
    public double X { get; set; }

    [JsonPropertyName("y")]
    public double Y { get; set; }
}

public sealed class FigmaText
{
    [JsonPropertyName("characters")]
    public string? Characters { get; set; }

    [JsonPropertyName("textAlignHorizontal")]
    public string? TextAlignHorizontal { get; set; }

    [JsonPropertyName("textAlignVertical")]
    public string? TextAlignVertical { get; set; }

    [JsonPropertyName("fontName")]
    public FigmaFontName? FontName { get; set; }

    [JsonPropertyName("fontSize")]
    public double FontSize { get; set; }

    [JsonPropertyName("lineHeight")]
    public FigmaLineHeight? LineHeight { get; set; }
}

public sealed class FigmaFontName
{
    [JsonPropertyName("family")]
    public string? Family { get; set; }

    [JsonPropertyName("style")]
    public string? Style { get; set; }
}

public sealed class FigmaLineHeight
{
    [JsonPropertyName("unit")]
    public string? Unit { get; set; }

    [JsonPropertyName("value")]
    public double? Value { get; set; }
}

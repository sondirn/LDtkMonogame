namespace LDtk.Codegen.Generators;

using System.Globalization;
using System.Linq;
using System.Text.Json;

using Full;

public class ClassGenerator(LDtkFileFull ldtkFile, Options options) : BaseGenerator(ldtkFile, options)
{
    public void Generate()
    {
        // Level Classes
        GenClass(Options.LevelClassName, string.Empty, fieldDefinitions: LDtkFile.Defs.LevelFields);

        // Entity Classes
        foreach (EntityDefinition e in LDtkFile.Defs.Entities)
        {
            GenClass(e.Identifier, "Entities", e);
        }
    }

    void GenClass(string identifier, string folder, EntityDefinition entityDefinition = null,
        FieldDefinition[] fieldDefinitions = null)
    {
        GenHeaders(options);

        string classDef = $"public partial class {identifier}";

        if (entityDefinition != null)
        {
            classDef += ": ILDtkEntity";
        }

        Line(classDef);

        StartBlock();
        {
            if (entityDefinition != null)
            {
                GenEntityFields(identifier, entityDefinition);
            }
            else if (fieldDefinitions != null)
            {
                GenFieldDefs(fieldDefinitions);
            }
        }
        EndBlock();

        Line("#pragma warning restore");
        Output(folder, identifier);
    }

    void GenEntityFields(string identifier, EntityDefinition entityDefinition)
    {
        //generate the default data for fields.
        Line($"public static {identifier} Default()");
        StartBlock();
        {
            Line($"var entity = new {identifier}();");
            Line($"entity.Identifier = \"{identifier}\";");
            Line($"entity.Uid = {entityDefinition.Uid};");
            Line(
                $"entity.Size = new Vector2({entityDefinition.Width.ToString()}f, {entityDefinition.Height.ToString()}f);");
            Line($"entity.Pivot = new Vector2({entityDefinition.PivotX}f, {entityDefinition.PivotY}f);");
            if (entityDefinition.TileRect != null)
            {
                GenTilesetRectangle("Tile", entityDefinition.TileRect);
            }

            byte r = entityDefinition.Color.R;
            byte g = entityDefinition.Color.G;
            byte b = entityDefinition.Color.B;
            byte a = entityDefinition.Color.A;

            Line($"entity.SmartColor = new Color({r}, {g}, {b}, {a});");
            Blank();

            //generate default data for custom fields
            GenCustomFieldDefData(entityDefinition.FieldDefs);

            Blank();
            Line("return entity;");
        }
        EndBlock();
        Blank();

        //generate the rest of the class
        Line("public string Identifier { get; set; }");
        Line("public System.Guid Iid { get; set; }");
        Line("public int Uid { get; set; }");
        Line("public Vector2 Position { get; set; }");
        Line("public Vector2 Size { get; set; }");
        Line("public Vector2 Pivot { get; set; }");
        Line("public Rectangle Tile { get; set; }");
        Blank();
        Line("public Color SmartColor { get; set; }");
        Blank();
        FieldDefinition[] fields = entityDefinition.FieldDefs;
        GenFieldDefs(fields);
    }

    void GenFieldDefs(FieldDefinition[] fields)
    {
        foreach (FieldDefinition value in fields)
        {
            string type = Converter.ConvertFieldDefinitionTypes(value._Type, Options.PointAsVector2);

            if (value.CanBeNull)
            {
                type += "?";
            }

            Line($"public {type} {value.Identifier} {{ get; set; }}");
        }
    }

    void GenCustomFieldDefData(FieldDefinition[] fieldDefs)
    {
        foreach (FieldDefinition field in fieldDefs)
        {
            if (field.DefaultOverride == null)
            {
                continue;
            }

            if (field.DefaultOverride.Params.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            JsonElement defaultValue = field.DefaultOverride.Params.EnumerateArray().First();

            if (field._Type.Contains("Enum"))
            {
                string enumName = field._Type.Replace("LocalEnum.", "");
                string value = defaultValue.GetString();
                string enumValue = $"{enumName}.{value}";

                Line($"entity.{field.Identifier} = {enumValue};");
            }

            switch (field._Type)
            {
                case Field.IntType:
                    Line($"entity.{field.Identifier} = {defaultValue.GetInt32().ToString()};");
                    break;
                case Field.FloatType:
                    Line(
                        $"entity.{field.Identifier} = {defaultValue.GetSingle().ToString(CultureInfo.InvariantCulture)}f;");
                    break;
                case Field.BoolType:
                    Line($"entity.{field.Identifier} = {defaultValue.GetBoolean().ToString().ToLower()};");
                    break;
                case Field.StringType:
                    Line($"entity.{field.Identifier} = {defaultValue.GetRawText()};");
                    break;
                case Field.FilePathType:
                    Line($"entity.{field.Identifier} = {defaultValue.GetString()};");
                    break;
                case Field.TileType:
                    string[] rectValues = defaultValue.GetString()!.Split(',');
                    int x = int.Parse(rectValues[0]);
                    int y = int.Parse(rectValues[1]);
                    int width = int.Parse(rectValues[2]);
                    int height = int.Parse(rectValues[3]);
                    int tilesetUid = (int)field.TilesetUid!;
                    TilesetRectangle finalRect = new()
                    {
                        X = x,
                        Y = y,
                        W = width,
                        H = height,
                        TilesetUid = tilesetUid
                    };
                    GenTilesetRectangle(field.Identifier, finalRect);
                    break;
                case Field.ColorType:
                    int colorValue = defaultValue.GetInt32();
                    int red = (colorValue >> 16) & 0xFF;
                    int green = (colorValue >> 8) & 0xFF;
                    int blue = colorValue & 0xFF;
                    break;
            }
        }
    }

    void GenHeaders(Options options)
    {
        Line($"namespace {Options.Namespace};");
        Blank();
        Line("// This file was automatically generated, any modifications will be lost!");
        Line("#pragma warning disable");
        Blank();
        Line("using LDtk;");
        Line("using Microsoft.Xna.Framework;");
        Blank();
    }

    void GenTilesetRectangle(string identifier, TilesetRectangle rect)
    {
        Line($"entity.{identifier} = new TilesetRectangle()");
        StartBlock();
        {
            Line($"X = {rect.X.ToString()},");
            Line($"Y = {rect.Y.ToString()},");
            Line($"W = {rect.W.ToString()},");
            Line($"H = {rect.H.ToString()}");
        }
        EndCodeBlock();
    }
}

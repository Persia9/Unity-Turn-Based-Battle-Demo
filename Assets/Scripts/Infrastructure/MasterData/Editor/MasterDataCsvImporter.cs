using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Infrastructure.MasterData.Editor
{
    public static class MasterDataCsvImporter
    {
        public static readonly string[] TableNames = { "Characters", "Skills", "Stages", "UI", "Localization" };

        public static MasterDataSourceData Load(string directory)
        {
            var tables = TableNames.ToDictionary(name => name,
                name => File.ReadAllText(Path.Combine(directory, name + ".csv"), Encoding.UTF8));
            return Parse(tables);
        }

        public static MasterDataSourceData Parse(IReadOnlyDictionary<string, string> tables)
            => new MasterDataSourceData
            {
                Characters = ReadRows<CharacterSourceRow>(tables, "Characters"),
                Skills = ReadRows<SkillSourceRow>(tables, "Skills"),
                Stages = ReadRows<StageSourceRow>(tables, "Stages"),
                UI = ReadRows<UISourceRow>(tables, "UI"),
                Localization = ReadRows<LocalizationSourceRow>(tables, "Localization")
            };

        private static List<T> ReadRows<T>(IReadOnlyDictionary<string, string> tables, string name) where T : new()
        {
            if (!tables.TryGetValue(name, out var text)) throw new FormatException($"Missing table: {name}.csv");
            var records = MasterDataCsvCodec.Read(text);
            if (records.Count == 0) throw new FormatException($"{name}.csv: header row is missing.");
            var fields = typeof(T).GetFields(BindingFlags.Instance | BindingFlags.Public);
            var headers = records[0].Select(x => x.Trim()).ToArray();
            if (headers.Length != fields.Length || headers.Distinct(StringComparer.Ordinal).Count() != headers.Length ||
                fields.Any(field => !headers.Contains(field.Name, StringComparer.Ordinal)))
                throw new FormatException(
                    $"{name}.csv: expected columns {string.Join(", ", fields.Select(x => x.Name))}.");
            var ordered = headers.Select(header => fields.Single(x => x.Name == header)).ToArray();
            var output = new List<T>();
            for (var record = 1; record < records.Count; record++)
            {
                var values = records[record];
                if (values.All(string.IsNullOrWhiteSpace)) continue;
                if (values.Length != ordered.Length)
                    throw new FormatException($"{name}.csv record {record + 1}: wrong column count.");
                var item = new T();
                for (var col = 0; col < values.Length; col++)
                {
                    try
                    {
                        ordered[col].SetValue(item, Convert(values[col], ordered[col].FieldType));
                    }
                    catch (Exception error)
                    {
                        throw new FormatException(
                            $"{name}.csv record {record + 1}, column {headers[col]}: {error.Message}", error);
                    }
                }

                output.Add(item);
            }

            return output;
        }

        private static object Convert(string value, Type type)
        {
            if (type == typeof(string)) return value;
            value = value.Trim();
            if (type == typeof(int) && decimal.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture,
                                        out var number)
                                    && number == decimal.Truncate(number) && number >= int.MinValue &&
                                    number <= int.MaxValue) return (int)number;
            if (type == typeof(bool))
            {
                if (value == "1") return true;
                if (value == "0") return false;
                if (bool.TryParse(value, out var boolean)) return boolean;
            }

            if (type.IsEnum && Enum.TryParse(type, value, false, out var result) && Enum.IsDefined(type, result))
                return result;
            throw new FormatException($"'{value}' is not a valid {type.Name}.");
        }
    }
}
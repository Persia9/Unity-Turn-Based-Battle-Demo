using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Infrastructure.MasterData.Editor
{
    // Reads the saved OpenXML values, without Excel/COM or a third-party runtime dependency.
    public static class MasterDataExcelReader
    {
        private static readonly XNamespace Main = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";

        private static readonly XNamespace Relations =
            "http://schemas.openxmlformats.org/officeDocument/2006/relationships";

        public static Dictionary<string, string> ReadAll(string directory)
            => MasterDataCsvImporter.TableNames.ToDictionary(name => name,
                name => MasterDataCsvCodec.Write(Read(Path.Combine(directory, name + ".xlsx"))));

        public static List<string[]> Read(string path)
        {
            using var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var zip = new ZipArchive(file, ZipArchiveMode.Read);
            var workbook = Load(zip, "xl/workbook.xml");
            var sheet = workbook.Descendants(Main + "sheet")
                .SingleOrDefault(x => (string)x.Attribute("name") == "Data");
            if (sheet == null) throw new FormatException($"{path}: worksheet 'Data' is missing.");
            var id = (string)sheet.Attribute(Relations + "id");
            var relationship = Load(zip, "xl/_rels/workbook.xml.rels").Root.Elements()
                .SingleOrDefault(x => (string)x.Attribute("Id") == id);
            if (relationship == null || (string)relationship.Attribute("TargetMode") == "External")
                throw new FormatException($"{path}: invalid worksheet relationship.");
            var target = (string)relationship.Attribute("Target");
            var uri = new Uri(new Uri("https://workbook/xl/workbook.xml"), target);
            var entry = Uri.UnescapeDataString(uri.AbsolutePath).TrimStart('/');
            if (uri.Host != "workbook" || !entry.StartsWith("xl/", StringComparison.Ordinal))
                throw new FormatException($"{path}: invalid worksheet path.");
            var sharedEntry = zip.GetEntry("xl/sharedStrings.xml");
            var shared = sharedEntry == null
                ? Array.Empty<string>()
                : Load(zip, "xl/sharedStrings.xml")
                    .Root.Elements(Main + "si").Select(ReadText).ToArray();
            var rows = Load(zip, entry).Descendants(Main + "sheetData").Elements(Main + "row").ToList();
            if (rows.Count > 100000) throw new FormatException($"{path}: too many data rows.");
            var output = new List<string[]>();
            var width = -1;
            foreach (var row in rows)
            {
                var values = new Dictionary<int, string>();
                foreach (var cell in row.Elements(Main + "c"))
                {
                    var reference = (string)cell.Attribute("r");
                    if (cell.Element(Main + "f") != null)
                        throw new FormatException(
                            $"{path} {reference}: formulas are not supported; paste values before exporting.");
                    var type = (string)cell.Attribute("t");
                    var raw = (string)cell.Element(Main + "v");
                    var inline = cell.Element(Main + "is");
                    if (raw == null && inline == null) continue;
                    if (type == "e") throw new FormatException($"{path} {reference}: Excel error '{raw}'.");
                    var col = Column(reference);
                    if (col >= 64) throw new FormatException($"{path} {reference}: unexpected data column.");
                    string value;
                    if (type == "s")
                    {
                        if (!int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var index) ||
                            index < 0 || index >= shared.Length)
                            throw new FormatException($"{path} {reference}: invalid shared-string index.");
                        value = shared[index];
                    }
                    else if (type == "inlineStr") value = inline == null ? string.Empty : ReadText(inline);
                    else if (type == "b")
                        value = raw == "1" ? "TRUE"
                            : raw == "0" ? "FALSE"
                            : throw new FormatException($"{path} {reference}: invalid boolean.");
                    else value = raw ?? string.Empty;

                    values[col] = value;
                }

                if (width < 0)
                {
                    if ((string)row.Attribute("r") != "1" || values.Count == 0)
                        throw new FormatException($"{path}: put field headers on Data row 1.");
                    width = values.Keys.Max() + 1;
                }

                if (values.Any(x => x.Key >= width && !string.IsNullOrEmpty(x.Value)))
                    throw new FormatException($"{path}: data outside header columns.");
                var record = Enumerable.Range(0, width)
                    .Select(col => values.TryGetValue(col, out var value) ? value : string.Empty).ToArray();
                if (output.Count == 0 || record.Any(x => !string.IsNullOrWhiteSpace(x))) output.Add(record);
            }

            if (output.Count == 0) throw new FormatException($"{path}: data sheet is empty.");
            return output;
        }

        private static XDocument Load(ZipArchive zip, string name)
        {
            var entry = zip.GetEntry(name) ?? throw new FormatException($"Invalid Excel package: missing {name}.");
            using var stream = entry.Open();
            return XDocument.Load(stream, LoadOptions.PreserveWhitespace);
        }

        private static string ReadText(XElement element)
        {
            var text = string.Concat(element.Descendants(Main + "t").Where(x => !x.Ancestors(Main + "rPh").Any())
                .Select(x => x.Value));
            return Regex.Replace(text, @"_x([0-9a-fA-F]{4})_",
                m => ((char)int.Parse(m.Groups[1].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture))
                    .ToString());
        }

        private static int Column(string reference)
        {
            if (string.IsNullOrEmpty(reference)) throw new FormatException("Excel cell has no reference.");
            var value = 0;
            var letters = 0;
            foreach (var c in reference)
            {
                if (c < 'A' || c > 'Z') break;
                value = checked(value * 26 + c - 'A' + 1);
                letters++;
            }

            if (letters == 0) throw new FormatException($"Invalid Excel cell reference: {reference}");
            return value - 1;
        }
    }
}
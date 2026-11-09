#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using MasterMemory;
using UnityEditor;
using UnityEngine;

namespace Infrastructure.MasterData.Editor
{
    public static class MasterDataBuildMenu
    {
        public const string CoreOutputPath = "Assets/MasterData/Binary/master_core.bytes";
        public const string UIOutputPath = "Assets/MasterData/Binary/master_ui.bytes";
        public const string LocalizationOutputPath = "Assets/MasterData/Binary/master_localization.bytes";
        public const string ExcelDirectory = "Assets/MasterData/Excel";
        public const string CsvDirectory = "Assets/MasterData/CSV";

        [MenuItem("Tools/MasterData/Open Excel Folder")]
        public static void OpenExcelFolder() => EditorUtility.RevealInFinder(Absolute(ExcelDirectory));

        [MenuItem("Tools/MasterData/Build From Excel (Excel - CSV - Bytes)")]
        public static void BuildFromExcel()
        {
            var csv = MasterDataExcelReader.ReadAll(Absolute(ExcelDirectory));
            var data = MasterDataCsvImporter.Parse(csv);
            var binaries = Serialize(data); // Validate and build all outputs before changing any generated files.
            WriteCsv(csv);
            WriteBinaries(binaries);
            Debug.Log("Excel -> CSV -> MasterMemory completed (five tables, three databases).");
        }

        private static void Validate(MasterDataSourceData data)
        {
            var errors = MasterDataSourceValidator.Validate(data);
            if (errors.Count != 0)
                throw new InvalidOperationException("Master data validation failed:\n" + string.Join("\n", errors));
        }

        public static Dictionary<string, byte[]> Serialize(MasterDataSourceData data)
        {
            Validate(data);
            var core = new DatabaseBuilder();
            core.Append(data.Characters.Select(x => x.ToMaster()));
            core.Append(data.Skills.Select(x => x.ToMaster()));
            core.Append(data.Stages.Select(x => x.ToMaster()));
            var ui = new DatabaseBuilder();
            ui.Append(data.UI.Select(x => x.ToMaster()));
            var localization = new DatabaseBuilder();
            localization.Append(data.Localization.Select(x => x.ToMaster()));
            return new Dictionary<string, byte[]>
            {
                [CoreOutputPath] = core.Build(),
                [UIOutputPath] = ui.Build(),
                [LocalizationOutputPath] = localization.Build()
            };
        }

        public static void WriteCsv(IReadOnlyDictionary<string, string> tables)
        {
            Directory.CreateDirectory(Absolute(CsvDirectory));
            foreach (var name in MasterDataCsvImporter.TableNames)
                File.WriteAllText(Path.Combine(Absolute(CsvDirectory), name + ".csv"), tables[name],
                    new UTF8Encoding(true));
        }

        public static void WriteBinaries(IReadOnlyDictionary<string, byte[]> binaries)
        {
            foreach (var pair in binaries) Write(pair.Key, pair.Value);
            AssetDatabase.Refresh();
            Debug.Log("MasterMemory databases rebuilt.");
        }

        private static string Absolute(string path) =>
            Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath, "..", path));

        private static void Write(string path, ReadOnlySpan<byte> bytes)
        {
            var absolute = Absolute(path);
            Directory.CreateDirectory(Path.GetDirectoryName(absolute));
            File.WriteAllBytes(absolute, bytes.ToArray());
        }
    }
}
#endif
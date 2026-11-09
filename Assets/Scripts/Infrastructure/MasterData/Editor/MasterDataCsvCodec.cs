using System;
using System.Collections.Generic;
using System.Text;

namespace Infrastructure.MasterData.Editor
{
    public static class MasterDataCsvCodec
    {
        public static List<string[]> Read(string text)
        {
            if (text == null) throw new ArgumentNullException(nameof(text));
            var rows = new List<string[]>();
            var row = new List<string>();
            var cell = new StringBuilder();
            bool quoted = false, closed = false, started = false;

            void EndCell()
            {
                row.Add(cell.ToString());
                cell.Clear();
                closed = started = false;
            }

            void EndRow()
            {
                EndCell();
                rows.Add(row.ToArray());
                row.Clear();
            }

            for (var i = text.Length > 0 && text[0] == '\uFEFF' ? 1 : 0; i < text.Length; i++)
            {
                var c = text[i];
                if (quoted)
                {
                    if (c != '"') cell.Append(c);
                    else if (i + 1 < text.Length && text[i + 1] == '"')
                    {
                        cell.Append('"');
                        i++;
                    }
                    else
                    {
                        quoted = false;
                        closed = true;
                    }

                    continue;
                }

                if (c == ',')
                {
                    EndCell();
                    continue;
                }

                if (c == '\r' || c == '\n')
                {
                    EndRow();
                    if (c == '\r' && i + 1 < text.Length && text[i + 1] == '\n') i++;
                    continue;
                }

                if (closed) throw new FormatException("CSV: unexpected character after a closing quote.");
                if (c == '"')
                {
                    if (started || cell.Length != 0) throw new FormatException("CSV: quote inside an unquoted field.");
                    quoted = true;
                    started = true;
                }
                else
                {
                    started = true;
                    cell.Append(c);
                }
            }

            if (quoted) throw new FormatException("CSV: unterminated quoted field.");
            if (started || closed || cell.Length > 0 || row.Count > 0) EndRow();
            return rows;
        }

        public static string Write(IEnumerable<string[]> rows)
        {
            var output = new StringBuilder();
            foreach (var row in rows)
            {
                for (var i = 0; i < row.Length; i++)
                {
                    if (i != 0) output.Append(',');
                    var value = row[i] ?? string.Empty;
                    if (value.IndexOfAny(new[] { ',', '"', '\r', '\n' }) >= 0)
                        output.Append('"').Append(value.Replace("\"", "\"\"")).Append('"');
                    else output.Append(value);
                }

                output.Append("\r\n");
            }

            return output.ToString();
        }
    }
}
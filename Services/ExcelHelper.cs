using ClosedXML.Excel;

namespace mapos_dotnet.Services;

public static class ExcelHelper
{
    private static readonly XLColor HeaderBg  = XLColor.FromHtml("#1e40af");
    private static readonly XLColor HeaderFg  = XLColor.White;
    private static readonly XLColor AltRowBg  = XLColor.FromHtml("#f1f5f9");

    /// <summary>
    /// Generates an XLSX workbook and returns the raw bytes.
    /// </summary>
    public static byte[] Gerar(string sheetName, string[] cabecalhos, IEnumerable<object?[]> linhas)
    {
        using var wb = new XLWorkbook();
        var name = sheetName.Length > 31 ? sheetName[..31] : sheetName;
        var ws = wb.Worksheets.Add(name);

        // Header
        for (var i = 0; i < cabecalhos.Length; i++)
        {
            var cell = ws.Cell(1, i + 1);
            cell.Value = cabecalhos[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = HeaderBg;
            cell.Style.Font.FontColor = HeaderFg;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }

        // Data
        var row = 2;
        foreach (var linha in linhas)
        {
            if (row % 2 == 1)
                ws.Row(row).Style.Fill.BackgroundColor = AltRowBg;

            for (var col = 0; col < linha.Length; col++)
            {
                var val = linha[col];
                var cell = ws.Cell(row, col + 1);
                switch (val)
                {
                    case decimal d:
                        cell.Value = d;
                        cell.Style.NumberFormat.Format = "#,##0.00";
                        break;
                    case int n:
                        cell.Value = n;
                        break;
                    case bool b:
                        cell.Value = b ? "Sim" : "Não";
                        break;
                    case DateOnly dt:
                        cell.Value = dt.ToDateTime(TimeOnly.MinValue);
                        cell.Style.DateFormat.Format = "dd/MM/yyyy";
                        break;
                    case DateTime dtm:
                        cell.Value = dtm;
                        cell.Style.DateFormat.Format = "dd/MM/yyyy HH:mm";
                        break;
                    default:
                        cell.Value = val?.ToString() ?? "";
                        break;
                }
            }
            row++;
        }

        ws.Columns().AdjustToContents(1, Math.Max(row, 2));

        // Freeze header row
        ws.SheetView.FreezeRows(1);

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    public const string ContentType =
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
}

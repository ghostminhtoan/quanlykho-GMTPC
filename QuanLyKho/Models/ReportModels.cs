using System;

namespace QuanLyKho.Models;

public class WeeklyReportItem
{
    public string DayName { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string FormattedDate => Date.ToString("dd/MM");
    public int TotalImport { get; set; }
    public int TotalExport { get; set; }
    public int Difference => TotalImport - TotalExport;
    public string DiffText => Difference >= 0 ? $"+{Difference}" : $"{Difference}";
    public string DiffColor => Difference >= 0 ? "#10B981" : "#EF4444";
}

public class KpiSummary
{
    public int TotalProducts { get; set; }
    public int TotalStock { get; set; }
    public int LowStockCount { get; set; }
    public int TotalImportToday { get; set; }
    public int TotalExportToday { get; set; }
}

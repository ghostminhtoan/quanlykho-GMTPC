using System;
using SQLite;

namespace QuanLyKho.Models;

[Table("Products")]
public class Product
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed(Unique = true), MaxLength(50)]
    public string ProductCode { get; set; } = string.Empty;

    [MaxLength(200)]
    public string ProductName { get; set; } = string.Empty;

    [MaxLength(50)]
    public string Unit { get; set; } = "Cái";

    public int CurrentStock { get; set; }

    public int TotalImport { get; set; }

    public int TotalExport { get; set; }

    public DateTime? LastImportDate { get; set; }

    public DateTime? LastExportDate { get; set; }

    public int MinStockAlert { get; set; } = 10;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    [Ignore]
    public string StatusText => CurrentStock <= 0 ? "Hết hàng" : (CurrentStock <= MinStockAlert ? "Sắp hết" : "Còn hàng");

    [Ignore]
    public string StatusColor => CurrentStock <= 0 ? "#EF4444" : (CurrentStock <= MinStockAlert ? "#F59E0B" : "#10B981");

    [Ignore]
    public string LastImportFormatted => LastImportDate.HasValue ? LastImportDate.Value.ToString("dd/MM/yyyy HH:mm") : "-";

    [Ignore]
    public string LastExportFormatted => LastExportDate.HasValue ? LastExportDate.Value.ToString("dd/MM/yyyy HH:mm") : "-";
}

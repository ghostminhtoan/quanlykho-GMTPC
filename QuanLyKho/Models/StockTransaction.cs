using System;
using SQLite;

namespace QuanLyKho.Models;

[Table("StockTransactions")]
public class StockTransaction
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed]
    public int ProductId { get; set; }

    [MaxLength(50)]
    public string ProductCode { get; set; } = string.Empty;

    [MaxLength(200)]
    public string ProductName { get; set; } = string.Empty;

    [MaxLength(10)]
    public string TransactionType { get; set; } = "NHAP"; // "NHAP" hoặc "XUAT"

    public int Quantity { get; set; }

    public DateTime TransactionDate { get; set; } = DateTime.Now;

    [MaxLength(500)]
    public string Note { get; set; } = string.Empty;

    [Ignore]
    public string TypeText => TransactionType == "NHAP" ? "Nhập kho" : "Xuất kho";

    [Ignore]
    public string TypeColor => TransactionType == "NHAP" ? "#10B981" : "#EF4444";

    [Ignore]
    public string FormattedDate => TransactionDate.ToString("dd/MM/yyyy HH:mm");
}


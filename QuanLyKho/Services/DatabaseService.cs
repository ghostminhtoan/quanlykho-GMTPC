using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SQLite;
using QuanLyKho.Models;

namespace QuanLyKho.Services;

public class DatabaseService
{
    private static DatabaseService? _instance;
    public static DatabaseService Instance => _instance ??= new DatabaseService();

    private SQLiteAsyncConnection? _database;
    private readonly string _dbPath;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private bool _isInitialized;

    public DatabaseService()
    {
        string folder;
        if (OperatingSystem.IsAndroid())
        {
            folder = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
        }
        else
        {
            folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "QuanLyKho");
        }

        if (!Directory.Exists(folder))
        {
            Directory.CreateDirectory(folder);
        }

        _dbPath = Path.Combine(folder, "quanlykho.db");
    }

    public async Task InitializeAsync()
    {
        if (_isInitialized) return;

        await _initLock.WaitAsync();
        try
        {
            if (_isInitialized) return;

            if (_database == null)
            {
                _database = new SQLiteAsyncConnection(_dbPath, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache);
            }

            // Đảm bảo tạo tuần tự và đầy đủ cả 2 bảng
            await _database.CreateTableAsync<Product>();
            await _database.CreateTableAsync<StockTransaction>();

            var count = await _database.Table<Product>().CountAsync();
            if (count == 0)
            {
                await SeedInitialDataAsync();
            }

            _isInitialized = true;
        }
        finally
        {
            _initLock.Release();
        }
    }

    private async Task SeedInitialDataAsync()
    {
        var now = DateTime.Now;
        var sampleProducts = new List<Product>
        {
            new Product
            {
                ProductCode = "SP001",
                ProductName = "Laptop Dell Latitude 5420",
                Unit = "Máy",
                CurrentStock = 25,
                TotalImport = 30,
                TotalExport = 5,
                MinStockAlert = 5,
                LastImportDate = now.AddDays(-5),
                LastExportDate = now.AddDays(-1),
                CreatedAt = now.AddDays(-30),
                UpdatedAt = now.AddDays(-1)
            },
            new Product
            {
                ProductCode = "SP002",
                ProductName = "Bàn phím cơ Akko 3087",
                Unit = "Chiếc",
                CurrentStock = 12,
                TotalImport = 20,
                TotalExport = 8,
                MinStockAlert = 5,
                LastImportDate = now.AddDays(-3),
                LastExportDate = now.AddHours(-12),
                CreatedAt = now.AddDays(-20),
                UpdatedAt = now.AddHours(-12)
            },
            new Product
            {
                ProductCode = "SP003",
                ProductName = "Chuột không dây Logitech M331",
                Unit = "Con",
                CurrentStock = 4,
                TotalImport = 15,
                TotalExport = 11,
                MinStockAlert = 8,
                LastImportDate = now.AddDays(-10),
                LastExportDate = now.AddHours(-2),
                CreatedAt = now.AddDays(-25),
                UpdatedAt = now.AddHours(-2)
            },
            new Product
            {
                ProductCode = "SP004",
                ProductName = "Màn hình Dell UltraSharp 24 inch",
                Unit = "Chiếc",
                CurrentStock = 0,
                TotalImport = 10,
                TotalExport = 10,
                MinStockAlert = 3,
                LastImportDate = now.AddDays(-14),
                LastExportDate = now.AddDays(-2),
                CreatedAt = now.AddDays(-35),
                UpdatedAt = now.AddDays(-2)
            },
            new Product
            {
                ProductCode = "SP005",
                ProductName = "Tai nghe Sony WH-CH520",
                Unit = "Hộp",
                CurrentStock = 38,
                TotalImport = 40,
                TotalExport = 2,
                MinStockAlert = 5,
                LastImportDate = now.AddDays(-2),
                LastExportDate = now.AddHours(-5),
                CreatedAt = now.AddDays(-15),
                UpdatedAt = now.AddHours(-5)
            }
        };

        foreach (var p in sampleProducts)
        {
            await _database!.InsertAsync(p);
        }

        var p1 = await _database!.Table<Product>().Where(p => p.ProductCode == "SP001").FirstOrDefaultAsync();
        if (p1 != null)
        {
            await _database.InsertAsync(new StockTransaction
            {
                ProductId = p1.Id,
                ProductCode = p1.ProductCode,
                ProductName = p1.ProductName,
                TransactionType = "NHAP",
                Quantity = 30,
                TransactionDate = now.AddDays(-5),
                Note = "Nhập lô hàng chính hãng"
            });
            await _database.InsertAsync(new StockTransaction
            {
                ProductId = p1.Id,
                ProductCode = p1.ProductCode,
                ProductName = p1.ProductName,
                TransactionType = "XUAT",
                Quantity = 5,
                TransactionDate = now.AddDays(-1),
                Note = "Xuất kho giao công ty đối tác"
            });
        }

        var p3 = await _database.Table<Product>().Where(p => p.ProductCode == "SP003").FirstOrDefaultAsync();
        if (p3 != null)
        {
            await _database.InsertAsync(new StockTransaction
            {
                ProductId = p3.Id,
                ProductCode = p3.ProductCode,
                ProductName = p3.ProductName,
                TransactionType = "NHAP",
                Quantity = 15,
                TransactionDate = now.AddDays(-10),
                Note = "Nhập hàng tồn kho"
            });
            await _database.InsertAsync(new StockTransaction
            {
                ProductId = p3.Id,
                ProductCode = p3.ProductCode,
                ProductName = p3.ProductName,
                TransactionType = "XUAT",
                Quantity = 11,
                TransactionDate = now.AddHours(-2),
                Note = "Xuất bán lẻ khách hàng"
            });
        }
    }

    public async Task<List<Product>> GetProductsAsync(string? query = null)
    {
        await InitializeAsync();
        var all = await _database!.Table<Product>().OrderByDescending(p => p.UpdatedAt).ToListAsync();
        if (string.IsNullOrWhiteSpace(query))
            return all;

        query = query.Trim().ToLowerInvariant();
        return all.Where(p => p.ProductCode.ToLowerInvariant().Contains(query) ||
                              p.ProductName.ToLowerInvariant().Contains(query)).ToList();
    }

    public async Task<Product?> GetProductByIdAsync(int id)
    {
        await InitializeAsync();
        return await _database!.Table<Product>().Where(p => p.Id == id).FirstOrDefaultAsync();
    }

    public async Task<Product?> GetProductByCodeAsync(string code)
    {
        await InitializeAsync();
        return await _database!.Table<Product>().Where(p => p.ProductCode == code).FirstOrDefaultAsync();
    }

    public async Task AddProductAsync(Product product)
    {
        await InitializeAsync();
        product.CreatedAt = DateTime.Now;
        product.UpdatedAt = DateTime.Now;
        await _database!.InsertAsync(product);
    }

    public async Task UpdateProductAsync(Product product)
    {
        await InitializeAsync();
        product.UpdatedAt = DateTime.Now;
        await _database!.UpdateAsync(product);
    }

    public async Task DeleteProductAsync(int id)
    {
        await InitializeAsync();
        await _database!.Table<StockTransaction>().Where(t => t.ProductId == id).DeleteAsync();
        await _database!.DeleteAsync<Product>(id);
    }

    public async Task ProcessTransactionAsync(int productId, string type, int quantity, string note, DateTime date)
    {
        if (quantity <= 0)
            throw new ArgumentException("Số lượng phải lớn hơn 0.");

        await InitializeAsync();
        var product = await _database!.Table<Product>().Where(p => p.Id == productId).FirstOrDefaultAsync();
        if (product == null)
            throw new InvalidOperationException("Không tìm thấy sản phẩm.");

        if (type == "XUAT")
        {
            if (quantity > product.CurrentStock)
            {
                throw new InvalidOperationException($"Số lượng xuất ({quantity}) vượt quá tồn kho hiện tại ({product.CurrentStock}).");
            }

            product.CurrentStock -= quantity;
            product.TotalExport += quantity;
            product.LastExportDate = date;
        }
        else // "NHAP"
        {
            product.CurrentStock += quantity;
            product.TotalImport += quantity;
            product.LastImportDate = date;
        }

        product.UpdatedAt = DateTime.Now;

        var transaction = new StockTransaction
        {
            ProductId = product.Id,
            ProductCode = product.ProductCode,
            ProductName = product.ProductName,
            TransactionType = type,
            Quantity = quantity,
            TransactionDate = date,
            Note = note
        };

        // Ghi transaction và update product an toàn
        await _database.InsertAsync(transaction);
        await _database.UpdateAsync(product);
    }

    public async Task<KpiSummary> GetKpiSummaryAsync()
    {
        await InitializeAsync();
        var products = await _database!.Table<Product>().ToListAsync();
        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);

        var todayTrans = await _database.Table<StockTransaction>()
            .Where(t => t.TransactionDate >= today && t.TransactionDate < tomorrow)
            .ToListAsync();

        return new KpiSummary
        {
            TotalProducts = products.Count,
            TotalStock = products.Sum(p => p.CurrentStock),
            LowStockCount = products.Count(p => p.CurrentStock <= p.MinStockAlert),
            TotalImportToday = todayTrans.Where(t => t.TransactionType == "NHAP").Sum(t => t.Quantity),
            TotalExportToday = todayTrans.Where(t => t.TransactionType == "XUAT").Sum(t => t.Quantity)
        };
    }

    public async Task<List<StockTransaction>> GetDailyTransactionsAsync(DateTime date)
    {
        await InitializeAsync();
        var start = date.Date;
        var end = start.AddDays(1);

        var list = await _database!.Table<StockTransaction>()
            .Where(t => t.TransactionDate >= start && t.TransactionDate < end)
            .OrderByDescending(t => t.TransactionDate)
            .ToListAsync();

        return list;
    }

    public async Task<List<WeeklyReportItem>> GetWeeklyReportAsync(DateTime anchorDate)
    {
        await InitializeAsync();
        var result = new List<WeeklyReportItem>();
        for (int i = 6; i >= 0; i--)
        {
            var day = anchorDate.Date.AddDays(-i);
            var nextDay = day.AddDays(1);

            var dayTrans = await _database!.Table<StockTransaction>()
                .Where(t => t.TransactionDate >= day && t.TransactionDate < nextDay)
                .ToListAsync();

            string dayOfWeekName = day.DayOfWeek switch
            {
                DayOfWeek.Monday => "Thứ Hai",
                DayOfWeek.Tuesday => "Thứ Ba",
                DayOfWeek.Wednesday => "Thứ Tư",
                DayOfWeek.Thursday => "Thứ Năm",
                DayOfWeek.Friday => "Thứ Sáu",
                DayOfWeek.Saturday => "Thứ Bảy",
                DayOfWeek.Sunday => "Chủ Nhật",
                _ => day.ToString("ddd")
            };

            result.Add(new WeeklyReportItem
            {
                DayName = dayOfWeekName,
                Date = day,
                TotalImport = dayTrans.Where(t => t.TransactionType == "NHAP").Sum(t => t.Quantity),
                TotalExport = dayTrans.Where(t => t.TransactionType == "XUAT").Sum(t => t.Quantity)
            });
        }
        return result;
    }

    public async Task<(int totalImport, int totalExport, List<Product> topExportProducts)> GetMonthlyReportAsync(int month, int year)
    {
        await InitializeAsync();
        var start = new DateTime(year, month, 1);
        var end = start.AddMonths(1);

        var trans = await _database!.Table<StockTransaction>()
            .Where(t => t.TransactionDate >= start && t.TransactionDate < end)
            .ToListAsync();

        var totalImport = trans.Where(t => t.TransactionType == "NHAP").Sum(t => t.Quantity);
        var totalExport = trans.Where(t => t.TransactionType == "XUAT").Sum(t => t.Quantity);

        var grouped = trans.Where(t => t.TransactionType == "XUAT")
            .GroupBy(t => t.ProductId)
            .Select(g => new { ProductId = g.Key, Exported = g.Sum(x => x.Quantity) })
            .OrderByDescending(x => x.Exported)
            .Take(5)
            .ToList();

        var topProducts = new List<Product>();
        foreach (var item in grouped)
        {
            var p = await GetProductByIdAsync(item.ProductId);
            if (p != null)
            {
                topProducts.Add(p);
            }
        }

        return (totalImport, totalExport, topProducts);
    }
}

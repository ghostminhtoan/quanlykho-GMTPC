using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuanLyKho.Models;
using QuanLyKho.Services;

namespace QuanLyKho.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly DatabaseService _db = DatabaseService.Instance;
    private DispatcherTimer? _clockTimer;

    public MainViewModel()
    {
        _currentTimeText = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
        _clockTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _clockTimer.Tick += (s, e) =>
        {
            CurrentTimeText = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
        };
        _clockTimer.Start();

        _dailyDate = DateTimeOffset.Now;
        _selectedMonth = DateTime.Now.Month;
        _selectedYear = DateTime.Now.Year;

        _ = InitializeAppAsync();
    }

    private async Task InitializeAppAsync()
    {
        try
        {
            await _db.InitializeAsync();
            await LoadProductsAsync();
            await RefreshKpiAsync();
            await LoadDailyReportAsync();
            await LoadWeeklyReportAsync();
            await LoadMonthlyReportAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Lỗi khởi tạo: {ex.Message}";
        }
    }

    #region Banner KPI & Clock
    [ObservableProperty]
    private string _currentTimeText = string.Empty;

    [ObservableProperty]
    private int _totalProducts;

    [ObservableProperty]
    private int _totalStock;

    [ObservableProperty]
    private int _lowStockCount;

    [ObservableProperty]
    private int _totalImportToday;

    [ObservableProperty]
    private int _totalExportToday;

    private async Task RefreshKpiAsync()
    {
        try
        {
            var kpi = await _db.GetKpiSummaryAsync();
            TotalProducts = kpi.TotalProducts;
            TotalStock = kpi.TotalStock;
            LowStockCount = kpi.LowStockCount;
            TotalImportToday = kpi.TotalImportToday;
            TotalExportToday = kpi.TotalExportToday;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Lỗi cập nhật KPI: {ex.Message}";
        }
    }
    #endregion

    #region Tab Navigation
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsTab0Selected))]
    [NotifyPropertyChangedFor(nameof(IsTab1Selected))]
    [NotifyPropertyChangedFor(nameof(IsTab2Selected))]
    [NotifyPropertyChangedFor(nameof(IsTab3Selected))]
    private int _selectedTabIndex = 0;

    public bool IsTab0Selected => SelectedTabIndex == 0;
    public bool IsTab1Selected => SelectedTabIndex == 1;
    public bool IsTab2Selected => SelectedTabIndex == 2;
    public bool IsTab3Selected => SelectedTabIndex == 3;

    [RelayCommand]
    private void SelectTab(object? param)
    {
        if (param != null && int.TryParse(param.ToString(), out int tabIndex))
        {
            SelectedTabIndex = tabIndex;
            if (tabIndex == 1) _ = LoadDailyReportAsync();
            if (tabIndex == 2) _ = LoadWeeklyReportAsync();
            if (tabIndex == 3) _ = LoadMonthlyReportAsync();
        }
    }
    #endregion

    #region Product List & Search
    [ObservableProperty]
    private string _searchText = string.Empty;

    public ObservableCollection<Product> Products { get; } = new();

    [ObservableProperty]
    private Product? _selectedProduct;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _statusMessage;

    partial void OnSearchTextChanged(string value)
    {
        _ = LoadProductsAsync();
    }

    [RelayCommand]
    private async Task LoadProductsAsync()
    {
        IsLoading = true;
        try
        {
            var list = await _db.GetProductsAsync(SearchText);
            Products.Clear();
            foreach (var p in list)
            {
                Products.Add(p);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Lỗi tải sản phẩm: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }
    #endregion

    #region Add Product Modal
    [ObservableProperty]
    private bool _isAddProductModalVisible;

    [ObservableProperty]
    private string _newProductCode = string.Empty;

    [ObservableProperty]
    private string _newProductName = string.Empty;

    [ObservableProperty]
    private string _newProductUnit = "Cái";

    [ObservableProperty]
    private int _newProductInitialStock = 0;

    [ObservableProperty]
    private int _newProductMinAlert = 5;

    [ObservableProperty]
    private string? _addProductError;

    [RelayCommand]
    private void OpenAddProductModal()
    {
        NewProductCode = $"SP{DateTime.Now:fff}";
        NewProductName = string.Empty;
        NewProductUnit = "Cái";
        NewProductInitialStock = 0;
        NewProductMinAlert = 5;
        AddProductError = null;
        IsAddProductModalVisible = true;
    }

    [RelayCommand]
    private void CloseAddProductModal()
    {
        IsAddProductModalVisible = false;
        AddProductError = null;
    }

    [RelayCommand]
    private async Task SubmitAddProductAsync()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(NewProductCode))
            {
                AddProductError = "Vui lòng nhập mã sản phẩm.";
                return;
            }
            if (string.IsNullOrWhiteSpace(NewProductName))
            {
                AddProductError = "Vui lòng nhập tên sản phẩm.";
                return;
            }

            var existing = await _db.GetProductByCodeAsync(NewProductCode.Trim());
            if (existing != null)
            {
                AddProductError = "Mã sản phẩm này đã tồn tại.";
                return;
            }

            var product = new Product
            {
                ProductCode = NewProductCode.Trim().ToUpperInvariant(),
                ProductName = NewProductName.Trim(),
                Unit = string.IsNullOrWhiteSpace(NewProductUnit) ? "Cái" : NewProductUnit.Trim(),
                CurrentStock = Math.Max(0, NewProductInitialStock),
                TotalImport = Math.Max(0, NewProductInitialStock),
                MinStockAlert = Math.Max(0, NewProductMinAlert),
                LastImportDate = NewProductInitialStock > 0 ? DateTime.Now : null
            };

            await _db.AddProductAsync(product);

            if (NewProductInitialStock > 0)
            {
                var created = await _db.GetProductByCodeAsync(product.ProductCode);
                if (created != null)
                {
                    await _db.ProcessTransactionAsync(created.Id, "NHAP", NewProductInitialStock, "Nhập tồn kho ban đầu khi tạo sản phẩm", DateTime.Now);
                }
            }

            IsAddProductModalVisible = false;
            await LoadProductsAsync();
            await RefreshKpiAsync();
        }
        catch (Exception ex)
        {
            AddProductError = $"Lỗi: {ex.Message}";
        }
    }
    #endregion

    #region Transaction (Import / Export) Modal
    [ObservableProperty]
    private bool _isTransactionModalVisible;

    [ObservableProperty]
    private string _transactionModalTitle = string.Empty;

    [ObservableProperty]
    private string _transactionType = "NHAP";

    [ObservableProperty]
    private Product? _transactionProduct;

    [ObservableProperty]
    private int _transactionQuantity = 1;

    [ObservableProperty]
    private string _transactionNote = string.Empty;

    [ObservableProperty]
    private string? _transactionError;

    [RelayCommand]
    private void OpenImportModal(Product? product)
    {
        var target = product ?? SelectedProduct;
        if (target == null) return;

        TransactionProduct = target;
        TransactionType = "NHAP";
        TransactionModalTitle = $"Nhập Kho: {target.ProductName} ({target.ProductCode})";
        TransactionQuantity = 1;
        TransactionNote = "Nhập bổ sung kho";
        TransactionError = null;
        IsTransactionModalVisible = true;
    }

    [RelayCommand]
    private void OpenExportModal(Product? product)
    {
        var target = product ?? SelectedProduct;
        if (target == null) return;

        TransactionProduct = target;
        TransactionType = "XUAT";
        TransactionModalTitle = $"Xuất Kho: {target.ProductName} ({target.ProductCode})";
        TransactionQuantity = 1;
        TransactionNote = "Xuất hàng bán";
        TransactionError = null;
        IsTransactionModalVisible = true;
    }

    [RelayCommand]
    private void CloseTransactionModal()
    {
        IsTransactionModalVisible = false;
        TransactionError = null;
    }

    [RelayCommand]
    private async Task SubmitTransactionAsync()
    {
        if (TransactionProduct == null) return;

        if (TransactionQuantity <= 0)
        {
            TransactionError = "Số lượng phải lớn hơn 0.";
            return;
        }

        if (TransactionType == "XUAT" && TransactionQuantity > TransactionProduct.CurrentStock)
        {
            TransactionError = $"Số lượng xuất ({TransactionQuantity}) không thể lớn hơn tồn kho hiện tại ({TransactionProduct.CurrentStock})!";
            return;
        }

        try
        {
            await _db.ProcessTransactionAsync(
                TransactionProduct.Id,
                TransactionType,
                TransactionQuantity,
                TransactionNote.Trim(),
                DateTime.Now
            );

            IsTransactionModalVisible = false;
            await LoadProductsAsync();
            await RefreshKpiAsync();
            await LoadDailyReportAsync();
            await LoadWeeklyReportAsync();
            await LoadMonthlyReportAsync();
        }
        catch (Exception ex)
        {
            TransactionError = $"Lỗi: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task DeleteProductAsync(Product? product)
    {
        try
        {
            var target = product ?? SelectedProduct;
            if (target == null) return;

            await _db.DeleteProductAsync(target.Id);
            await LoadProductsAsync();
            await RefreshKpiAsync();
            await LoadDailyReportAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Lỗi xóa: {ex.Message}";
        }
    }
    #endregion

    #region Daily Report
    [ObservableProperty]
    private DateTimeOffset _dailyDate;

    partial void OnDailyDateChanged(DateTimeOffset value)
    {
        _ = LoadDailyReportAsync();
    }

    public ObservableCollection<StockTransaction> DailyTransactions { get; } = new();

    [ObservableProperty]
    private int _dailyTotalImport;

    [ObservableProperty]
    private int _dailyTotalExport;

    [RelayCommand]
    private async Task LoadDailyReportAsync()
    {
        try
        {
            var list = await _db.GetDailyTransactionsAsync(DailyDate.DateTime);
            DailyTransactions.Clear();
            foreach (var t in list)
            {
                DailyTransactions.Add(t);
            }

            DailyTotalImport = list.Where(t => t.TransactionType == "NHAP").Sum(t => t.Quantity);
            DailyTotalExport = list.Where(t => t.TransactionType == "XUAT").Sum(t => t.Quantity);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Lỗi báo cáo ngày: {ex.Message}";
        }
    }
    #endregion

    #region Weekly Report
    public ObservableCollection<WeeklyReportItem> WeeklyItems { get; } = new();

    [ObservableProperty]
    private int _weeklyTotalImport;

    [ObservableProperty]
    private int _weeklyTotalExport;

    [RelayCommand]
    private async Task LoadWeeklyReportAsync()
    {
        try
        {
            var list = await _db.GetWeeklyReportAsync(DateTime.Today);
            WeeklyItems.Clear();
            foreach (var item in list)
            {
                WeeklyItems.Add(item);
            }

            WeeklyTotalImport = list.Sum(x => x.TotalImport);
            WeeklyTotalExport = list.Sum(x => x.TotalExport);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Lỗi báo cáo tuần: {ex.Message}";
        }
    }
    #endregion

    #region Monthly Report
    [ObservableProperty]
    private int _selectedMonth;

    [ObservableProperty]
    private int _selectedYear;

    [ObservableProperty]
    private int _monthlyTotalImport;

    [ObservableProperty]
    private int _monthlyTotalExport;

    public ObservableCollection<Product> MonthlyTopProducts { get; } = new();

    [RelayCommand]
    private async Task LoadMonthlyReportAsync()
    {
        try
        {
            var (totalImport, totalExport, topProducts) = await _db.GetMonthlyReportAsync(SelectedMonth, SelectedYear);
            MonthlyTotalImport = totalImport;
            MonthlyTotalExport = totalExport;

            MonthlyTopProducts.Clear();
            foreach (var p in topProducts)
            {
                MonthlyTopProducts.Add(p);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Lỗi báo cáo tháng: {ex.Message}";
        }
    }
    #endregion
}

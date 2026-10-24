# Lịch Sử Khắc Phục Lỗi & Giải Pháp Phòng Ngừa (Bug Fixes History)

> **Mục đích**: Lưu trữ mọi lỗi phát sinh trong quá trình phát triển, phân tích nguyên nhân gốc rễ và đưa ra giải pháp phòng ngừa chuẩn xác để **không bao giờ lặp lại lỗi đó trong tương lai**.
> **Quy tắc**: Mỗi khi sửa xong một lỗi, bắt buộc ghi vào file này. **Mục mới nhất nằm ở trên cùng**, các mục cũ hơn nằm ở phía dưới.

---

### [BUG-002] Ô Nhập Liệu Bị Đổi Màu Trắng Xóa Khi Focus Hoặc Gõ Chữ
- **Ngày phát hiện & khắc phục**: 2026-10-24
- **Triệu chứng**:
  - Khi chưa gõ chữ, ô `TextBox` hiển thị nền tối bình thường.
  - Khi click chuột vào để focus hoặc nhập ký tự, toàn bộ ô nhập văn bản (Mã SP, Tên SP, Đơn vị, Ghi chú...) bị chuyển thành một khối màu trắng xóa (`#FFFFFF`), chữ bên trong bị biến màu xanh/đen hoặc trắng chìm trên nền trắng rất khó đọc.
- **Nguyên nhân gốc rễ**:
  1. Trong [`App.axaml`](QuanLyKho/App.axaml), cấu hình mặc định là `RequestedThemeVariant="Default"`. Khi chạy trên hệ điều hành Windows đang đặt giao diện sáng (Light Mode), Avalonia sẽ tự động kích hoạt bảng màu Fluent Light Theme.
  2. Fluent Light Theme có style mặc định cho trạng thái focus: `TextBox:focus /template/ Border#PART_BorderElement` sẽ gán `Background = TextControlBackgroundFocused` (là màu trắng `#FFFFFF`).
  3. Mặc dù thẻ cha (UserControl, Window, Border) được vẽ màu nền tối (`#0F172A`, `#1E293B`), nhưng khi `TextBox` nhận focus, style của FluentTheme đè lên và biến nền của nó thành màu trắng.
- **Cách đã xử lý**:
  1. Trong [`App.axaml`](QuanLyKho/App.axaml), đổi `RequestedThemeVariant="Default"` thành `RequestedThemeVariant="Dark"`. Toàn bộ ứng dụng sẽ hoạt động đồng nhất ở chế độ Dark Mode chuẩn.
  2. Thêm bộ style toàn cục trong [`App.axaml`](QuanLyKho/App.axaml) để cố định màu nền và viền cho `TextBox`, `NumericUpDown`, và `ButtonSpinner`:
     ```xml
     <Style Selector="TextBox:focus /template/ Border#PART_BorderElement">
         <Setter Property="Background" Value="#0F172A" />
         <Setter Property="BorderBrush" Value="#38BDF8" />
         <Setter Property="BorderThickness" Value="1.5" />
     </Style>
     <Style Selector="TextBox:focus">
         <Setter Property="Foreground" Value="#F8FAFC" />
     </Style>
     ```
  3. Xóa các thuộc tính inline cục bộ không cần thiết (`Background="#0F172A" Foreground="White"`) trong [`MainView.axaml`](QuanLyKho/Views/MainView.axaml) để các control tuân thủ theo style toàn cục.
- **Quy tắc phòng ngừa (Best Practice)**:
  - Khi thiết kế giao diện Dark Theme trong Avalonia, **bắt buộc** phải đặt `RequestedThemeVariant="Dark"` ở cấp Application (`App.axaml`), không phụ thuộc vào `Default` của hệ điều hành.
  - Luôn định nghĩa style ghi đè (override) cho các pseudo-classes `:pointerover`, `:focus`, `:disabled` của các control nhập liệu (`TextBox`, `NumericUpDown`, `ComboBox`, `DatePicker`).

---

### [BUG-001] Crash Ứng Dụng Khi Thêm Sản Phẩm & Lỗi "no such table: StockTransactions" Khi Nhập/Xuất Kho
- **Ngày phát hiện & khắc phục**: 2026-10-24
- **Triệu chứng**:
  - Khi người dùng thêm sản phẩm mới có kèm số lượng nhập ban đầu > 0, ứng dụng lập tức bị văng (crash) mà không có thông báo.
  - Khi bấm nút Nhập kho hoặc Xuất kho trên giao diện, xuất hiện dòng chữ đỏ báo lỗi: `no such table: StockTransactions`.
- **Nguyên nhân gốc rễ**:
  1. **Xung đột metadata SQLite attribute**: Trong model [`Product.cs`](QuanLyKho/Models/Product.cs), trường `ProductCode` được khai báo cả 2 attribute cùng lúc: `[Indexed, Unique]`. Trong thư viện `sqlite-net-pcl`, `[Indexed]` mặc định tạo chỉ mục với `Unique = false`, trong khi `[Unique]` tạo chỉ mục với `Unique = true` trên cùng một cột. SQLite ném ngoại lệ: `All the columns in an index must have the same value for their Unique property`. Lỗi này làm dừng chuỗi lệnh `InitializeAsync()` ngay tại câu lệnh tạo bảng `Products`, khiến câu lệnh tiếp theo (`CreateTableAsync<StockTransaction>()`) không bao giờ được chạy. Bảng `StockTransactions` vì thế chưa từng được tạo trong file `.db`.
  2. **Race condition khi khởi tạo bất đồng bộ**: Hàm `InitializeAsync()` trong `DatabaseService.cs` gán cờ `_database != null` ngay trước khi các bảng tạo xong. Khi app khởi động, nhiều task (`LoadProductsAsync`, `RefreshKpiAsync`, `LoadDailyReportAsync`) cùng gọi `InitializeAsync()` song song. Task 2 thấy `_database != null` liền trả về ngay và truy vấn bảng `StockTransactions` trong khi Task 1 còn chưa tạo xong bảng.
  3. **Thiếu khối try/catch trong Command**: Hàm `SubmitAddProductAsync` trong `MainViewModel.cs` không có khối `try ... catch`. Khi gặp lỗi không tìm thấy bảng `StockTransactions`, ngoại lệ bất đồng bộ chưa được xử lý (unhandled exception) đã đẩy thẳng lên Dispatcher khiến ứng dụng văng (crash).
- **Cách đã xử lý**:
  1. Trong [`Product.cs`](QuanLyKho/Models/Product.cs), sửa attribute `[Indexed, Unique]` thành cú pháp chuẩn duy nhất:
     ```csharp
     [Indexed(Unique = true), MaxLength(50)]
     public string ProductCode { get; set; } = string.Empty;
     ```
  2. Trong [`DatabaseService.cs`](QuanLyKho/Services/DatabaseService.cs), triển khai khóa `SemaphoreSlim _initLock = new(1, 1)` và biến cờ `_isInitialized` để đảm bảo chuỗi lệnh tạo bảng `Products` và `StockTransactions` được thực hiện tuần tự, an toàn đa luồng và chỉ chạy đúng 1 lần duy nhất.
  3. Trong [`MainViewModel.cs`](QuanLyKho/ViewModels/MainViewModel.cs), bọc toàn bộ logic trong `SubmitAddProductAsync`, `SubmitTransactionAsync`, `DeleteProductAsync` vào khối `try ... catch`, gán lỗi vào biến `AddProductError` hoặc `TransactionError` để hiển thị trên UI thay vì để ứng dụng bị crash.
- **Quy tắc phòng ngừa (Best Practice)**:
  - Trong `sqlite-net-pcl`, tuyệt đối **không bao giờ** kết hợp `[Indexed]` và `[Unique]` trên cùng một property. Luôn dùng `[Indexed(Unique = true)]`.
  - Mọi hàm Service có thao tác I/O khởi tạo bất đồng bộ (`InitializeAsync`) bắt buộc phải dùng `SemaphoreSlim` để ngăn chặn race condition.
  - Toàn bộ `[RelayCommand]` gọi đến Service hoặc Database trong ViewModel bắt buộc phải có khối `try ... catch` để bắt lỗi và hiển thị thân thiện trên UI.

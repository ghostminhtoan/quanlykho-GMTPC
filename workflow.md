# Workflow Chuẩn - Quản Lý Kho Đa Nền Tảng (Avalonia .NET 10)

Tài liệu quy tắc kỹ thuật, kiến trúc mã nguồn và luồng xử lý bắt buộc cho dự án **Quản Lý Kho** (Windows Desktop & Android Mobile).

---

## 1. Quy tắc cốt lõi & Kỷ luật Kỹ thuật (Core Rules)
- **Ngôn ngữ**: Luôn luôn giao tiếp và trả lời bằng **tiếng Việt**.
- **Kỹ năng bắt buộc**: Luôn luôn áp dụng quy trình từ skill `teamwork-preview-protocol` và `prompt-optimizer` cho mọi tác vụ lập trình (bóc tách kiến trúc, phòng ngừa rủi ro biên, mental dry-run).
- **YAGNI & KISS**: Sửa đúng trọng tâm, tối giản mã nguồn, tận dụng tối đa API/Helper chuẩn của Avalonia và .NET 10. Tuyệt đối không thêm abstraction thừa thãi.
- **Tiêu chuẩn biên dịch**: Sau khi hoàn thành code, bắt buộc chạy build dứt điểm đạt **0 Warning(s), 0 Error(s)** trước khi bàn giao.
- **Quy tắc Git & An toàn Mã nguồn**:
  - **Chỉ commit local** theo định dạng Conventional Commits (`<type>(<scope>): <mô tả>`).
  - **TUYỆT ĐỐI KHÔNG TỰ Ý PUSH GITHUB** trừ khi người dùng ra chỉ thị yêu cầu rõ ràng.
- **Trạng thái chuẩn cuối mỗi phản hồi**:
  ```text
  commit local: <mã hash>
  commit github: "không" (hoặc URL commit nếu người dùng yêu cầu push)
  path exe: <đường dẫn file exe sau build>
  ```

---

## 2. Kiến trúc Dự án & Cấu trúc Thư mục (Architecture)
- **Nền tảng**: .NET 10 (`net10.0`), C# latest.
- **Framework UI**: Avalonia UI 12.x (`avalonia.xplat` cross-platform single-codebase).
- **Mô hình kiến trúc**: MVVM chuẩn với `CommunityToolkit.Mvvm` (`[ObservableObject]`, `[ObservableProperty]`, `[RelayCommand]`).
- **Quản lý Package**: Central Package Management (`Directory.Packages.props`).
- **Solution & Projects**:
  - `QuanLyKho.slnx`: Solution file chính.
  - `QuanLyKho/QuanLyKho.csproj`: Thư viện logic dùng chung (Models, ViewModels, Views, Database Services).
  - `QuanLyKho.Desktop/QuanLyKho.Desktop.csproj`: Host chạy trên Windows Desktop (xuất file `.exe`).
  - `QuanLyKho.Android/QuanLyKho.Android.csproj`: Host chạy trên Android Mobile (xuất file `.apk`).
  - `QuanLyKho.Browser/` & `QuanLyKho.iOS/`: Sẵn sàng mở rộng cho WebAssembly và iOS.

---

## 3. Tiêu chuẩn Giao diện & Theme (UI/UX Standards)
- **Chế độ màu**: Bắt buộc đặt `RequestedThemeVariant="Dark"` trong `App.axaml`.
- **Bảng màu chủ đạo (Dark Slate Palette)**:
  - Nền ứng dụng chính: `#0F172A`
  - Nền thẻ / Card / Panel: `#1E293B`
  - Đường viền phân cách: `#334155` / `#475569`
  - Điểm nhấn chính (Primary Accent): `#3B82F6` / `#2563EB`
  - Trạng thái thành công / Tồn an toàn / Nhập kho: `#10B981`
  - Trạng thái cảnh báo / Sắp hết hàng: `#F59E0B`
  - Trạng thái nguy hiểm / Hết hàng / Xuất kho: `#EF4444`
  - Chữ chính (Foreground): `#F8FAFC`, Chữ phụ: `#94A3B8`
- **Quy chuẩn Input (TextBox & NumericUpDown)**:
  - Tuyệt đối không để ô nhập bị đổi thành nền trắng khi Focus/gõ chữ trên nền tối.
  - Phải dùng bộ style toàn cục định nghĩa trong `App.axaml` (`TextBox:focus`, `NumericUpDown:focus`, `ButtonSpinner`).
  - Dùng `PlaceholderText` thay cho thuộc tính lỗi thời `Watermark`.
- **Top Banner**:
  - Luôn duy trì đồng hồ thời gian thực (`DispatcherTimer` cập nhật mỗi giây).
  - Khối 5 thẻ KPI Dashboard trực quan: Tổng mặt hàng, Tổng tồn kho, Cảnh báo sắp hết, Lượng nhập hôm nay, Lượng xuất hôm nay.

---

## 4. Cơ sở Dữ liệu & Bất biến Nghiệp vụ (Database & Business Rules)
- **Công nghệ**: `sqlite-net-pcl` (1.11.285) - Hoạt động Offline-first 100%.
- **Vị trí lưu trữ Database (`quanlykho.db`)**:
  - Windows: `%LOCALAPPDATA%\QuanLyKho\quanlykho.db`
  - Android: `Environment.SpecialFolder.Personal/quanlykho.db`
- **Quy tắc Index Model**:
  - Đối với các trường định danh duy nhất (như `ProductCode`), sử dụng cú pháp chuẩn `[Indexed(Unique = true)]`. Tuyệt đối không dùng đồng thời cả 2 attribute `[Indexed, Unique]` vì sẽ gây xung đột metadata SQLite.
- **Khởi tạo An toàn Luồng (Thread-Safety)**:
  - `DatabaseService.InitializeAsync()` bắt buộc sử dụng `SemaphoreSlim _initLock` để bảo vệ, ngăn chặn race condition khi nhiều View/Task gọi khởi tạo cùng lúc lúc mở app.
- **Bất biến Kho Hàng (Inventory Invariants)**:
  - `CurrentStock` (Còn lại) = Tổng lượng nhập - Tổng lượng xuất.
  - Chặn xuất âm: Số lượng xuất **không bao giờ được vượt quá** tồn kho hiện tại.
  - Mọi thao tác Thêm, Nhập, Xuất, Xóa phải được bọc trong khối `try ... catch` để chống crash ứng dụng, hiển thị thông báo lỗi màu đỏ rõ ràng trên giao diện.

---

## 5. Lệnh Biên dịch & Đường dẫn Thực thi (Build & Run Commands)
- **Build Windows Desktop (Debug)**:
  ```powershell
  dotnet build QuanLyKho.Desktop\QuanLyKho.Desktop.csproj
  ```
- **Build Windows Desktop (Release - Sản phẩm chính)**:
  ```powershell
  dotnet build -c Release QuanLyKho.Desktop\QuanLyKho.Desktop.csproj
  ```
  *File thực thi nằm tại*:
  `R:\HDD R\ZC SYMLINK\USERS\source\repos\ghostminhtoan\Quản lý kho\QuanLyKho.Desktop\bin\Release\net10.0\QuanLyKho.Desktop.exe`
- **Build Android App**:
  ```powershell
  dotnet build QuanLyKho.Android\QuanLyKho.Android.csproj
  ```

# Lịch Sử Phát Triển Dự Án (Workflow History)

> **Quy tắc**: Mỗi lần code hoặc cập nhật tính năng xong, bắt buộc ghi tóm tắt công việc vào file này.
> **Thứ tự**: **Mục mới nhất nằm ở trên cùng**, các mục cũ hơn nằm ở phía dưới.

---

### [2026-10-24] - Thêm Nhật Ký Quy Trình & Sổ Tay Khắc Phục Lỗi
- **Tác vụ**:
  - Tạo file [`workflow history.md`](workflow%20history.md) để theo dõi tiến độ phát triển sau mỗi phiên lập trình.
  - Tạo file [`bug fixes history.md`](bug%20fixes%20history.md) để lưu trữ chi tiết nguyên nhân gốc rễ và giải pháp khắc phục triệt để mọi lỗi phát sinh.
  - Cập nhật [`workflow.md`](workflow.md) để liên kết chặt chẽ hai tài liệu này vào quy trình làm việc chuẩn bắt buộc.
- **Trạng thái**: Đã nghiệm thu, tài liệu được tích hợp vào dự án.

---

### [2026-10-24] - Sửa Lỗi Crash Thêm Sản Phẩm, Lỗi Bảng Giao Dịch & Lỗi Bảng Màu Focus
- **Tác vụ**:
  - Sửa xung đột attribute trong model `Product.cs` (`[Indexed, Unique]` ➔ `[Indexed(Unique = true)]`), giải phóng tiến trình khởi tạo SQLite.
  - Thêm cơ chế đồng bộ `SemaphoreSlim` trong `DatabaseService.cs`, đảm bảo bảng `StockTransactions` và `Products` luôn được tạo 100% trước khi xử lý nghiệp vụ.
  - Bọc khối phòng vệ `try ... catch` cho toàn bộ các command trong `MainViewModel.cs` (thêm sản phẩm, nhập kho, xuất kho, xóa sản phẩm) nhằm ngăn ngừa tình trạng crash ứng dụng.
  - Kích hoạt `RequestedThemeVariant="Dark"` trong `App.axaml` và bổ sung bộ Style Dark Mode toàn diện cho `TextBox`, `NumericUpDown`, `ButtonSpinner`, loại bỏ hoàn toàn hiện tượng ô nhập biến thành nền trắng khi click/gõ chữ.
- **Trạng thái**: Build đạt 0 Error, 0 Warning. Đã push mã nguồn lên GitHub.

---

### [2026-10-24] - Khởi Tạo Ứng Dụng Quản Lý Kho Đa Nền Tảng (Windows & Android)
- **Tác vụ**:
  - Khởi tạo kiến trúc đa nền tảng Single-Codebase bằng Avalonia UI 12.x (`avalonia.xplat`) trên .NET 10 (`net10.0`).
  - Thiết kế Top Banner với nhận diện thương hiệu, đồng hồ thời gian thực (`DispatcherTimer`) và 5 thẻ KPI Dashboard trực quan.
  - Xây dựng phân hệ Quản lý danh sách sản phẩm với DataGrid/Cards, lọc tìm kiếm tức thì và các nút thao tác nhanh (Nhập kho, Xuất kho, Thêm mới, Xóa).
  - Triển khai 3 phân hệ Báo cáo chuyên sâu: Báo cáo Ngày (phiếu giao dịch chi tiết), Báo cáo Tuần (biến động 7 ngày), Báo cáo Tháng (tổng kết nhập/xuất/tồn + Top 5 sản phẩm bán chạy).
  - Tích hợp hệ thống lưu trữ ngoại tuyến Offline-First bằng SQLite (`sqlite-net-pcl`).
- **Trạng thái**: Build thành công trên cả Windows Desktop (.exe) và Android (.apk).

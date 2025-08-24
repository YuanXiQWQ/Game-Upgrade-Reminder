# Trình nhắc Nâng cấp Trò chơi

[![License: AGPL-3.0](https://img.shields.io/badge/License-AGPL--3.0-blue.svg)](https://opensource.org/licenses/AGPL-3.0)

---

Một công cụ để ghi lại và theo dõi tiến trình nâng cấp trò chơi mất nhiều thời gian. Ban đầu được tạo cho **Boom Beach**.

## Tính năng

- 🕒 Theo dõi các nhiệm vụ nâng cấp trên nhiều tài khoản
- ⏰ Khác với lịch/báo thức, bộ đếm ngược được đồng bộ với trò chơi, loại bỏ việc phải tính toán thời gian thủ công mỗi lần
- 🔔 Hiển thị thông báo hệ thống khi hoàn tất nâng cấp
- ♻️ Nhiệm vụ lặp lại: hàng ngày / hàng tuần / hàng tháng / hàng năm / tùy chỉnh; thời gian kết thúc tùy chọn (mặc định: không); hỗ trợ quy tắc bỏ qua
- 🌐 Hỗ trợ 27 ngôn ngữ

## Yêu cầu hệ thống

- [Windows 10](https://www.microsoft.com/en-ca/software-download/windows10) hoặc mới hơn
- [.NET 8.0](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) hoặc mới hơn

> Không chắc các phiên bản khác có hoạt động không :<

## Cài đặt

1. Tải phiên bản mới nhất từ trang [Releases](https://github.com/YuanXiQWQ/Game-Upgrade-Reminder/releases)
2. Giải nén vào bất kỳ thư mục nào
3. Chạy `Game Upgrade Reminder.exe`

## Hướng dẫn sử dụng

### Thêm nhiệm vụ nâng cấp

1. Chọn tài khoản ở phía trên giao diện
2. Chọn hoặc tạo tên nhiệm vụ (có thể để trống)
3. Đặt thời gian cần thiết: thời gian bắt đầu, ngày, giờ, phút (nếu không đặt thời gian bắt đầu, mặc định sẽ là thời gian hệ thống hiện tại)
4. Nhấn nút "Thêm" để tạo nhiệm vụ

### Quản lý nhiệm vụ

- Các nhiệm vụ đến hạn sẽ được làm nổi bật; nhấn "Hoàn tất" để đánh dấu hoàn thành
- Có thể xóa nhiệm vụ khỏi danh sách, và việc xóa có thể được hoàn tác trong vòng ba giây

## Câu hỏi thường gặp

### Không nhận được thông báo hệ thống

- Tắt **Trợ lý Tập trung (Focus Assist)** hoặc thêm `Game Upgrade Reminder.exe` vào danh sách ưu tiên. Nếu quy tắc tự động được đặt thành "Chỉ báo thức", hãy thay đổi thành "Chỉ ưu tiên".
- Ngoài ra thì tôi không rõ

### Các vấn đề kỳ lạ khác

- Có lẽ là lỗi (bug), cứ bỏ qua
- Có thể báo cáo trên trang Issues, nhưng có lẽ tôi sẽ không biết cách sửa

## Giấy phép

Dự án này được cấp phép theo [GNU Affero General Public License v3.0](../LICENSE).
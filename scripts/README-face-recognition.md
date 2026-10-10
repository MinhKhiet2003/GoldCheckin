# Cấu hình nhận diện khuôn mặt miễn phí

GoldCheckin dùng CompreFace tự host để lưu ảnh mẫu và so khớp ảnh chấm công. Camera trên điện thoại chỉ chụp ảnh; việc nhận diện diễn ra ở API CompreFace trên máy chạy GoldCheckin.

## Thiết lập trên Windows

1. Cài Docker Desktop, bật WSL 2, mở Docker Desktop và đợi Docker Engine chạy. CompreFace chính thức yêu cầu Docker/Docker Compose; máy cần CPU x86 có AVX.
2. Mở PowerShell tại thư mục gốc repository `Base` và chạy:

   ```powershell
   Set-ExecutionPolicy -Scope Process Bypass
   .\scripts\setup-compreface.ps1
   ```

3. Script tải bản phát hành CompreFace chính thức, khởi động các container, mở `http://localhost:8000/login`, xác thực API key và lưu cấu hình vào `Base/appsettings.Development.json`. Cổng CompreFace chỉ bind vào `127.0.0.1`, không công khai API nhận diện ra mạng nội bộ/Internet. File cấu hình đã được Git bỏ qua; không commit API key.
4. Trên trang CompreFace, tạo một **Face Recognition service**, sao chép API key của service đó rồi dán vào PowerShell khi script hỏi. Sau khi script báo hoàn tất, khởi động lại API GoldCheckin.
5. Nhân viên mở trang **Chấm công**, bấm **Bắt đầu đăng ký khuôn mặt → Chụp ảnh khuôn mặt → Lưu khuôn mặt vào thư viện**. Khi đã lưu lần đầu, lần sau mở camera, chụp ảnh mới và xác nhận chấm công.

## Kiểm tra và khắc phục

```powershell
docker compose ps
```

Nếu API key sai, chạy lại script và nhập đúng key của **Face Recognition service**. Nếu service không sẵn sàng, xem log tại thư mục CompreFace mà script tải về bằng `docker compose logs -f compreface-api`.

CompreFace quản lý ảnh mẫu trong volume Docker. Không xóa volume nếu cần giữ thư viện khuôn mặt. Địa chỉ `localhost:8000` chỉ dành cho API GoldCheckin chạy trên cùng máy với Docker; tunnel công khai chỉ cần đưa giao diện/API GoldCheckin tới điện thoại, không cần công khai cổng CompreFace.

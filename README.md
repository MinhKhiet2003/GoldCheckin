# Base API

Base API là nền tảng .NET 10 cho các dự án cần quản lý người dùng, xác thực JWT, phân quyền, file, thông báo và nhật ký hoạt động. Dự án dùng ASP.NET Core, Entity Framework Core và PostgreSQL.

## Khởi động dự án

1. Cập nhật `Base/appsettings.json` với chuỗi kết nối PostgreSQL và cấu hình JWT.
2. Dùng database mới hoặc trống nếu database cũ còn lịch sử migration trước khi dự án được tạo lại.
3. Chạy tại thư mục chứa `Base.slnx`:

```powershell
dotnet ef database update --project Base/Base.csproj
dotnet run --project Base/Base.csproj --launch-profile https
```

Swagger chạy tại `https://localhost:7015/swagger` trong môi trường Development.

Migration tạo sẵn tài khoản quản trị đầu tiên:

```json
{
  "username": "admin",
  "password": "admin"
}
```

Tài khoản này có role `admin`. Mật khẩu trong database được băm, không được lưu dạng văn bản.

## Cấu trúc thư mục

```text
Base/
├── Authorization/            Role, permission và handler kiểm tra quyền
├── Common/                   Thành phần dùng chung, audit context, response writer
├── Controllers/              Các endpoint HTTP
├── Data/                     DbContext, factory migration, seed dữ liệu
├── Entities/                 Model ánh xạ bảng database
├── Exceptions/               Lỗi nghiệp vụ ApiException
├── Extensions/               Cấu hình API, validation, OpenAPI
├── Filters/                  Lọc response an toàn cho audit log
├── Middleware/               Nhật ký request API
├── Migrations/               Lịch sử thay đổi database
├── Models/
│   ├── Requests/             DTO client gửi lên
│   └── Responses/            DTO API trả về
├── Repositories/             Truy cập dữ liệu
│   └── IRepositories/        Interface repository
├── Services/                 Nghiệp vụ
│   └── IServices/            Interface service
└── wwwroot/uploads/          File đã upload
```

Request và Response được tách thành từng file. Không trả Entity trực tiếp cho client.

## Luồng xử lý

```text
Client → Controller → IService → Service → IRepository → Repository → AppDbContext → PostgreSQL
```

Ví dụ `PUT /api/accounts/me`:

1. JWT kiểm tra token, thời hạn và trạng thái tài khoản.
2. Permission handler kiểm tra quyền `users.profile.update`.
3. Controller lấy ID tài khoản từ claim `sub` và gọi service.
4. Service kiểm tra nghiệp vụ, tạo snapshot trước/sau thay đổi và lưu qua repository.
5. Controller trả `ApiResponse<UserResponse>`.
6. Middleware ghi nhật ký request và audit data.

Controller chỉ xử lý HTTP; Service chứa nghiệp vụ; Repository chỉ truy cập database.

## Xác thực và phiên đăng nhập

`POST /api/accounts/login` trả access token. Client gửi token cho các API bảo vệ:

```http
Authorization: Bearer <accessToken>
```

Access token có thời hạn 30 phút. Mỗi request xác minh token, tài khoản có tồn tại/đang hoạt động và lấy lại role hiện tại từ database. Vì vậy token cũ không giữ quyền sau khi tài khoản bị khóa hoặc bị hạ role.

Hệ thống đã có Entity cho `RefreshToken` và `PasswordResetToken` để triển khai tiếp refresh token, đăng xuất nhiều thiết bị, quên mật khẩu và đặt lại mật khẩu. Các endpoint xác thực nâng cao này chưa được công khai cho đến khi có cơ chế gửi email/SMS hoặc kênh trao token an toàn.

## Role và permission

Role trong bảng `"User"` gồm `admin` và `user`. `Permission` là danh mục quyền; `RolePermission` ánh xạ role với permission.

| Permission | admin | user |
|---|---:|---:|
| `users.profile.read`, `users.profile.update` | Có | Có |
| `users.list`, `users.read`, `users.update`, `users.delete` | Có | Không |
| `files.upload` | Có | Có |
| `notifications.read`, `notifications.update` | Có | Có |
| `system-logs.read`, `roles.manage`, `permissions.manage` | Có | Không |
| `settings.manage`, `dashboard.read` | Có | Không |

Đăng ký công khai luôn tạo role `user`. Admin có thể đổi role và khóa/mở khóa qua API cập nhật tài khoản.

Khi thêm một permission: thêm hằng số tại `Authorization/AppPermissions.cs`, seed ở `AppDbContext`, tạo policy tại `Program.cs`, rồi gắn `[Authorize(Policy = ...)]` vào endpoint.

## API hiện có

| Method | Endpoint | Mục đích |
|---|---|---|
| POST | `/api/accounts/register` | Đăng ký tài khoản user |
| POST | `/api/accounts/login` | Đăng nhập và nhận JWT |
| GET / PUT | `/api/accounts/me` | Xem/cập nhật thông tin cá nhân |
| GET | `/api/accounts` | Admin: danh sách, tìm kiếm, lọc, phân trang |
| GET / PUT / DELETE | `/api/accounts/{id}` | Admin: xem, cập nhật, xóa tài khoản |
| POST | `/api/files/upload` | Upload JPG, PNG, PDF, TXT |
| POST | `/api/accounts/me/avatar` | Upload và gán avatar JPG/PNG/JPEG cho tôi |
| GET | `/api/files/{id}` | Xem/tải file của tôi; admin xem mọi file |
| DELETE | `/api/files/{id}` | Xóa file của tôi; admin xóa mọi file |
| GET | `/api/notifications` | Danh sách thông báo của tôi |
| GET | `/api/notifications/unread-count` | Số thông báo chưa đọc |
| PUT | `/api/notifications/{id}/read` | Đánh dấu một thông báo đã đọc |
| PUT | `/api/notifications/read-all` | Đánh dấu tất cả đã đọc |
| GET | `/api/dashboard` | Admin: tổng quan người dùng và đăng nhập gần đây |
| GET | `/api/system-logs` | Admin: danh sách nhật ký |
| GET | `/api/system-logs/{id}` | Admin: chi tiết nhật ký và audit data |

Danh sách tài khoản hỗ trợ `pageNumber`, `pageSize`, `keyword`, `role`, `isActive`. `pageSize` từ 1 đến 100.

## Phản hồi API

Mọi API nghiệp vụ dùng cùng cấu trúc:

```json
{
  "statusCode": 200,
  "success": true,
  "message": "Thao tác thành công.",
  "data": {},
  "errors": null
}
```

Lỗi validation có `errors` theo tên trường và thông báo tiếng Việt. HTTP status luôn trùng `statusCode` trong body. Danh sách dùng `ApiResponse<PagedResult<T>>` với `items`, `pageNumber`, `pageSize`, `totalItems`, `totalPages`.

Swagger ẩn danh sách response mẫu để giao diện gọn. Kết quả thực tế sau khi bấm **Execute** vẫn hiển thị đầy đủ.

## Quản lý file

`POST /api/files/upload` dùng `multipart/form-data`, trường có tên `file`.

- Tối đa 10 MB, mỗi request một file.
- Chỉ nhận `.jpg`, `.jpeg`, `.png`, `.pdf`, `.txt`.
- Ảnh/PDF được kiểm tra chữ ký nội dung.
- File được đổi tên bằng GUID và lưu tại `wwwroot/uploads`.
- Metadata lưu trong `UploadedFile`; nếu lưu metadata lỗi, file vừa tạo được xóa.

API trả URL tương đối như `/uploads/<guid>.png`. Client dùng URL đó để cập nhật `avatarUrl`. URL file công khai; ảnh hiển thị trực tiếp, PDF/TXT tải xuống. Chưa tích hợp quét virus.

`POST /api/accounts/me/avatar` upload và gán avatar trong một request; chỉ nhận JPG, JPEG, PNG.

## Cấu hình hệ thống

Admin quản lý các cấu hình chung qua `GET /api/system-settings` và `PUT /api/system-settings/{key}`. Key chỉ gồm chữ/số/dấu chấm/gạch dưới/gạch ngang.

Nên dùng các key thống nhất: `system.name`, `system.logoUrl`, `system.faviconUrl`, `organization.name`, `organization.taxCode`, `organization.address`, `organization.phone`, `organization.email`, `organization.website`. Logo và favicon nên upload trước, sau đó lưu URL trả về vào cấu hình.

## Nhật ký hệ thống và audit

Mỗi request `/api` được ghi vào `SystemLog`: người thực hiện, method, path, status code, IP, trace ID, thời gian xử lý và thời điểm.

Với các thao tác thay đổi dữ liệu thành công, log có thêm:

| Trường | Ý nghĩa |
|---|---|
| `action` | Create, Update, Delete, Login, Upload |
| `entityType`, `entityId` | Bản ghi bị tác động |
| `beforeData` | Snapshot trước khi sửa/xóa |
| `afterData` | Snapshot sau khi thêm/sửa/upload |
| `responseData` | Dữ liệu response đã được cho phép ghi log |

Không lưu password, password hash, access token, request body, query string hay nội dung file. Danh sách log trả metadata gọn; gọi `GET /api/system-logs/{id}` để xem snapshot chi tiết.

Ghi log dùng scope database riêng. Nếu ghi log thất bại, nghiệp vụ chính không bị thất bại theo.

## Database và migration

Giữ toàn bộ migration trong source control và áp dụng theo thứ tự. Khi thay đổi Entity hoặc mapping:

```powershell
dotnet ef migrations add TenMigration --project Base/Base.csproj
dotnet ef database update --project Base/Base.csproj
```

Không sửa/xóa migration đã áp dụng ở môi trường dùng chung. Để làm lại dữ liệu phát triển, sử dụng database trống.

## Phần nền đã chuẩn bị để phát triển tiếp

Danh sách yêu cầu đã có phần nền database/permission nhưng chưa có endpoint nghiệp vụ hoàn chỉnh:

- Refresh token, thu hồi phiên, đăng xuất một/tất cả thiết bị.
- Quên mật khẩu, đặt lại mật khẩu, đổi mật khẩu; cần tích hợp email/SMS trước khi phát hành.
- CRUD role/permission, gán permission theo role.
- Admin tạo tài khoản riêng và cấu hình hệ thống.
- Gửi thông báo từ nghiệp vụ/admin; hiện API mới quản lý thông báo đã có.
- Xem/tải/xóa file riêng theo quyền; hiện đã có upload và static URL công khai.

Khi triển khai các phần này, giữ đúng luồng Controller → Service → Repository → DbContext, tạo DTO riêng và bổ sung audit snapshot an toàn.

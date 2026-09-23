# Base API

.NET 10, JWT Bearer, EF Core và PostgreSQL.

## Cấu trúc

- Controllers: tiếp nhận yêu cầu và trả ApiResponse<T>.
- Services/IServices và Services: hợp đồng và xử lý nghiệp vụ.
- Repositories/IRepositories và Repositories: hợp đồng và truy cập dữ liệu.
- Data: DbContext và factory phục vụ migration.
- Entities: thực thể lưu trong database.
- Models/Requests: mỗi DTO đầu vào nằm trong một file riêng.
- Models/Responses: UserResponse, LoginResponse, ApiResponse<T>, PagedResult<T>.
- Exceptions: lỗi nghiệp vụ.
- Common: ghi phản hồi lỗi chung.
- Extensions: cấu hình validation và OpenAPI.
- Migrations: lịch sử cấu trúc database, cần giữ để cập nhật database.

## Chạy

Cấu hình ConnectionStrings:DefaultConnection và Jwt trong Base/appsettings.json.
Chạy tại thư mục chứa Base.slnx:

```powershell
dotnet ef database update --project Base/Base.csproj
dotnet run --project Base/Base.csproj --launch-profile https
```

Swagger: https://localhost:7015/swagger khi chạy môi trường Development.
Đăng nhập, lấy data.accessToken và dán vào Authorize (không thêm tiền tố Bearer).

## API

| Phương thức | Đường dẫn | Quyền |
|---|---|---|
| POST | /api/accounts/register | Công khai |
| POST | /api/accounts/login | Công khai |
| GET | /api/accounts/me | Đăng nhập |
| PUT | /api/accounts/me | Đăng nhập, chỉ sửa hồ sơ của mình |
| GET | /api/accounts?pageNumber=1&pageSize=20 | Admin |
| GET | /api/accounts/{id} | Admin |
| PUT | /api/accounts/{id} | Admin |
| DELETE | /api/accounts/{id} | Admin, xóa vĩnh viễn |

Phân quyền sử dụng cột Role trong bảng User và bảng Permission/RolePermission; không còn sử dụng Authorization:AdminAccountIds.
PUT thay thế toàn bộ hồ sơ: FullName/Email bắt buộc, các trường tùy chọn bị bỏ qua sẽ được đặt null. AdminUpdateRequest có thêm IsActive. Username không đổi sau đăng ký.

## Phản hồi chung

API trả ApiResponse<T>, gồm statusCode, success, message, data, errors. Tên thuộc tính JSON dùng camelCase. HTTP status khớp với statusCode trong body.
Danh sách trả ApiResponse<PagedResult<UserResponse>>:

```json
{
  "statusCode": 200,
  "success": true,
  "message": "Lấy danh sách tài khoản thành công.",
  "data": {
    "items": [],
    "pageNumber": 1,
    "pageSize": 20,
    "totalItems": 0,
    "totalPages": 0
  },
  "errors": null
}
```

Lỗi validation chứa errors dạng từ điển tên trường và danh sách thông báo. Các lỗi nghiệp vụ, xác thực, phân quyền, đường dẫn API, giới hạn tần suất và lỗi máy chủ dùng cùng cấu trúc. Không trả thông tin exception nội bộ cho client.
Đăng ký thành công trả 201; đọc, đăng nhập, cập nhật và xóa trả 200. Xóa có data = null. Không dùng 204 để phản hồi xóa vẫn có body ApiResponse.
Phân trang dùng pageNumber thay cho page; totalItems thay cho totalCount. PageSize từ 1 đến 100.
Swagger UI và tài liệu OpenAPI giữ định dạng riêng để trình duyệt và công cụ đọc được.

## Quy tắc tài khoản

Password được băm tại máy chủ; không trả PasswordHash. Password dài 12–128 ký tự. Username 3–50 ký tự, chỉ chữ Latin, số, dấu chấm, gạch ngang và gạch dưới. Username/email được chuẩn hóa chữ thường và kiểm tra trùng.
Gender: Male, Female, Other, PreferNotToSay hoặc null. DateOfBirth không ở tương lai.
Token có thời hạn 30 phút; tài khoản bị khóa hoặc xóa sẽ bị từ chối. Đổi email/điện thoại đặt lại cờ xác minh. Các luồng OTP, xác minh email/SMS, quên/đổi mật khẩu và refresh token chưa được triển khai.
Các file test, request mẫu và script SQL trùng với migration đã được xóa theo yêu cầu.
## Role và permission

Bộ migration đã được tạo lại từ đầu thành InitialCreate. Migration tạo User, Permission, RolePermission và tài khoản admin mặc định. Chỉ áp dụng lên database mới/trống. Database cũ vẫn giữ nguyên và không tương thích với lịch sử migration mới; không chạy database update trực tiếp lên database cũ. Hãy đổi Database trong ConnectionStrings:DefaultConnection sang tên database mới trước khi chạy:

```powershell
dotnet ef database update --project "K:\Ta Minh Khiet - Admin\Project\Base\Base\Base.csproj"
```

Role hợp lệ: admin, user. Đăng ký công khai luôn tạo user, bỏ qua role do client gửi. Admin PUT /api/accounts/{id} có thể gửi thêm role; bỏ qua hoặc gửi null sẽ giữ role hiện tại. PUT /me không cho sửa role.

| Permission | admin | user |
|---|---|---|
| users.profile.read | Có | Có |
| users.profile.update | Có | Có |
| users.list | Có | Không |
| users.read | Có | Không |
| users.update | Có | Không |
| users.delete | Có | Không |

Permission là danh mục quyền; RolePermission ánh xạ role tới permission. Quyền được đọc từ database mỗi yêu cầu. JWT chứa role nhưng máy chủ cập nhật role từ database khi xác thực, nên token cũ không giữ quyền admin sau khi bị hạ quyền. API cập nhật quản trị yêu cầu cả role admin và permission users.update vì có thể đổi role.

Tài khoản được tạo tự động khi áp dụng InitialCreate:

- Username: admin
- Password: admin
- Role: admin
- FullName: Quản trị viên hệ thống
- Email: admin@example.com
- Address: Việt Nam
- Gender: PreferNotToSay
- IsActive: true
- EmailVerified / PhoneVerified: false
- PhoneNumber, AvatarUrl, DateOfBirth, LastLoginAt: null

Mật khẩu được lưu bằng PasswordHasher của ASP.NET Core Identity. Seed sử dụng hash, Id và thời gian cố định để migration không thay đổi mỗi lần build. Đăng ký công khai vẫn yêu cầu mật khẩu 12–128 ký tự; tài khoản khởi tạo admin/admin được seed trực tiếp. Đăng nhập bằng POST /api/accounts/login:

```json
{
  "username": "admin",
  "password": "admin"
}
```

Tên bảng User cần đặt trong dấu ngoặc kép khi viết SQL PostgreSQL. Entity C# vẫn là Account và đường dẫn API vẫn là /api/accounts để giữ tương thích.

Swagger ẩn bảng phản hồi mẫu khai báo sẵn; sau Execute vẫn hiển thị mã HTTP và body thực tế. API vẫn trả ApiResponse<T> với thông báo đúng trường hợp. OpenAPI giữ schema thành công để các công cụ sinh client sử dụng, không liệt kê sẵn toàn bộ mã lỗi.
## Upload file

POST /api/files/upload, yêu cầu JWT và permission files.upload (admin và user đều được cấp mặc định). Trong Swagger chọn Try it out, chọn File rồi Execute. Request dùng multipart/form-data, không gửi JSON. Mỗi lần một file, tối đa 10 MB; hỗ trợ .jpg, .jpeg, .png, .pdf, .txt. Kiểm tra chữ ký file ảnh/PDF, tên file lưu là GUID do máy chủ tạo. Đây không phải cơ chế quét virus.

File lưu tại Base/wwwroot/uploads; metadata lưu trong UploadedFile gồm người upload, tên gốc, tên lưu, dung lượng, loại nội dung, URL và thời gian UTC. Nếu lưu metadata thất bại, file vừa tạo được dọn. Không đặt khóa ngoại tới User để thông tin người upload vẫn còn khi tài khoản bị xóa.

Response data gồm id, originalName, url, contentType, size (byte), createdAt. Ghép URL tương đối với địa chỉ API để truy cập, ví dụ https://localhost:7015/uploads/<guid>.png. URL file là công khai. PDF/TXT được tải xuống dạng attachment; ảnh có thể hiển thị trực tiếp. Không ghi đè file dựa trên tên client gửi. File không tự động cập nhật AvatarUrl; dùng url trả về để tạo đường dẫn đầy đủ khi cập nhật hồ sơ.

## Nhật ký hệ thống

GET /api/system-logs?pageNumber=1&pageSize=20, yêu cầu role admin và permission system-logs.read.
Bộ lọc tùy chọn: userId, statusCode, from, to; thời gian dùng ISO 8601 có múi giờ, ví dụ 2026-09-23T00:00:00Z.

Middleware ghi các request /api sau khi xử lý, bao gồm thành công, đăng nhập sai, validation, phân quyền, lỗi máy chủ và upload. Nhật ký lưu UserId (null nếu chưa xác thực), Method, Path, StatusCode, Success, Message, IpAddress, TraceId, DurationMs, CreatedAt. Đăng ký/đăng nhập thành công gắn ID tài khoản tương ứng. Không lưu body request, query string, mật khẩu, token hoặc nội dung file. Dữ liệu response và bản chụp trước/sau được lưu có chọn lọc theo phần Nhật ký chi tiết bên dưới. Không ghi log khởi động ứng dụng. Message ghi thông báo tổng quát theo mã HTTP.

Ghi log dùng scope database riêng, giới hạn chờ 5 giây. Nếu ghi database lỗi, ghi lỗi qua ILogger và không đổi kết quả nghiệp vụ đã trả. Nhật ký là best-effort, chưa có cơ chế lưu bền dự phòng hoặc tự xóa log theo thời gian. Mỗi lần đọc danh sách log cũng phát sinh một log cho lần đọc đó; Swagger và tải file tĩnh không được ghi.

## Cập nhật database cho upload và log

Migration UploadFilesAndSystemLogs bổ sung UploadedFile, SystemLog cùng các permission mới. Nếu database đã áp dụng InitialCreate hiện tại, cập nhật tiếp bằng:

```powershell
dotnet ef database update --project "K:\Ta Minh Khiet - Admin\Project\Base\Base\Base.csproj"
```

Nếu database còn dùng bộ migration cũ trước khi tạo lại InitialCreate, cần dùng database mới/trống theo hướng dẫn ở trên. Không xóa database hoặc sửa lịch sử migration tự động.
## Nhật ký chi tiết

Migration SystemLogDetails bổ sung Action, EntityType, EntityId và ba cột JSONB nullable vào SystemLog:

- ResponseData: dữ liệu data của response thành công, gồm hồ sơ tài khoản, danh sách tài khoản (đã phân trang), metadata upload. Đăng nhập chỉ lưu account/expiresAt, loại bỏ accessToken.
- BeforeData: bản chụp thông tin trước thay đổi hoặc trước khi xóa.
- AfterData: bản chụp thông tin sau thêm/cập nhật hoặc metadata file đã upload.

Action hiện có: Create, Update, Delete, Login, Upload. EntityType: User hoặc UploadedFile. Các snapshot tài khoản sử dụng UserResponse, tuyệt đối không truyền Account/PasswordHash hoặc request chứa mật khẩu vào AuditContext.

GET /api/system-logs/{id} trả ApiResponse<SystemLogDetailResponse>, yêu cầu admin và system-logs.read. Các trường responseData, beforeData, afterData được trả thành JSON object, không phải chuỗi JSON escape. Danh sách nhật ký chỉ thêm action/entityType/entityId, không trả các snapshot. Không ghi data của API đọc nhật ký để tránh lặp nhật ký bên trong nhật ký.

Chỉ phản hồi HTTP 2xx mới lưu dữ liệu chi tiết. Bản ghi cũ trước migration không có snapshot nên các trường này là null. Khi thêm module mới, gọi AuditContext.Begin/Complete với DTO đã loại thông tin nhạy cảm và mở rộng danh sách cho phép trong AuditResponseFilter. Cơ chế lưu log vẫn best-effort, tách transaction nghiệp vụ; không phải audit transactional bảo đảm tuyệt đối khi database ghi log gặp lỗi.

Áp dụng migration mới bằng lệnh database update như trên. Không thay đổi cấu trúc ApiResponse hiện có.
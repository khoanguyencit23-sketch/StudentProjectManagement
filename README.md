# Student Project Management — Base Project (CSW306)

Base solution cho hệ thống quản lý đồ án sinh viên, dùng ASP.NET Core Web API và Clean Architecture. Repository này cung cấp nền tảng kỹ thuật để 6 thành viên phát triển các module Đề tài, Tiến độ, Chấm điểm, Lịch bảo vệ và Thống kê.

## 1. Technology stack

- .NET 8 / C# 12
- ASP.NET Core Web API
- Entity Framework Core 8 + SQL Server
- JWT Bearer Authentication + refresh-token rotation
- BCrypt.Net-Next để hash mật khẩu
- Swagger/OpenAPI

> **Lưu ý vòng đời .NET:** .NET 8 và .NET 9 kết thúc hỗ trợ ngày 10/11/2026. Bản Base Project này target `net8.0` theo yêu cầu/khả năng tương thích của môn học. Nếu nhóm không bị ràng buộc bởi môi trường chấm bài, nên cân nhắc chuyển sang .NET 10 LTS trước khi triển khai lâu dài.

## 2. Solution structure

```text
StudentProjectManagement/
├── StudentProjectManagement.sln
├── README.md
├── .gitignore
├── .editorconfig
├── .env.example
└── src/
    ├── StudentProjectManagement.Domain/
    │   ├── Common/
    │   │   └── BaseEntity.cs
    │   ├── Entities/
    │   │   ├── User.cs
    │   │   ├── Role.cs
    │   │   └── UserRoleAssignment.cs
    │   ├── Enums/
    │   │   ├── UserRole.cs
    │   │   └── ProjectStatus.cs
    │   └── StudentProjectManagement.Domain.csproj
    ├── StudentProjectManagement.Application/
    │   ├── Common/
    │   │   ├── ApiResponse.cs
    │   │   └── Interfaces/ICurrentUserService.cs
    │   ├── Auth/
    │   │   ├── DTOs/
    │   │   ├── Interfaces/
    │   │   └── TokenResult.cs
    │   └── StudentProjectManagement.Application.csproj
    ├── StudentProjectManagement.Infrastructure/
    │   ├── Identity/
    │   │   ├── AuthService.cs
    │   │   ├── CurrentUserService.cs
    │   │   └── JwtTokenService.cs
    │   ├── Options/JwtOptions.cs
    │   ├── Persistence/
    │   │   ├── ApplicationDbContext.cs
    │   │   ├── DbSeeder.cs
    │   │   └── Migrations/
    │   ├── DependencyInjection.cs
    │   └── StudentProjectManagement.Infrastructure.csproj
    └── StudentProjectManagement.API/
        ├── Controllers/
        │   ├── BaseApiController.cs
        │   └── AuthController.cs
        ├── Middleware/GlobalExceptionMiddleware.cs
        ├── Properties/launchSettings.json
        ├── Program.cs
        ├── appsettings.json
        ├── appsettings.Development.json
        ├── StudentProjectManagement.API.http
        └── StudentProjectManagement.API.csproj
```

### 2.1 Dependency direction

```text
API ───────────────► Application
 │                       ▲
 │                       │
 ▼                       │
Infrastructure ──────────┘
 │
 ▼
Domain ◄──────────────── Application
```

Quy tắc phụ thuộc:

1. `Domain` không tham chiếu project nào khác.
2. `Application` chỉ tham chiếu `Domain`; khai báo DTO và interface/use-case contracts.
3. `Infrastructure` tham chiếu `Application` và `Domain`; triển khai EF Core, SQL Server, JWT, hashing.
4. `API` là composition root, kết nối dependency injection, HTTP pipeline và controllers.
5. Controller không truy cập `ApplicationDbContext` trực tiếp. Controller gọi interface thuộc Application.

## 3. Database model và quy ước nền tảng

### 3.1 User, Role, UserRoles

- `UserRole` là **enum** trong Domain, gồm `Admin`, `Lecturer`, `Student`.
- `Role` là entity/table lưu danh mục vai trò.
- `UserRoleAssignment` là entity bảng nối; DbSet có tên `UserRoles`.

Việc phân biệt tên này tránh xung đột giữa enum `UserRole` và entity bảng nối. Bảng `UserRoles` dùng khóa chính ghép `(UserId, RoleId)`.

### 3.2 BaseEntity và audit

Các entity kế thừa `BaseEntity` có `Guid Id`, `CreatedAt`, `CreatedBy`, `UpdatedAt`, `UpdatedBy`, `IsDeleted`.

`ApplicationDbContext` tự động cập nhật audit fields mỗi khi `SaveChanges`/`SaveChangesAsync` được gọi. Gọi `DbSet.Remove(entity)` sẽ chuyển thành soft delete (`IsDeleted = true`). Global query filter ẩn các bản ghi đã xóa khỏi truy vấn mặc định.

`CreatedBy`/`UpdatedBy` là `Guid?` vì thao tác seed hoặc tác vụ hệ thống không nhất thiết có người dùng đăng nhập.

## 4. Setup trên máy cá nhân

### 4.1 Prerequisites

Cài đặt:

- .NET 8 SDK
- Visual Studio 2022 (ASP.NET and web development workload) hoặc VS Code + C# Dev Kit
- SQL Server LocalDB / SQL Server Express / SQL Server Developer
- SQL Server Management Studio (tùy chọn)

Kiểm tra SDK:

```bash
dotnet --list-sdks
dotnet --info
```

### 4.2 Clone repository

```bash
git clone <repository-url>
cd StudentProjectManagement
git checkout dev
```

### 4.3 Configure connection string và JWT secret

Mở `src/StudentProjectManagement.API/appsettings.json`, chỉnh `ConnectionStrings:DefaultConnection` theo SQL Server trên máy.

Ví dụ SQL Server LocalDB:

```text
Server=(localdb)\MSSQLLocalDB;Database=StudentProjectManagementDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True
```

Ví dụ SQL Server Express:

```text
Server=localhost\SQLEXPRESS;Database=StudentProjectManagementDb;Trusted_Connection=True;TrustServerCertificate=True
```

Không commit mật khẩu database hoặc JWT secret thật. Nên dùng .NET User Secrets cho môi trường Development:

```bash
dotnet user-secrets init --project src/StudentProjectManagement.API
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<your-connection-string>" --project src/StudentProjectManagement.API
dotnet user-secrets set "Jwt:SecretKey" "<random-secret-at-least-32-utf8-bytes>" --project src/StudentProjectManagement.API
```

`appsettings.json` và `.env.example` chỉ chứa giá trị mẫu. **ASP.NET Core không tự động đọc file `.env`**. Nếu nhóm dùng `.env`, phải bổ sung thư viện dotenv và cấu hình nạp file có chủ đích; không được giả định rằng tạo `.env` là ứng dụng tự nhận biến.

### 4.4 Restore packages và build

```bash
dotnet restore StudentProjectManagement.sln
dotnet build StudentProjectManagement.sln
```

### 4.5 EF Core Migration

Migration `InitialCreate` đã được đặt trong Infrastructure. Tạo database/cập nhật schema bằng .NET CLI:

```bash
dotnet tool install --global dotnet-ef --version 8.0.32

dotnet ef database update \
  --project src/StudentProjectManagement.Infrastructure \
  --startup-project src/StudentProjectManagement.API \
  --context ApplicationDbContext
```

Nếu dùng Visual Studio Package Manager Console:

```powershell
Add-Migration InitialCreate -Project StudentProjectManagement.Infrastructure -StartupProject StudentProjectManagement.API -OutputDir Persistence/Migrations
Update-Database -Project StudentProjectManagement.Infrastructure -StartupProject StudentProjectManagement.API
```

> Nếu migration `InitialCreate` đã tồn tại trong source, không chạy `Add-Migration InitialCreate` lần nữa. Chỉ chạy `Update-Database`. Lệnh `Add-Migration` chỉ dành cho migration mới khi model thay đổi.

Từ các lần thay đổi schema tiếp theo, ví dụ thêm cột Topic:

```bash
dotnet ef migrations add AddTopicModule \
  --project src/StudentProjectManagement.Infrastructure \
  --startup-project src/StudentProjectManagement.API \
  --context ApplicationDbContext \
  --output-dir Persistence/Migrations

dotnet ef database update \
  --project src/StudentProjectManagement.Infrastructure \
  --startup-project src/StudentProjectManagement.API \
  --context ApplicationDbContext
```

Cấu hình mẫu để `Database:ApplyMigrationsOnStartup=false`: nhóm chủ động chạy `Update-Database` trước khi khởi động API. Có thể bật `true` cho môi trường Development muốn tự áp dụng migration lúc chạy. Với production, nên chạy migration qua deployment pipeline thay vì cho mọi API instance tự migrate đồng thời.

### 4.6 Run API

```bash
dotnet run --project src/StudentProjectManagement.API
```

Nếu máy chưa trust HTTPS development certificate, chạy `dotnet dev-certs https --trust`.

Mở Swagger:

```text
https://localhost:7180/swagger
```

Lần khởi động đầu tiên:

1. EF Core áp dụng migrations nếu `Database:ApplyMigrationsOnStartup=true`.
2. Seeder tạo 3 role nếu chưa tồn tại.
3. Trong môi trường `Development`, seeder tạo 6 tài khoản demo nếu chưa tồn tại.
4. Trong môi trường khác Development, seeder **không** tạo tài khoản/mật khẩu demo.

## 5. Development accounts

Chỉ dùng để chạy local ở môi trường `Development`:

| Role | Email | Password |
|---|---|---|
| Admin | `admin@spm.local` | `Admin@12345` |
| Lecturer | `lecturer1@spm.local` | `Lecturer@12345` |
| Lecturer | `lecturer2@spm.local` | `Lecturer@12345` |
| Student | `student1@spm.local` | `Student@12345` |
| Student | `student2@spm.local` | `Student@12345` |
| Student | `student3@spm.local` | `Student@12345` |

Mật khẩu được hash bằng BCrypt work factor 12 trước khi ghi DB. Không sử dụng tài khoản demo hoặc JWT secret mẫu trong môi trường thật.

## 6. Authentication endpoints

Base route: `/api/v1/auth`

| Method | Endpoint | Authentication | Mô tả |
|---|---|---|---|
| POST | `/login` | Không | Đăng nhập, nhận access + refresh token |
| POST | `/register` | Không | Đăng ký tài khoản Student |
| POST | `/refresh` | Không | Đổi refresh token lấy cặp token mới |
| POST | `/logout` | Bearer | Thu hồi refresh token hiện tại |

### 6.1 Login request

```json
{
  "email": "admin@spm.local",
  "password": "Admin@12345"
}
```

### 6.2 Register request

```json
{
  "fullName": "Nguyen Van A",
  "email": "student@example.com",
  "password": "Student@12345"
}
```

API register không nhận `Role` từ client. Tất cả tài khoản đăng ký công khai được gán `Student`. Admin chỉ cấp Lecturer/Admin thông qua quy trình quản trị riêng được phát triển sau.

### 6.3 Standard response

```json
{
  "success": true,
  "code": "SUCCESS",
  "message": "Login successful.",
  "data": {
    "userId": "00000000-0000-0000-0000-000000000000",
    "fullName": "System Admin",
    "email": "admin@spm.local",
    "role": "Admin",
    "accessToken": "<jwt-access-token>",
    "accessTokenExpiresAt": "2026-11-04T13:00:00+00:00",
    "refreshToken": "<opaque-refresh-token>",
    "refreshTokenExpiresAt": "2026-11-11T12:45:00+00:00"
  },
  "error": null,
  "timestamp": "2026-11-04T12:45:00+00:00"
}
```

Giá trị ví dụ trong JSON chỉ minh họa shape. Token thật và timestamp được tạo khi request chạy.

### 6.4 Swagger Bearer token

1. Gọi `/api/v1/auth/login`.
2. Copy giá trị `data.accessToken` trong response.
3. Nhấn **Authorize** ở Swagger UI.
4. Paste access token nguyên bản; không cần tự thêm tiền tố `Bearer`.
5. Gọi endpoint có `[Authorize]`.

## 7. Hướng dẫn Git dành cho 6 thành viên

### 7.1 Branch strategy

| Branch | Mục đích | Quy tắc |
|---|---|---|
| `main` | Phiên bản ổn định để demo/nộp bài | Chỉ merge qua Pull Request đã review |
| `dev` | Nhánh tích hợp chung | Tập hợp module đã hoàn thành và kiểm tra |
| `feature/<module-name>` | Công việc của từng thành viên | Tạo từ `dev`, chỉ chứa một module/nhóm thay đổi liên quan |

Tên branch ví dụ:

```text
feature/topic-management
feature/project-progress
feature/project-grading
feature/defense-schedule
feature/dashboard-statistics
feature/auth-improvements
```

### 7.2 Quy trình làm việc hằng ngày

```bash
# 1. Đồng bộ nhánh tích hợp
git checkout dev
git pull origin dev

# 2. Tạo branch riêng
git checkout -b feature/topic-management

# 3. Sau khi code xong
git add .
git commit -m "feat(topic): add topic CRUD endpoints"

# 4. Đẩy branch lên remote
git push -u origin feature/topic-management
```

Sau đó tạo Pull Request `feature/topic-management` → `dev`. Tech Lead review, kiểm tra build, migration, response format và conflict trước khi merge. Khi đủ chức năng và ổn định, Tech Lead tạo PR `dev` → `main`.

### 7.3 Commit convention

```text
feat(topic): add topic CRUD
fix(auth): handle expired refresh token
refactor(project): extract project service
chore(database): add initial migration
docs(readme): document local setup
```

### 7.4 Quy tắc tích hợp database

- Thành viên tạo migration trên branch của module mình.
- Tên migration mô tả thay đổi, ví dụ `AddTopicEntity`, `AddProjectMilestones`.
- Commit đồng thời entity/configuration, migration và thay đổi liên quan.
- Không sửa/xóa migration đã được merge lên `dev` nếu chưa trao đổi với Tech Lead.
- Khi pull `dev`, chạy `dotnet ef database update` để đồng bộ schema local.
- Không để hai thành viên cùng chỉnh sửa một migration hoặc `ApplicationDbContext` mà không phối hợp.

## 8. Quy chuẩn tạo module mới

Mỗi module nên tuân theo cách chia thư mục sau (ví dụ module Topic):

```text
Domain/
└── Entities/Topic.cs

Application/
└── Topics/
    ├── DTOs/
    ├── Interfaces/ITopicService.cs
    └── TopicServiceContracts.cs

Infrastructure/
├── Persistence/Configurations/TopicConfiguration.cs
└── Topics/TopicService.cs

API/
└── Controllers/TopicController.cs
```

- Entity và business invariant đặt trong Domain.
- DTO, service interface/use-case contract đặt trong Application.
- EF Core query, persistence và implementation đặt trong Infrastructure.
- Controller chỉ nhận request, gọi service và trả response.

### 8.1 Mẫu `TopicController`

Đoạn dưới là hợp đồng mẫu cho module Topic. `ITopicService`, `CreateTopicRequest` và `TopicDto` sẽ được thành viên module Topic tạo trong Application; không inject `ApplicationDbContext` vào controller.

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentProjectManagement.API.Controllers;
using StudentProjectManagement.Application.Common.Interfaces;
using StudentProjectManagement.Application.Topics.DTOs;
using StudentProjectManagement.Application.Topics.Interfaces;

namespace StudentProjectManagement.API.Controllers;

[Authorize]
public sealed class TopicController : BaseApiController
{
    private readonly ITopicService _topicService;

    public TopicController(
        ICurrentUserService currentUserService,
        ITopicService topicService) : base(currentUserService)
    {
        _topicService = topicService;
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateTopicRequest request,
        CancellationToken cancellationToken)
    {
        if (CurrentUser.UserId is not Guid userId)
        {
            return UnauthorizedResponse("User ID claim is missing or invalid.");
        }

        // Service chịu trách nhiệm kiểm tra nghiệp vụ và lưu DB.
        var topic = await _topicService.CreateAsync(request, userId, cancellationToken);

        return CreatedResponse(topic, "Topic created successfully.");
    }
}
```

Interface tương ứng trong Application có thể định nghĩa như sau:

```csharp
using StudentProjectManagement.Application.Topics.DTOs;

namespace StudentProjectManagement.Application.Topics.Interfaces;

public interface ITopicService
{
    Task<TopicDto> CreateAsync(
        CreateTopicRequest request,
        Guid createdBy,
        CancellationToken cancellationToken = default);
}
```

### 8.2 Checklist trước khi tạo Pull Request

- [ ] Controller kế thừa `BaseApiController`.
- [ ] Endpoint có `[Authorize]` nếu cần đăng nhập; dùng `[AllowAnonymous]` có chủ đích.
- [ ] Lấy user hiện tại qua `CurrentUser.UserId`, không tự nhận `CreatedBy` từ request body.
- [ ] Controller không gọi trực tiếp DbContext.
- [ ] Thành công trả `OkResponse`, `CreatedResponse` hoặc response helper tương ứng.
- [ ] Lỗi nghiệp vụ dùng exception phù hợp để GlobalExceptionMiddleware chuẩn hóa.
- [ ] DTO request có validation attributes phù hợp.
- [ ] Thêm migration nếu model/database thay đổi.
- [ ] `dotnet build StudentProjectManagement.sln` chạy thành công.
- [ ] Không commit connection string/mật khẩu/JWT secret thật.

## 9. API response và error handling

Tất cả response có cùng envelope:

```json
{
  "success": false,
  "code": "VALIDATION_ERROR",
  "message": "One or more validation errors occurred.",
  "data": null,
  "error": {
    "fieldName": ["Validation message"]
  },
  "timestamp": "2026-11-04T12:45:00+00:00"
}
```

Middleware mapping mặc định:

| Exception | HTTP | Code |
|---|---:|---|
| `ArgumentException` | 400 | `BAD_REQUEST` |
| `UnauthorizedAccessException` | 401 | `UNAUTHORIZED` |
| `KeyNotFoundException` | 404 | `NOT_FOUND` |
| `InvalidOperationException` | 409 | `CONFLICT` |
| Exception khác | 500 | `INTERNAL_SERVER_ERROR` |

Không trả stack trace hoặc nội dung exception nội bộ cho client ở môi trường thật. Dùng `traceId` để tra log.

## 10. Definition of Done — Base Project

Base Project được xem là sẵn sàng để nhóm phát triển tiếp khi các mục sau đã được xác nhận trên máy của Tech Lead:

- [ ] Restore và build solution thành công.
- [ ] SQL Server kết nối được.
- [ ] `InitialCreate` migration update database thành công.
- [ ] Seeder tạo 3 roles và 6 demo accounts trong Development.
- [ ] Login trả access token + refresh token.
- [ ] Register tạo tài khoản Student, password được BCrypt hash.
- [ ] Refresh token được rotate; token cũ không thể dùng lại.
- [ ] Logout thu hồi refresh token.
- [ ] Swagger hiển thị nút Authorize và gọi được endpoint bảo vệ.
- [ ] Các response lỗi và response thành công theo `ApiResponse<T>`.
- [ ] Thành viên khác clone nhánh `dev`, cấu hình local và chạy được project.

---

**Tech Lead ownership:** Thành viên 1 quản lý `main`, `dev`, package versions, migration integration, API response contract, authentication contract và review các Pull Request trước khi merge.

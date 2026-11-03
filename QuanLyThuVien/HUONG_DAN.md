# Quản lý thư viện – Hướng dẫn chạy & đối chiếu yêu cầu

## 1. Chạy project (làm đúng thứ tự)

1. Mở `QuanLyThuVien.slnx` bằng Visual Studio 2022+/2026 (.NET 10).
2. Mở **Package Manager Console** (Tools → NuGet Package Manager), chọn project `QuanLyThuVien` rồi chạy:
   ```
   Add-Migration UpdateBookBorrowAccount
   Update-Database
   ```
   (hoặc CLI: `dotnet ef migrations add UpdateBookBorrowAccount` rồi `dotnet ef database update`).
   > Bắt buộc: code đã thêm cột mới (Book.Description, Book.CoverImage, BorrowRecord.BookId/ContactPhone/DueDate, các cột đổi sang nullable). Nếu chưa tạo migration, EF sẽ báo lỗi "pending model changes" lúc khởi động.
3. Nhấn F5. Trang gốc `/` là giao diện **Khách**; trang quản trị: `/Auth/Login`.
4. Tài khoản mẫu tự tạo khi DB chưa có tài khoản nào: **admin / Admin@123** (đổi ngay trong menu Tài khoản).
   Tài khoản cũ có mật khẩu thô trong DB vẫn đăng nhập được và sẽ được tự động băm lại.

## 2. Hai loại người dùng

| | Khách (không đăng nhập) | Thủ thư | Admin |
|---|---|---|---|
| Xem/tra cứu sách, chi tiết sách | ✔ | ✔ | ✔ |
| Giỏ mượn (Session), gửi yêu cầu mượn | ✔ (chỉ tạo phiếu "Chờ duyệt") | | |
| CRUD sách, độc giả, mượn trả, phạt, báo cáo | ✘ | ✔ | ✔ |
| Duyệt/từ chối yêu cầu, nhận trả sách | ✘ | ✔ | ✔ |
| Quản lý tài khoản | ✘ | ✘ (403) | ✔ |

Mặc định **mọi trang đều yêu cầu đăng nhập** (global `AuthorizeFilter`); chỉ `KhachHang`, `Auth`, `GET /api/books*` mới `[AllowAnonymous]`.

## 3. Đối chiếu yêu cầu đề bài

| Yêu cầu | Đã làm ở đâu |
|---|---|
| 3.1 Hai loại người dùng, quyền khác nhau | Cookie Authentication + role (`Program.cs`, `AuthController`, `AppRoles`) |
| 3.2 Template + framework front-end | AdminLTE (quản trị), Bootstrap 5 + jQuery (Khách), Bootstrap Icons |
| 3.3 Paging, Filtering, AJAX | `PaginatedList<T>`, `_Pager.cshtml`, `wwwroot/js/list-ajax.js`; Sách, Độc giả, Mượn trả, Phạt, trang Khách. Thêm vào giỏ + gợi ý tìm kiếm bằng AJAX (`khach.js`) |
| Thêm/sửa/xóa dữ liệu cần thiết | Mọi controller; xóa bằng POST + anti-forgery |
| Session, cookies | Session: giỏ mượn. Cookie: đăng nhập, ngôn ngữ, "sách xem gần đây", `last_user` |
| Validation (Annotation) | Thuộc tính `[Required]`, `[StringLength]`, `[Range]`, `[RegularExpression]`, `[EmailAddress]` trong `Models/` + jQuery unobtrusive validation |
| Web API RESTful | `Controllers/Api/BooksApiController` (`/api/books`), `ReadersApiController` (`/api/readers`) |
| Upload file | Ảnh bìa sách: `Services/ImageUploadService` (kiểm tra đuôi, kích thước ≤ 2MB, magic bytes, tên file ngẫu nhiên) |
| Editor cho textarea | CKEditor 5 ở ô "Mô tả" (Sach/Create, Sach/Edit) |
| 3.4 Responsive | Bootstrap grid, navbar có nút thu gọn, bảng `table-responsive` |
| 3.5 Bảo mật | Mục 4 |
| 3.5 Quốc tế hóa | Mục 5 |
| Kiểm thử | Mục 6 |

### API nhanh
```
GET    /api/books?q=&category=&page=1&pageSize=10     (công khai)
GET    /api/books/{id}   |   GET /api/books/categories (công khai)
POST   /api/books        body JSON {title, author, category, quantity, description}   (cần đăng nhập)
PUT    /api/books/{id}   |   DELETE /api/books/{id}                                   (cần đăng nhập)
GET/POST/PUT/DELETE /api/readers                                                       (cần đăng nhập)
```
Mã trả về: 200/201/204, 400 (dữ liệu sai), 401 (chưa đăng nhập), 403 (không đủ quyền), 404.

## 4. Các biện pháp bảo mật (dùng cho phần báo cáo 3.5)

- **Mật khẩu**: băm PBKDF2 bằng `PasswordHasher`, không lưu mật khẩu thô; chính sách ≥ 8 ký tự có chữ + số.
- **Xác thực/Phân quyền**: cookie `HttpOnly`, `SameSite=Lax`, `Secure` khi chạy production; chặn theo role; API trả 401/403 thay vì chuyển hướng.
- **Brute-force/spam**: rate limiting (đăng nhập 5 lần/phút, gửi yêu cầu mượn 10 lần/phút); thông báo đăng nhập sai chung chung.
- **CSRF**: `[ValidateAntiForgeryToken]` cho mọi POST, token gửi qua header với AJAX; thao tác xóa đổi từ GET sang POST.
- **XSS**: Razor tự encode đầu ra; HTML từ editor được **sanitize** (thư viện HtmlSanitizer, danh sách thẻ cho phép) cả lúc lưu lẫn lúc hiển thị.
- **SQL Injection**: dùng EF Core/LINQ (truy vấn tham số hóa), không nối chuỗi SQL.
- **Over-posting**: Edit chỉ gán các trường cho phép (không `Update(model)` nguyên object); `CoverImage` không nhận từ client.
- **Upload an toàn**: whitelist đuôi file, giới hạn dung lượng, kiểm tra magic bytes, tên file do server sinh (chống path traversal/ghi đè).
- **Open Redirect**: `returnUrl` chỉ chấp nhận URL nội bộ (`Url.IsLocalUrl`).
- **Header**: `X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`, HSTS + HTTPS redirect.
- **Cookie do client gửi** (sách xem gần đây) luôn được parse an toàn.
- Hạn chế còn lại / hướng mở rộng: thêm CSP, khóa tài khoản sau N lần sai, xác thực 2 lớp, ghi log kiểm toán.

## 5. Quốc tế hóa (i18n)

- Hai ngôn ngữ **Việt (mặc định) / Anh** cho giao diện Khách, chọn bằng nút VI | EN trên thanh menu; lựa chọn lưu bằng cookie culture.
- Cơ chế: `IStringLocalizer<SharedResource>` + `Resources/SharedResource.en.resx` (khóa chính là câu tiếng Việt, nên không cần file `vi` riêng). Thông báo lỗi validation cũng được dịch (`AddDataAnnotationsLocalization`).
- Thêm ngôn ngữ mới: thêm `SharedResource.<mã>.resx` và khai báo culture trong `Program.cs`.
- Giao diện quản trị hiện để tiếng Việt.

## 6. Kịch bản kiểm thử thủ công (điền kết quả vào báo cáo)

| # | Chức năng | Các bước | Kết quả mong đợi |
|---|---|---|---|
| 1 | Đăng nhập đúng | `/Auth/Login`, admin / Admin@123 | Vào Dashboard, menu hiện tên + nút Đăng xuất |
| 2 | Đăng nhập sai | Sai mật khẩu | Báo lỗi chung; sai > 5 lần/phút → 429 |
| 3 | Chưa đăng nhập | Vào `/Sach`, `/api/readers` | Chuyển sang trang đăng nhập / API trả 401 |
| 4 | Phân quyền | Thủ thư vào `/TaiKhoan` | 403 Access Denied |
| 5 | Thêm sách + validation | Để trống tên, số lượng -1 | Báo lỗi ngay trên form (client + server) |
| 6 | Upload ảnh | Chọn file `.exe` đổi tên `.jpg` / ảnh > 2MB | Bị từ chối; ảnh hợp lệ hiển thị ở danh sách |
| 7 | Editor + XSS | Mô tả nhập `<script>alert(1)</script>` | Script bị loại bỏ khi lưu và khi xem chi tiết |
| 8 | Phân trang/lọc/AJAX | Thêm > 8 sách, gõ từ khóa, đổi thể loại, bấm trang | Danh sách đổi không reload trang |
| 9 | Khách thêm giỏ | Bấm "Thêm vào giỏ mượn" | Badge giỏ tăng, không reload; thêm trùng báo lỗi; > 5 cuốn bị chặn |
| 10 | Khách gửi yêu cầu | Nhập tên + SĐT sai/đúng | Sai: báo lỗi; đúng: tạo phiếu "Chờ duyệt", giỏ trống |
| 11 | Duyệt yêu cầu | Admin bấm ✓ | Trạng thái "Đang mượn", kho giảm 1; sách hết thì không duyệt được |
| 12 | Trả sách | Bấm "Trả" | "Đã trả", kho tăng 1; phiếu quá hạn tô đỏ |
| 13 | Xóa | Xóa sách/độc giả | Có hộp xác nhận; gõ URL `/Sach/Delete/1` bằng GET không xóa được |
| 14 | API | `GET /api/books?q=a`, `POST` khi chưa đăng nhập | 200 / 401 |
| 15 | Ngôn ngữ | Bấm EN | Giao diện Khách chuyển tiếng Anh, giữ nguyên trang |
| 16 | Responsive | Thu nhỏ trình duyệt < 576px | Menu thu gọn, lưới sách 1 cột, bảng cuộn ngang |

## 7. Lưu ý
- Ảnh upload nằm ở `wwwroot/uploads/books/` (nên thêm vào `.gitignore` nếu không muốn đẩy ảnh lên Git).
- Editor CKEditor và Bootstrap Icons tải từ CDN → cần Internet khi chạy.
- Trong môi trường mình soạn code không có .NET SDK nên **chưa build thử**; nếu Visual Studio báo lỗi biên dịch, gửi lại nội dung lỗi để sửa tiếp.

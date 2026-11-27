# Frontend

Ứng dụng React TypeScript sử dụng Vite, Tailwind CSS và shadcn/ui.
Giao diện sử dụng tông xanh dương theo trang đăng nhập; màu chủ đạo là
`#243d9a` và xanh navy `#101c43`.

```powershell
cd Frontend
npm install
npm run dev
```

Mở địa chỉ Vite hiển thị trong terminal. Thêm component shadcn/ui từ thư mục
`Frontend`, ví dụ:

```powershell
npx shadcn@latest add card
```

Các component được tạo trong `src/components/ui` và có thể import bằng alias
`@`, ví dụ `import { Button } from '@/components/ui/button'`.

## Cấu trúc frontend

- `src/pages`: giao diện các trang và biểu mẫu.
- `src/routes`: định tuyến và điều kiện truy cập.
- `src/services`: xử lý HTTP, xác thực và các API CRM.
- `src/utils`: các hàm tiện ích dùng chung.
- `src/types`: các kiểu dữ liệu dùng chung.
- `src/components/ui`: component shadcn/ui.

## Đăng nhập

Chạy backend với profile `http` tại `http://localhost:5005` trước khi đăng nhập.
Vite chuyển tiếp các yêu cầu `/api` đến backend trong môi trường phát triển.
Khi triển khai, cấu hình máy chủ chuyển tiếp `/api` đến backend tương ứng.
Trang giới thiệu nằm ở `/`, đăng nhập tại `/login`, đăng ký tại `/signup` và
workspace tại `/dashboard`. Khi vào workspace, tài khoản chưa thuộc workspace nào
sẽ tự động có một workspace cá nhân; các workspace hiện có vẫn được giữ nguyên.
Các trang được đặt trong `src/pages`; điều kiện truy cập nằm trong `src/routes`.
Access token và refresh token được backend đặt trong cookie HttpOnly,
SameSite=Strict; frontend không lưu token trong localStorage hoặc sessionStorage.
Khi tải lại trang hoặc access token hết hạn, frontend kiểm tra phiên và làm mới
token bằng cookie. Yêu cầu thay đổi dữ liệu dùng header `X-CRM-Request: 1` để
chống CSRF; triển khai frontend và `/api` cùng origin, không cho phép CORS tùy ý.
Cookie yêu cầu HTTPS ngoài môi trường Development. Backend hiện dùng token
stateless: đăng xuất xóa cookie nhưng token đã cấp vẫn có hiệu lực đến khi hết hạn.

Dashboard hiển thị số liệu workspace từ API. Khách hàng, công ty, quy trình và
cơ hội hỗ trợ tạo, xem, sửa, xóa; quy trình hỗ trợ quản lý và sắp xếp giai đoạn.
Lộ trình hiển thị các giai đoạn theo thứ tự; chọn một giai đoạn để xem và chuyển
cơ hội, thay đổi được lưu qua API. Lịch và
công việc hỗ trợ xem và tạo, chưa có giao diện sửa/xóa. Trang thành viên chỉ
hiển thị danh sách: backend chưa hỗ trợ CRUD tài khoản nhân viên toàn hệ thống.

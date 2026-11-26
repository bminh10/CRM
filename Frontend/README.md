# Frontend

Ứng dụng React TypeScript sử dụng Vite, Tailwind CSS và shadcn/ui.

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

## Đăng nhập

Chạy backend với profile `http` tại `http://localhost:5005` trước khi đăng nhập.
Vite chuyển tiếp các yêu cầu `/api` đến backend trong môi trường phát triển.
Khi triển khai, cấu hình máy chủ chuyển tiếp `/api` đến backend tương ứng.
Trang đăng nhập nằm ở `/login`, trang sau đăng nhập ở `/dashboard`.
Các trang được đặt trong `src/pages`; điều kiện truy cập nằm trong `src/routes`.
Phiên đăng nhập chỉ được giữ trong bộ nhớ của trang, sẽ mất khi tải lại hoặc
khi access token hết hạn. Đăng xuất xóa phiên trên trang; token đã cấp vẫn có
hiệu lực ở backend cho đến khi hết hạn.

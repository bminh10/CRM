# Frontend

Ứng dụng React JavaScript sử dụng Vite, Tailwind CSS và shadcn/ui.

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

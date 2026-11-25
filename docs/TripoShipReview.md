# Đánh giá FBX Tripo — 2026-11-06

Nguồn: `C:/Users/nguye/Downloads/tripo_mesh_uv_51d2b153-bf66-480a-abd6-0c6d107d44ef.fbx`.

Bản sao trong project: `Assets/_Project/Art/Spacecraft/TripoShip/tripo_mesh_uv_51d2b153-bf66-480a-abd6-0c6d107d44ef.fbx`.

## Kết quả kiểm tra

- Unity 6000.3.28f1 import thành công; không cần cài Blender hoặc thêm package.
- FBX binary 7400, 310.236 byte; 1 mesh, 1 material URP/Lit.
- 9.393 tam giác; 5.380 vị trí đỉnh trong FBX, 8.950 đỉnh sau Unity import (UV/normal seams có thể khiến đỉnh được tách).
- Có UV0, normals và tangents. Không có texture nhúng hoặc tham chiếu texture trong FBX; material hiện không có texture.
- Bounds sau import: khoảng 0,5835 × 0,6401 × 0,9995 mét; tâm bounds ở gốc. Hướng nhập có rotation X=270 độ. Cần đặt tỉ lệ và pivot theo thiết kế gameplay thực tế.
- Không có skeleton, animation hoặc collider. Mesh hiện tắt Read/Write theo mặc định import.
- Hai ảnh render Unity cho thấy nhiều tên lửa cùng cụm linh kiện tách tầng nằm trong một mesh, kèm một số mảnh/đường nối mảnh. Không phải một tàu đơn đã được chuẩn bị để tháo lắp.

## Có thể sử dụng thế nào?

Dùng để tham khảo hình dáng, trưng bày và prototype. Chưa nên đưa trực tiếp làm asset tàu chính hoặc bộ module lắp ráp. Trước khi dùng chính thức cần chọn riêng một tàu, loại mảnh thừa, kiểm tra bề mặt kín/normals, thêm texture PBR, xác định tỉ lệ/pivot, collider đơn giản và các điểm kết nối. Số tam giác hiện tại phù hợp để thử trên PC; hiệu năng thực tế còn phụ thuộc số bản sao, vật liệu, ánh sáng và thiết bị.

Nếu còn dự án Tripo gốc, nên xuất từng vật thể riêng và tải kèm texture. Có thể xem và cấu hình vật liệu/collider trong Unity mà không cần Blender. Tách geometry rời có thể làm bằng công cụ Editor, nhưng không đảm bảo các phần AI tạo ra có đường phân chia thích hợp cho gameplay. File FBX không chứng minh quyền thương mại; giữ thông tin gói/license của lần tạo từ Tripo.

## Bằng chứng và trạng thái project

- `Logs/fbx-inspection.json`: cấu trúc FBX đọc trực tiếp.
- `Logs/fbx-unity-inspection.json`: kết quả import, thống kê và bounds từ Unity.
- `Logs/tripo-ship-preview-1.png`, `Logs/tripo-ship-preview-2.png`: render mesh thực bằng material xám tạm, không phải texture gốc.
- Không sửa scene gameplay hoặc model gốc trong Downloads. Scene demo vẫn không dirty; asset FBX đã được chọn trong Project window để xem Inspector.
- Sau import/render, Console có 0 error, 1 warning từ Unity AI Account API không kết nối được trong 30 giây; không có compile error.
- Đã khôi phục và lưu `EditorSettings.asyncShaderCompilation=true`; Play Mode đang tắt; Run In Background đã trả về thiết lập gốc, không còn override.
- Cập nhật công cụ Unity CLI chính thức từ 1.0.0-beta.16 lên beta.17 theo hướng dẫn skill; không cài Blender, Tripo hoặc package mới vào project.

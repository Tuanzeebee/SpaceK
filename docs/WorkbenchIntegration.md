# Bàn chế tạo Tripo trong Unity — 2026-11-06

Đã thay hình bàn placeholder bằng model từ `tripo_pbr_model_c2c45001-d21d-4660-a508-8f9a82da387d_meshopt.glb` do người dùng cung cấp. Model gốc trong Downloads được giữ nguyên.

## Asset và scene

- Prefab hiện tại: `Assets/_Project/Prefabs/Interactables/Workbench.prefab`. Giữ GUID, CraftingStation và 3 recipe để các tham chiếu scene/HUD tiếp tục hoạt động.
- Bản dự phòng: `Assets/_Project/Prefabs/Interactables/Workbench_Placeholder.prefab`.
- Mesh, material và texture: `Assets/_Project/Art/Workbench/`.
- Scene đã lưu: `Assets/_Project/Scenes/SpacecraftSimulator_Test.unity`, vị trí bàn `(-4, 0, -3)`, ngăn kéo hướng về phía người chơi tiếp cận từ cửa spawn.
- Model được chuyển thành Mesh asset và material URP/Lit gốc Unity. Gameplay không phụ thuộc GLB importer, bộ giải mã, đường dẫn Downloads hoặc Logs. Không thêm package vào manifest.
- Generator `FoundationSceneBuilder.Workbench` tái sử dụng prefab đã có để bảo toàn model khi tạo lại scene bị thiếu.

## Tối ưu và chuyển đổi

- Meshoptimizer từ repository gốc `zeux/meshoptimizer`, license MIT, dùng riêng cho bước chuyển đổi. License và công cụ nằm trong `Logs/WorkbenchGLB/`.
- Mesh gốc 1.948.936 tam giác / 1.016.291 đỉnh được giảm xuống **60.000 tam giác / 42.055 đỉnh**, giảm khoảng **96,9%** tam giác. Giữ UV, normals và bảo vệ seams; chỉ số sai số appearance do simplifier báo là 0,000464. Đã xem ảnh thực trong Game view; đây không phải kiểm định hiệu năng hoặc chứng nhận chất lượng mesh cho sản phẩm cuối.
- Chuyển hệ trục bằng phản chiếu X, đảo winding và UV V; tính lại tangents. Normal map được import đúng loại và đảo kênh xanh tương ứng với việc đảo UV V.
- Texture màu dùng sRGB; texture dữ liệu dùng linear. Đóng gói metallic (R), occlusion (G), smoothness (A = 1 − roughness) cho URP. Giữ nguồn texture 4K, giới hạn import ở 2K với mipmaps và compression.
- Kích thước tổng sau đặt: khoảng **2,397 × 1,469 × 0,978 m**, gồm thanh ngang phía trên; đáy bounds nằm đúng y=0. Mặt bàn gần y=0,96 m.
- 8 BoxCollider đơn giản cho hai tủ, mặt bàn, khung trên và gối đỡ; không dùng mesh hàng triệu tam giác làm collider.

## Kiểm tra

- Biên dịch cuối: completed, failed=false, errors=[]; compilationFailed=false.
- 4/4 EditMode SceneReferenceTests đạt sau lần sửa cuối.
- 2/2 PlayMode đạt: tương tác pickup/door/workbench qua input E, và nhấn recipe tạo output sau thời gian chế tạo. Kiểm thử tương tác dùng collider thật thay vì tên child Display của placeholder.
- Kiểm tra trực tiếp Game view 1600×900: bàn đứng trên sàn, hiển thị material/texture và prompt `[E] Use Fabrication workbench`. Đã quan sát frame đang tăng.
- Console cuối: 0 error; compilationFailed=false, compiling=false; còn 1 warning trong Editor Console.
- Play Mode đã tắt, scene không dirty, asyncShaderCompilation=true, Run In Background đã trả về thiết lập gốc, không còn override.
- Bằng chứng: `Logs/WorkbenchGLB/editmode-tests.json`, `playmode-tests.json`, `optimization.json`, `workbench-in-game.png`. Bản scene trước thay model ở `Logs/WorkbenchGLB/SpacecraftSimulator_Test.before-workbench.unity`.

Ngăn kéo và gối đỡ vẫn là một mesh tĩnh. Không thêm animation ngăn kéo, đèn hoạt động, LOD hoặc tính năng chế tạo mới; chức năng chế tạo hiện có được giữ nguyên. Chưa thử standalone build hoặc mobile.

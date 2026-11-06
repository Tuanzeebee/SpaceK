# Sáu model Tripo trong Unity — 2026-11-06

Đã nhập asset Unity gốc và thay các mẫu tạm trong scene `Assets/_Project/Scenes/SpacecraftSimulator_Test.unity`. Không thêm package, không cần Blender. GLB gốc trong Downloads không bị chỉnh sửa.

## Nguồn và tối ưu

`Hull Module.glb` đầu tiên chứa model bàn chế tạo c2c45001, nên đã loại khỏi bước nhập. Thân ống được lấy từ **Hull Module_1.glb**, model e7a6b3aa. Năm file còn lại dùng tên do người dùng gửi.

| Model | Tam giác gốc | LOD0 gần | LOD1 | LOD2 xa | Giảm LOD0 |
|---|---:|---:|---:|---:|---:|
| Hull Module | 1.944.018 | 22.917 | 6.849 | 4.593 | 98,82% |
| Nose Cone | 1.936.734 | 15.526 | 4.298 | 2.630 | 99,20% |
| Engine Module | 1.916.100 | 30.000 | 8.816 | 5.536 | 98,43% |
| Stabilizer Fin | 1.943.784 | 10.416 | 3.210 | 2.078 | 99,46% |
| Fuel Tank | 1.960.865 | 16.000 | 4.462 | 2.510 | 99,18% |
| Material Crate | 1.895.682 | 28.890 | 8.148 | 5.172 | 98,48% |

Mỗi file chứa một mesh, một material và ba texture PBR 4K. Giải mã EXT_meshopt_compression bằng công cụ MIT meshoptimizer đã dùng cho bàn chế tạo; không tải thêm DLL/executable. Giảm lưới có trọng số cho normals/UV, bảo vệ UV seams và dùng Regularize. Sau so sánh render bản gốc/bản giảm, tăng ngân sách LOD0 so với thử nghiệm đầu và tái tạo normals/tangents để loại phản sáng dạng tam giác ở các mép. Sai số simplifier là chỉ số nội bộ, không phải chứng nhận chất lượng hoặc giới hạn sai số tính bằng mét.

Chuyển hệ tọa độ X/winding/UV V sang Unity. Texture màu sRGB; normal/dữ liệu linear. URP packing: R=metallic, G=occlusion, A=1-roughness. Import texture tối đa 2K, mipmaps, compression, trilinear. Nguồn 4K vẫn nằm trong Art để chỉnh lại nếu cần.

## File và tham chiếu

- `Assets/_Project/Art/ShipModules/<model>/`: LOD0/1/2 Mesh assets, PBR.mat, BaseColor.jpg, NormalGL.png, MetalOcclusionSmoothness.png.
- `Assets/_Project/Prefabs/Modules/Hull Module.prefab` và `Thruster Module.prefab`: thay hình, giữ GUID. Engine giữ tên file cũ để bảo toàn tham chiếu, tên hiển thị là Engine Module.
- `Prefabs/Modules/Nose Cone.prefab`, `Stabilizer Fin.prefab`, `Fuel Tank.prefab`: prefab mới.
- `Prefabs/Environment/Material Crate.prefab`: thùng môi trường mới. Bốn thùng primitive trong hangar đã thay.
- `Prefabs/Interactables/Pickup-alloy.prefab`, `Pickup-circuit.prefab`: thay hình bằng crate, giữ PickupInteractable, số lượng và ItemData. Collider/scale/độ cao đã chỉnh theo model.
- `Prefabs/Interactables/Workbench.prefab`: giữ bàn Tripo và GUID, mở rộng từ 3 lên 6 recipe.
- `Prefabs/Player/Engineer.prefab` và scene: 6 lựa chọn xây dựng, gồm 5 module Tripo và cockpit cũ. Thêm vật mẫu đầu chóp/cánh/bình vào cargo khởi đầu của demo.
- `ScriptableObjects/Items`, `Recipes`, `Modules`: dữ liệu mới cho nose-cone, stabilizer-fin và fuel-tank; cập nhật size hull/engine; icon item lấy từ render model.
- `Scripts/Building/BuildingController.cs`: ghost chỉ sao chép LOD0, không chồng cả ba mức LOD và không chạy script prefab.
- `Editor/FoundationSceneBuilder.cs`: giữ dữ liệu/module/pickup/crate nhập sẵn khi tạo một scene demo bị thiếu; vẫn từ chối ghi đè scene đã tồn tại.
- Tests: `ImportedModuleTests.cs`, `SceneReferenceTests.cs`, `SimulatorPlayModeTests.cs`.

Các prefab dùng pivot giữa bounds, root scale 1, BoxCollider và LODGroup. Kích thước X×Y×Z lần lượt: hull 1,2×2×1,2 m; nose 1,2×1,1×1,2; engine 1,2×1,3×1,2; fin 0,14×0,9×0,65; tank 0,95×1,5×0,95; crate 0,8×0,6×0,6. Visual scale được chuẩn hóa riêng theo từng trục; không khẳng định Tripo đã tạo đúng tỷ lệ này từ nguồn. Collider hộp đơn giản xem phần rỗng như khối kín.

Scene có một tên lửa trưng bày ghép từ hull/nose/engine và 4 fin ở bên trái bàn tại x=-8,5; z=-2, thêm tank bên cạnh bàn. Nhóm `Module Art Display` tách khỏi `Spacecraft Assembly` để không tính vào module người chơi đã lắp. Một reflection probe 128 realtime OnAwake cung cấp phản xạ môi trường cho kim loại. Đây là trưng bày tĩnh; chưa có lực đẩy, nhiên liệu, socket snapping hoặc mô phỏng cấu trúc.

Dùng B vào Building, Q chuyển 6 mẫu, R xoay, chuột trái đặt, chuột phải hủy; E nhặt hai thùng vật liệu, E/C tại bàn để chế tạo, I/Tab xem inventory. Các mẫu mới có recipe và cost tương ứng.

## Kiểm chứng

- EditMode: 27/27 đạt, gồm 6 kiểm tra LOD giảm dần, material/texture, pivot và collider bounds.
- Kiểm tra ghost trước sửa tái hiện 3 mesh thay vì 1; sau sửa đạt.
- PlayMode: 12/12 đạt, gồm WASD/V, pickup/door/workbench, crafting, collider, ghost chỉ dùng một LOD và đặt lần lượt cả 5 module Tripo với cost đúng. Báo cáo: Logs/ModuleModels/playmode-tests.json; EditMode summary: editmode-summary.json.
- Công cụ/ảnh/chỉ số và backup trước thay: `Logs/ModuleModels/`; bản ghi nguồn và SHA256 ở reports.json. Runtime không phụ thuộc Logs hoặc Downloads.

Chưa kiểm thử standalone/mobile hoặc đo FPS trên phần cứng mục tiêu. Mesh đã giảm vẫn có thể cần retopology, bake lại normal map và chỉnh hard edges nếu dùng cho hình cận cảnh sản phẩm cuối.


- Game view 1600×900: Logs/ModuleModels/models-in-game.png; inventory icon/content: new-modules-inventory.png. FrameCount đã tăng 6518→6529, framesObserved=2 trong 137ms.
- Console cuối: 0 errors, compilationFailed=false, compiling=false. Còn 1 warning (2 lần trong buffer) do Unity AI Account API không truy cập được sau 30 giây; không phải lỗi model/gameplay.
- Handoff: Play Mode off, scene đã lưu/không dirty, asyncShaderCompilation giữ true, Run In Background trở về false và không có override. Không chỉnh pipeline URP hoặc cài thêm package.

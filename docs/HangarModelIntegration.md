# Sàn, tường và inspection terminal — 2026-11-06

Đã chuyển ba GLB người dùng cung cấp thành Mesh/Material/Prefab Unity gốc, thay hình trong `Assets/_Project/Scenes/SpacecraftSimulator_Test.unity`. Không cài package, không cần Blender, không sửa GLB nguồn.

| Model | Tam giác gốc | LOD0 | LOD1 | LOD2 |
|---|---:|---:|---:|---:|
| Wall Panel | 1.964.568 | 3.052 | 1.654 | 1.354 |
| Floor Tile | 1.943.356 | 1.644 | 746 | 626 |
| Inspection Terminal | 1.948.554 | 15.999 | 3.999 | 2.619 |

Meshoptimizer MIT đã có từ đợt nhập trước dùng để giải nén EXT_meshopt_compression và giản lược có trọng số normal/UV, bảo vệ UV seams. Tái tạo normals/tangents sau giảm lưới. Texture nguồn 4K, import giới hạn 2K, mipmaps và compression; PBR URP/Lit, metallic/occlusion/smoothness được đóng gói đúng kênh như các module trước.

## Asset và scene

- `Assets/_Project/Art/HangarModels/{Wall Panel,Floor Tile,Inspection Terminal}/`: texture, material, mesh gốc đã tối ưu và mesh chuẩn hóa 3 LOD.
- `Assets/_Project/Prefabs/Environment/Wall Panel.prefab`: kích thước 2×3×0,15 m, pivot chân tường, BoxCollider.
- `Assets/_Project/Prefabs/Environment/Floor Tile.prefab`: kích thước 2×0,12×2 m, pivot mặt trên y=0, BoxCollider.
- `Assets/_Project/Prefabs/Interactables/Terminal.prefab`: giữ GUID, TerminalInteractable và Status; thay model kích thước 0,65×1,55×0,5 m, pivot chân, BoxCollider và 3 LOD. Vị trí trong scene (4,0,-6). E tiếp tục mở bảng terminal.
- `Assets/_Project/Prefabs/Environment/Hangar Surfaces.prefab`: 320 tile lát sàn 32×40 m, 160 panel phủ bốn tường ngoài và hai bên/header của airlock. Khoảng mở cửa được giữ nguyên.
- Các mesh cụm dùng chung nằm ngay trong `Art/HangarModels`. 20 cụm sàn và 21 cụm tường, mỗi cụm một renderer hoạt động cho một mức LOD; tránh tạo 480 renderer tile riêng lẻ. Ba LOD không vẽ cùng lúc. Tường ngoài cao 8 m dùng hai tầng panel kéo cao 4 m; tường airlock/header được chuẩn hóa theo kích thước khung cũ.
- Renderer Deck, bốn tường và ba khối khung airlock cũ được ẩn; BoxCollider cũ giữ nguyên để bảo toàn nền/vách vật lý. Deck Seam cũ được ẩn. Assembly Bay, lưới xây dựng, cửa, dầm/cột, ánh sáng, bàn, tàu mẫu và module trước đó được giữ lại.
- `FoundationSceneBuilder.cs` tái sử dụng terminal và hangar surfaces có sẵn khi tạo scene bị thiếu; không ghi đè scene đang tồn tại.
- Test prefab mở rộng cho cả ba model. PlayMode test terminal kiểm tra raycast + phím E thật thay vì gọi Interact trực tiếp.

Model/mesh từng tấm đã giảm 99,18–99,92%; tổng lượng tam giác render thực tế còn phụ thuộc số tấm nhìn thấy và LOD. Không lấy tỷ lệ giảm một tile để tuyên bố mức giảm toàn scene hoặc FPS.

Backup scene/terminal trước thay, report nguồn + SHA256, script QA và ảnh nằm trong `Logs/HangarModels/`. Runtime không phụ thuộc thư mục Logs hoặc Downloads. Collision dùng mặt phẳng/hộp đơn giản nên không phản ánh các rãnh nhỏ của mesh. Chưa thử standalone/mobile hoặc đo FPS trên máy mục tiêu.

## Kết quả kiểm thử
- Compile hoàn tất, failed=false, errors=[].
- 30/30 EditMode tests đạt, gồm cả 9 prefab model tối ưu và tham chiếu scene.
- 12/12 PlayMode tests đạt; terminal được kiểm tra bằng camera raycast và phím E thực, cùng di chuyển, camera, pickup, crafting và building.
- Xác nhận bố trí 41 cụm/3 LOD, sàn phủ 32×40 m với mặt trên y=0; vỏ collider cũ hoạt động, các renderer placeholder được ẩn.
- Ảnh Game view: Logs/HangarModels/hangar-in-game.png và terminal-in-game.png. Terminal đã xoay mặt màn hình về hướng âm Z để người chơi tiếp cận từ spawn.
- Lỗi kiểm tra ban đầu được xác định là phép đo Collider.bounds trên prefab không thuộc physics scene; test dùng center đã biến đổi từ dữ liệu BoxCollider thay thế. Prefab có center/size đúng, không thay collider để làm test qua.

- Sau chỉnh hướng màn hình: kiểm tra terminal bằng E đạt lại 1/1 (terminal-recheck.json).
- Phiên xác minh cuối với PlayerCameraRig bật bình thường: frame 14021→14036, 2 frame quan sát/103 ms; Console buffer và Editor đều 0 error/0 warning, compilationFailed=false. Ảnh hangar cuối chụp từ phiên này.
- Buffer QA trước đó có IndexOutOfRangeException ở InputAction.activeControl/PlayerInputReader sau chuỗi test/đổi thiết bị. Đã lưu qa-historical-errors.json. Sau refresh/domain reload và Play mới, bật lại camera bình thường và theo dõi frame/Console không tái phát. Chưa thay đổi Input System hoặc code runtime để che exception; nếu tái diễn khi hot reload/test thiết bị cần điều tra riêng.
- Handoff: Play Mode off, scene không dirty; Run In Background trả false không còn override, asyncShaderCompilation vẫn true. Không thêm package hoặc đổi render pipeline.

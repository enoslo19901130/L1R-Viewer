# Layer4 跨區塊渲染問題修復計劃

## 問題描述

S32 的 Layer4 物件座標可以超出該區塊的範圍（X > 127 或 Y > 63），這些物件應該繪製到相鄰的 S32 區域。目前每個 S32 獨立渲染，溢出的部分會被裁切。

### 數據分析（map/4）

```
80087fff.s32: X=0~145, Y=0~69, 溢出物件=454
80088000.s32: X=2~142, Y=0~72, 溢出物件=758
80097fff.s32: X=0~159, Y=0~79, 溢出物件=2649
80098000.s32: X=0~139, Y=0~71, 溢出物件=739
...
```

幾乎所有 S32 都有大量溢出物件。

### 座標系統

```
S32 區塊座標範圍：
  Layer1/2: X=0~127, Y=0~63 (格子座標)
  Layer4:   X=0~127, Y=0~63 (正常範圍)
            X>127 或 Y>63  (溢出，應繪製到相鄰區塊)

像素座標計算：
  halfX = X / 2
  baseX = -24 * halfX
  baseY = 63 * 12 - 12 * halfX
  pixelX = baseX + X * 24 + Y * 24
  pixelY = baseY + Y * 12

區塊像素大小：3072 x 1536
```

### 溢出方向

```
       Y-1 (上方區塊)
          ↑
X-1 ←  [當前]  → X+1
          ↓
       Y+1 (下方區塊)

溢出判斷：
  X >= 128 → 繪製到右邊區塊 (blockX + 1)
  Y >= 64  → 繪製到下方區塊 (blockY + 1)
  X >= 128 且 Y >= 64 → 繪製到右下區塊
```

---

## 方案比較

### 方案 A：渲染時查詢相鄰區塊（推薦）

**概念**：渲染一個區塊時，也繪製相鄰區塊中溢出到本區塊的物件

```
渲染 S32(x, y) 時：
1. 繪製本區塊的 Layer1, Layer2, Layer4
2. 查詢 S32(x-1, y) 的 Layer4，繪製 X >= 128 的物件
3. 查詢 S32(x, y-1) 的 Layer4，繪製 Y >= 64 的物件
4. 查詢 S32(x-1, y-1) 的 Layer4，繪製 X >= 128 且 Y >= 64 的物件
```

**優點**：
- 不修改原始資料
- 邏輯清晰
- 可以只在需要時啟用

**缺點**：
- 每次渲染需要查詢多個 S32
- 需要處理 Layer 排序（跨區塊物件的 Layer 順序）

---

### 方案 B：建立溢出物件索引

**概念**：載入時預先建立「哪些物件會溢出到哪個區塊」的索引

```csharp
// 溢出索引：目標區塊 → 來源物件列表
Dictionary<(int blockX, int blockY), List<(S32Data source, ObjectTile obj)>> _overflowIndex;

// 載入時建立
foreach (var s32 in allS32Files)
{
    foreach (var obj in s32.Layer4)
    {
        if (obj.X >= 128 || obj.Y >= 64)
        {
            var targetBlock = CalculateTargetBlock(s32, obj);
            _overflowIndex[targetBlock].Add((s32, obj));
        }
    }
}

// 渲染時使用
void RenderS32Block(S32Data s32)
{
    // 1. 繪製本區塊
    DrawLocalLayers(s32);

    // 2. 繪製溢出到此區塊的物件
    var key = (s32.SegInfo.nBlockX, s32.SegInfo.nBlockY);
    if (_overflowIndex.TryGetValue(key, out var overflows))
    {
        foreach (var (source, obj) in overflows.OrderBy(o => o.obj.Layer))
        {
            DrawOverflowObject(s32, source, obj);
        }
    }
}
```

**優點**：
- 渲染時查詢快速（O(1)）
- 只需建立一次索引

**缺點**：
- 需要額外記憶體
- 載入時間增加
- 需要處理動態修改（編輯模式）

---

### 方案 C：修正座標到正確區塊

**概念**：載入時將溢出物件移動到正確的 S32

```csharp
// 載入後處理
foreach (var s32 in allS32Files)
{
    var toRemove = new List<ObjectTile>();

    foreach (var obj in s32.Layer4)
    {
        if (obj.X >= 128 || obj.Y >= 64)
        {
            var targetS32 = FindTargetS32(s32, obj);
            if (targetS32 != null)
            {
                // 轉換座標
                var newObj = new ObjectTile
                {
                    X = obj.X - 128,  // 或 obj.X % 128
                    Y = obj.Y - 64,   // 或 obj.Y % 64
                    // ... 其他屬性
                };
                targetS32.Layer4.Add(newObj);
                toRemove.Add(obj);
            }
        }
    }

    foreach (var obj in toRemove)
        s32.Layer4.Remove(obj);
}
```

**優點**：
- 渲染邏輯不需修改
- 資料結構乾淨

**缺點**：
- 修改原始資料結構
- 儲存時需要還原
- Layer 順序可能錯亂
- 可能有物件跨多個區塊（X=200 跨兩個區塊）

---

## 推薦方案：A + B 混合

### 實作步驟

#### Step 1：建立溢出物件索引

```csharp
// 在 MapForm.cs 新增
private Dictionary<(int, int), List<(S32Data, ObjectTile)>> _layer4OverflowIndex;

private void BuildLayer4OverflowIndex()
{
    _layer4OverflowIndex = new Dictionary<(int, int), List<(S32Data, ObjectTile)>>();

    foreach (var s32 in _document.S32Files.Values)
    {
        int srcBlockX = s32.SegInfo.nBlockX;
        int srcBlockY = s32.SegInfo.nBlockY;

        foreach (var obj in s32.Layer4)
        {
            // 計算溢出方向
            int deltaX = obj.X / 128;  // 0 或 1+
            int deltaY = obj.Y / 64;   // 0 或 1+

            if (deltaX > 0 || deltaY > 0)
            {
                var targetKey = (srcBlockX + deltaX, srcBlockY + deltaY);
                if (!_layer4OverflowIndex.ContainsKey(targetKey))
                    _layer4OverflowIndex[targetKey] = new List<(S32Data, ObjectTile)>();

                _layer4OverflowIndex[targetKey].Add((s32, obj));
            }
        }
    }
}
```

#### Step 2：修改 RenderS32Block

```csharp
private Bitmap RenderS32Block(S32Data s32Data, bool showLayer1, bool showLayer2, bool showLayer4)
{
    // ... 現有的 Layer1, Layer2 繪製 ...

    if (showLayer4)
    {
        // 1. 收集本區塊物件
        var allObjects = new List<(ObjectTile obj, int pixelX, int pixelY, bool isOverflow)>();

        foreach (var obj in s32Data.Layer4)
        {
            var (px, py) = CalculatePixelPosition(obj.X, obj.Y);
            allObjects.Add((obj, px, py, false));
        }

        // 2. 收集溢出到此區塊的物件
        var key = (s32Data.SegInfo.nBlockX, s32Data.SegInfo.nBlockY);
        if (_layer4OverflowIndex.TryGetValue(key, out var overflows))
        {
            foreach (var (srcS32, obj) in overflows)
            {
                // 計算相對於此區塊的像素位置
                int relX = obj.X - 128;  // 轉換到本區塊座標
                int relY = obj.Y - 64;
                var (px, py) = CalculatePixelPosition(relX, relY);
                allObjects.Add((obj, px, py, true));
            }
        }

        // 3. 按 Layer 排序後繪製
        foreach (var item in allObjects.OrderBy(o => o.obj.Layer))
        {
            DrawTilToBufferDirect(item.pixelX, item.pixelY, item.obj.TileId,
                                  item.obj.IndexId, rowpix, ptr, blockWidth, blockHeight);
        }
    }
}
```

#### Step 3：處理座標轉換

```csharp
// 溢出物件的座標轉換
private (int relX, int relY) ConvertOverflowCoordinates(
    S32Data srcS32, S32Data dstS32, ObjectTile obj)
{
    int deltaBlockX = dstS32.SegInfo.nBlockX - srcS32.SegInfo.nBlockX;
    int deltaBlockY = dstS32.SegInfo.nBlockY - srcS32.SegInfo.nBlockY;

    // 每個區塊是 128 格 (X) 和 64 格 (Y)
    int relX = obj.X - deltaBlockX * 128;
    int relY = obj.Y - deltaBlockY * 64;

    return (relX, relY);
}
```

---

## 測試計劃

### CLI 測試指令

```bash
# 現有指令 - 顯示相鄰區塊
render-adjacent <map_path> <gameX> <gameY>

# 新增指令 - 比較修復前後
render-adjacent <map_path> <gameX> <gameY> --fix-overflow
```

### 測試案例

#### 測試點 1：房子渲染問題
```
地圖路徑: C:\workspaces\lineage\v381\client_paktest\map\4
格子座標: (80, 7)
遊戲座標: (33384, 32775)
中心區塊: 80098000.s32 (BlockX=0x8009, BlockY=0x8000)

相關區塊（2x2）：
  - 80097fff.s32 (上方) - 15655 個 L4 物件，2649 個溢出
  - 80098000.s32 (中心) - 14161 個 L4 物件，739 個溢出
  - 80087fff.s32 (左上) - 6083 個 L4 物件，454 個溢出
  - 80088000.s32 (左邊) - 6773 個 L4 物件，758 個溢出

問題描述：
  該位置應該有一個房子，但因為 Layer4 物件的 Layer 排序問題，
  高 Layer 的物件被低 Layer 的物件覆蓋。
```

#### 其他測試案例

1. **邊界物件**：找一個 X=130 的物件，確認它繪製在正確的位置
2. **跨三區塊物件**：找一個 X=260 的物件（跨兩個區塊）
3. **Layer 排序**：確認跨區塊物件的 Layer 順序正確

---

## 待確認

1. **物件最大溢出範圍**：X 最大到多少？Y 最大到多少？
2. **是否需要處理負數座標**：X < 0 或 Y < 0 的情況？
3. **編輯模式**：修改物件後，索引需要更新嗎？
4. **效能影響**：索引建立時間、額外記憶體使用

---

## 檔案修改清單

| 檔案 | 修改內容 |
|------|----------|
| `MapForm.cs` | 新增 `_layer4OverflowIndex`、修改 `RenderS32Block` |
| `Models/MapDocument.cs` | 可能需要在載入時建立索引 |
| `CLI/Commands/BenchmarkCommands.cs` | 新增 `--fix-overflow` 測試選項 |

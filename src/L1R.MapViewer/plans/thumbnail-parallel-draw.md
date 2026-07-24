# 縮圖平行繪製優化方案

## 現況分析

### 效能數據
```
generateThumbnails = 8033ms（優化後反而變慢）
原本 = 2800ms
```

### 目前架構
```
860 groups ──Parallel.ForEach──→ 每個 group 產生一個 thumbnail
                                      │
                                      ▼
                              foreach object in group
                                  （按 Layer 順序，序列）
                                      │
                                      ▼
                              DrawTilToBufferDirect()
                                  （逐像素寫入 ptr）
```

### 瓶頸
- 860 個 group 已平行
- 每個 group 內平均 5400 個 objects（4.6M / 860）
- 每個 object 呼叫一次 `DrawTilToBufferDirect`
- 逐像素寫入 `byte* ptr`

---

## 核心問題

**可以平行寫 `byte* ptr` 嗎？**

```csharp
byte* ptr = (byte*)bmpData.Scan0;
// 每個像素地址 = ptr + y * stride + x * 2
```

**答案：可以，但有條件**

| 情況 | 能否平行 | 原因 |
|------|---------|------|
| 不同像素 | ✅ 可以 | 不同記憶體位置，無競爭 |
| 同一像素 | ❌ 不行 | 寫入順序會影響結果 |

**問題**：同一個 thumbnail 裡，tiles 會重疊（Layer 堆疊）

```
Layer 0: Tile A 畫在 (10, 10)
Layer 1: Tile B 畫在 (12, 11)  ← 與 A 重疊
Layer 2: Tile C 畫在 (11, 10)  ← 與 A, B 重疊
```

必須按 Layer 順序畫，否則結果錯誤。

---

## 優化方案

### 方案 A：接受不精確（最簡單）

**概念**：縮圖只是預覽，不需要完美的 Layer 順序

```csharp
// 原本：序列
foreach (var item in objectPixels.OrderBy(o => o.obj.Layer))
{
    DrawTil(...);
}

// 改成：平行
Parallel.ForEach(objectPixels, item =>
{
    DrawTil(...);  // 可能有競爭，但視覺上影響不大
});
```

**優點**：改動最小
**缺點**：可能有輕微視覺差異

---

### 方案 B：分 Layer 批次處理

**概念**：同一 Layer 內的 tiles 不重疊，可以平行

```csharp
var groupedByLayer = objectPixels.GroupBy(o => o.obj.Layer).OrderBy(g => g.Key);

foreach (var layerGroup in groupedByLayer)
{
    // 同一 Layer 內平行繪製
    Parallel.ForEach(layerGroup, item =>
    {
        DrawTil(...);
    });
    // 等這層畫完再畫下一層
}
```

**優點**：保持 Layer 順序正確
**缺點**：平行度受限於每層物件數

---

### 方案 C：預渲染 Tile 成 Bitmap

**概念**：用 GDI+ 的 `Graphics.DrawImage` 取代手動像素複製

```csharp
// 1. 預渲染每個 tile 成 48x48 Bitmap（帶透明）
Bitmap tileBitmap = RenderTileToBitmap(tileId, indexId);
_tileBitmapCache[key] = tileBitmap;

// 2. 用 Graphics.DrawImage 合成
using (Graphics g = Graphics.FromImage(tempBitmap))
{
    foreach (var item in objectPixels.OrderBy(o => o.obj.Layer))
    {
        g.DrawImage(tileBitmapCache[key], pixelX, pixelY);
    }
}
```

**優點**：
- `DrawImage` 是 GDI+ 原生函數，可能有硬體加速
- 不需要手動處理像素
- 透明度混合自動處理

**缺點**：
- 更多記憶體（每個 tile 一個 Bitmap）
- 跨格式轉換可能有開銷

---

### 方案 D：回歸原版 + 減少物件數

**概念**：縮圖不需要畫全部物件

```csharp
// 每個 group 最多畫 N 個物件（取樣）
var sampled = objectPixels
    .OrderBy(o => o.obj.Layer)
    .Take(100)  // 只畫前 100 個
    .ToList();

foreach (var item in sampled)
{
    DrawTilToBufferDirect(...);
}
```

**優點**：大幅減少繪製量
**缺點**：縮圖可能不完整

---

### 方案 E：完全不畫 Tile（極端）

**概念**：縮圖只顯示邊界框

```csharp
using (Graphics g = Graphics.FromImage(result))
{
    g.Clear(Color.White);
    g.DrawRectangle(Pens.Gray, boundingBox);
    g.DrawString($"G{groupId}", font, brush, center);
}
```

**優點**：超快（< 1ms）
**缺點**：沒有視覺預覽

---

## 建議

| 優先級 | 方案 | 預估效果 |
|--------|------|----------|
| 1 | 方案 A（接受不精確） | 8s → 1-2s |
| 2 | 方案 B（分 Layer 平行） | 8s → 2-3s |
| 3 | 方案 D（減少物件數） | 8s → 0.5s |
| 4 | 方案 C（預渲染 Bitmap） | 需測試 |

---

## 待決定

1. 縮圖是否需要精確的 Layer 順序？
2. 可接受的最大時間是多少？
3. 記憶體限制？

# 縮圖產生效能優化方案

## 問題分析

目前 `generateThumbnails` 花費 ~2800ms 產生 860 個群組縮圖。

**瓶頸分解**:
| 階段 | 時間 | 說明 |
|------|------|------|
| Bitmap 操作 | ~360ms | 建立、填充、縮放 bitmap |
| Tile 繪製 | ~2400ms | `DrawTilToBufferDirect` 查找和複製像素 |

Tile 繪製佔 **86%** 的時間。

---

## 優化方案

### 方案 1: 延遲產生 (Lazy Generation)

**概念**: 只在縮圖滾動到可見區域時才產生

**優點**:
- 初始載入快（0ms）
- 只產生需要看到的縮圖
- 使用者體驗：立即可操作

**缺點**:
- 滾動時會有短暫延遲
- 實作較複雜（需要監聽 ListView 滾動事件）

**實作方式**:
```csharp
// 1. 初始只建立空白項目（帶 placeholder 圖）
foreach (var group in allGroups)
{
    var item = new ListViewItem { Tag = groupId, ImageIndex = 0 }; // placeholder
    lvGroupThumbnails.Items.Add(item);
}

// 2. 監聽滾動事件，產生可見範圍的縮圖
private void lvGroupThumbnails_Scroll(object sender, ScrollEventArgs e)
{
    var visibleItems = GetVisibleItems();
    foreach (var item in visibleItems)
    {
        if (!HasThumbnail(item))
            GenerateThumbnailAsync(item);
    }
}
```

---

### 方案 2: 快取縮圖 (Thumbnail Cache)

**概念**: 產生過的縮圖儲存到記憶體/磁碟，下次直接使用

**優點**:
- 第二次載入快（從快取讀取）
- 相同群組不重複產生

**缺點**:
- 第一次還是慢
- 需要管理快取大小
- 群組變更時需要 invalidate

**實作方式**:
```csharp
// 使用 Dictionary 快取
private Dictionary<int, Bitmap> _thumbnailCache = new();

private Bitmap GetOrGenerateThumbnail(int groupId, List<ObjectTile> objects)
{
    if (_thumbnailCache.TryGetValue(groupId, out var cached))
        return cached;

    var thumbnail = GenerateGroupThumbnail(objects, 80);
    _thumbnailCache[groupId] = thumbnail;
    return thumbnail;
}
```

---

### 方案 3: 降低品質 (Reduced Quality)

**概念**: 減少繪製的物件數量或縮小暫存 bitmap

**優點**:
- 簡單直接
- 保持現有架構

**缺點**:
- 縮圖品質下降

**實作方式**:
```csharp
// 每個群組最多繪製 N 個物件
var objectsToRender = objects.Take(5).ToList();

// 或縮小暫存 bitmap
int maxTempSize = 256; // 原本 512
```

---

### 方案 4: 完全停用 (Disable)

**概念**: 不產生縮圖，只顯示群組 ID

**優點**:
- 最快（0ms）
- 最簡單

**缺點**:
- 失去視覺預覽功能

**實作方式**:
```csharp
// 直接用文字取代圖片
foreach (var group in allGroups)
{
    var item = new ListViewItem($"G{group.Key} ({group.Value.Count})");
    lvGroupThumbnails.Items.Add(item);
}
```

---

### 方案 5: 混合方案 (Recommended)

**概念**: 結合延遲產生 + 快取

1. 初始載入：只顯示 placeholder
2. 可見時：產生並快取
3. 再次可見：從快取讀取

**預期效果**:
- 初始載入: 0ms → ~50ms（只建立項目）
- 滾動時：每個可見縮圖 ~3ms
- 重複瀏覽：從快取 ~0ms

---

## 建議

如果縮圖功能重要 → **方案 5 (混合方案)**
如果縮圖功能不常用 → **方案 4 (停用)** 或加個開關

請告訴我你想用哪個方案。

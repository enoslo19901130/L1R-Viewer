# Tile 解碼快取優化方案

## 問題分析

`DrawTilToBufferDirect` 每次呼叫都要：
1. 從 `_tilFileCache` 取得 til 資料 ✓ (已快取)
2. 讀取 type byte
3. **解壓縮** - 根據 type 解析壓縮格式
4. **逐像素寫入** - 每個像素都要邊界檢查、計算地址、兩次 byte 寫入

步驟 3-4 每次都重複執行，即使是同一個 tile。

**目標**：把解壓縮結果快取起來，繪製時用批次複製取代逐像素寫入。

---

## 現行架構

```
til 檔案 → _tilFileCache (byte[]) → DrawTilToBufferDirect → 解壓 → 逐像素寫入 ptr
                 ↑ 已快取                                    ↑ 每次都做
```

## 新架構

```
til 檔案 → _tilFileCache (byte[]) → _decodedTileCache (DecodedTile) → 批次複製到 ptr
                 ↑ 已快取                    ↑ 新快取                      ↑ 快速
```

---

## 資料結構

```csharp
/// <summary>
/// 預解碼的 Tile 資料
/// </summary>
class DecodedTile
{
    /// <summary>
    /// 像素資料 (RGB555 格式)
    /// 大小固定為 48x48 = 2304 ushorts，未使用的位置為 0
    /// </summary>
    public ushort[] Pixels;

    /// <summary>
    /// 透明遮罩 (true = 有像素, false = 透明)
    /// </summary>
    public bool[] Mask;

    /// <summary>
    /// 是否需要與背景混合 (type 34/35)
    /// </summary>
    public bool NeedsBlend;

    /// <summary>
    /// 繪製偏移 X (壓縮格式的 x_offset)
    /// </summary>
    public int OffsetX;

    /// <summary>
    /// 繪製偏移 Y (壓縮格式的 y_offset)
    /// </summary>
    public int OffsetY;

    /// <summary>
    /// 實際寬度 (用於優化複製範圍)
    /// </summary>
    public int Width;

    /// <summary>
    /// 實際高度 (用於優化複製範圍)
    /// </summary>
    public int Height;

    /// <summary>
    /// 每行的起始X和像素數 (用於稀疏格式優化)
    /// </summary>
    public (int startX, int count)[] RowInfo;
}
```

---

## 快取結構

```csharp
// Key = (tileId << 16) | indexId，合併成一個 long 作為 key
private ConcurrentDictionary<long, DecodedTile> _decodedTileCache = new();
```

---

## 新方法實作

### 1. 解碼方法（只執行一次）

```csharp
private DecodedTile DecodeTile(int tileId, int indexId)
{
    // 從 til 快取取得原始資料
    var tilArray = _tilFileCache.GetOrAdd(tileId, ...);
    if (tilArray == null || indexId >= tilArray.Count) return null;

    byte[] tilData = tilArray[indexId];
    var decoded = new DecodedTile
    {
        Pixels = new ushort[48 * 48],
        Mask = new bool[48 * 48],
        RowInfo = new (int, int)[48]
    };

    // 解析 type 並解壓縮到 Pixels 陣列
    byte type = tilData[0];
    decoded.NeedsBlend = (type == 34 || type == 35);

    // ... 根據 type 填充 Pixels 和 Mask ...

    return decoded;
}
```

### 2. 快取繪製方法

```csharp
private unsafe void DrawTilCachableToBufferDirect(
    int pixelX, int pixelY,
    int tileId, int indexId,
    int rowpix, byte* ptr,
    int maxWidth, int maxHeight)
{
    // 取得或建立快取
    long key = ((long)tileId << 16) | (uint)indexId;
    var decoded = _decodedTileCache.GetOrAdd(key, _ => DecodeTile(tileId, indexId));
    if (decoded == null) return;

    // 計算繪製範圍
    int drawX = pixelX + decoded.OffsetX;
    int drawY = pixelY + decoded.OffsetY;

    // 批次複製每一行
    for (int ty = 0; ty < decoded.Height; ty++)
    {
        int destY = drawY + ty;
        if (destY < 0 || destY >= maxHeight) continue;

        var (startX, count) = decoded.RowInfo[ty];
        int destX = drawX + startX;

        // 計算有效複製範圍（處理邊界裁切）
        int srcStart = 0;
        if (destX < 0) { srcStart = -destX; destX = 0; }
        int copyCount = Math.Min(count - srcStart, maxWidth - destX);
        if (copyCount <= 0) continue;

        // 來源位置
        int srcOffset = ty * 48 + startX + srcStart;

        if (decoded.NeedsBlend)
        {
            // 需要混合：逐像素處理（無法批次）
            for (int i = 0; i < copyCount; i++)
            {
                if (!decoded.Mask[srcOffset + i]) continue;
                int v = destY * rowpix + (destX + i) * 2;
                ushort srcColor = decoded.Pixels[srcOffset + i];
                ushort dstColor = (ushort)(*(ptr + v) | (*(ptr + v + 1) << 8));
                ushort blended = (ushort)(dstColor + 0xffff - srcColor);
                *(ptr + v) = (byte)(blended & 0xFF);
                *(ptr + v + 1) = (byte)(blended >> 8);
            }
        }
        else
        {
            // 不需混合：批次複製
            int destOffset = destY * rowpix + destX * 2;
            fixed (ushort* srcPtr = &decoded.Pixels[srcOffset])
            {
                // 使用 Mask 過濾透明像素
                for (int i = 0; i < copyCount; i++)
                {
                    if (decoded.Mask[srcOffset + i])
                    {
                        *(ushort*)(ptr + destOffset + i * 2) = srcPtr[i];
                    }
                }
            }
        }
    }
}
```

---

## 使用位置

### 縮圖產生 (GenerateGroupThumbnail)

```csharp
// 改用快取版本
DrawTilCachableToBufferDirect(pixelX, pixelY, obj.TileId, obj.IndexId, rowpix, ptr, tempWidth, tempHeight);
```

### 主畫布渲染 (RenderS32Block) - 可選

如果主畫布也想用，可以替換。但主畫布通常每個 tile 只繪製一次，快取效益較低。

---

## 預期效果

| 項目 | 原本 | 優化後 |
|------|------|--------|
| 第一次解碼 | 每次都做 | 只做一次 |
| 繪製方式 | 逐像素 | 批次行複製 |
| 邊界檢查 | 每像素 | 每行 |
| 記憶體 | 無額外 | +48x48x3 bytes/tile |

**縮圖產生預估**：
- 860 群組 × 平均 5 物件 = 4300 次 tile 繪製
- 假設 500 個 unique tiles
- 第一次: 500 次解碼 + 4300 次快速複製
- 第二次之後: 4300 次快速複製

預估 **2400ms → 300-500ms**

---

## 記憶體估算

每個 DecodedTile:
- Pixels: 48 × 48 × 2 bytes = 4,608 bytes
- Mask: 48 × 48 × 1 byte = 2,304 bytes
- RowInfo: 48 × 8 bytes = 384 bytes
- 其他欄位: ~50 bytes
- **總計: ~7.3 KB/tile**

假設快取 2000 個 tiles: **~14.6 MB**

可接受，且可以設定 LRU 上限。

---

## 實作步驟

1. 新增 `DecodedTile` 類別
2. 新增 `_decodedTileCache` 字典
3. 實作 `DecodeTile()` 方法
4. 實作 `DrawTilCachableToBufferDirect()` 方法
5. 修改 `GenerateGroupThumbnail` 使用新方法
6. 測試效能

---

## 進階優化（可選）

### A. SIMD 加速
使用 `Vector<ushort>` 一次處理 8-16 個像素。

### B. 預熱快取
在 `BuildLayer4SpatialIndex` 後，背景預解碼常用 tiles。

### C. LRU 淘汰
限制快取大小，淘汰最少使用的 tiles。

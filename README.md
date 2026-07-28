# md2pdf

把 Markdown 轉成可直接列印的 PDF（或樣式化 HTML）的跨平台命令列工具。

專為「把 `docs/` 下的進度表、報告轉成開會用文件」而做，重點在**中文字型**與**表格跨頁重複表頭**。

轉換路徑：

```
Markdown →（Markdig）→ 內嵌樣式的獨立 HTML →（系統 Chrome / Edge 無頭模式）→ PDF
```

工具**不自帶瀏覽器核心**，PDF 由目標機器上已安裝的 Chromium 系瀏覽器列印。換得極小的產物體積，
代價是目標機器必須裝有 Chrome / Edge / Chromium / Brave 其中一種。

---

## 安裝

### 下載發佈版

到 [Releases](https://github.com/jeff377/md2pdf/releases) 取回對應平台的壓縮檔，
支援 `osx-arm64`、`osx-x64`、`win-x64`、`linux-x64`：

```bash
tar -xzf md2pdf-<版本>-<平台>.tar.gz
chmod +x md2pdf && mv md2pdf /usr/local/bin/
```

每個平台各有兩種：

| 檔名 | 目標機器需求 | 大小 |
|------|------------|------|
| `md2pdf-<版本>-<平台>` | **只需瀏覽器** | 約 12 MB |
| `md2pdf-<版本>-<平台>-slim` | 瀏覽器 **＋ .NET 10 執行階段** | 約 650 KB |

不確定就選**沒有 `-slim`** 的版本，兩者功能完全相同。

> **macOS 使用者**：本專案未經 Apple 簽章與公證，從瀏覽器下載的檔案會被標記隔離，
> 執行時出現「無法驗證開發者」。請先解除隔離標記：
> ```bash
> xattr -d com.apple.quarantine /usr/local/bin/md2pdf
> ```

### 自行建置

建置需要 .NET 10 SDK：

```bash
git clone https://github.com/jeff377/md2pdf.git && cd md2pdf && ./publish.sh
```

`publish.sh` 預設產出四個平台的單一執行檔至 `publish/<rid>/`。
預設模式為 **framework-dependent**——執行檔**不含 .NET 組件**，單一檔僅約 650 KB，
但目標機器須裝有 .NET 10 執行階段。

```bash
./publish.sh                            # 預設四個 RID
./publish.sh osx-arm64                  # 只發佈指定 RID（可給多個）
./publish.sh --self-contained           # 自含 .NET 執行階段，目標機不需裝 .NET
./publish.sh --version 0.2.0 --archive  # 帶入版號並打包（Release 流程用的形式）
```

推送 `v*` 格式的 tag 會觸發 [release.yml](.github/workflows/release.yml)，
自動建置八個資產與 `SHA256SUMS.txt` 並建立 GitHub Release。

---

## 用法

```bash
md2pdf <input.md> [選項]
```

| 選項 | 說明 |
|------|------|
| `-o`, `--output <path>` | 輸出檔路徑（預設：與來源同名、換副檔名） |
| `--to <pdf\|html>` | 輸出格式（預設：由 `--output` 副檔名推斷，再無則 `pdf`） |
| `--title <text>` | 文件標題（預設：內文第一個 H1，再無則檔名） |
| `--css <path>` | 自訂樣式表，**完全取代**內建樣式 |
| `--landscape` | 以橫向紙張排版（寬表格適用） |
| `--open` | 完成後以系統預設程式開啟輸出檔 |
| `--browser <path>` | 指定 Chrome / Edge 執行檔（預設：自動搜尋） |
| `-h`, `--help` | 顯示說明 |
| `-v`, `--version` | 顯示版本 |

### 範例

```bash
md2pdf report.md                                  # 產生 report.pdf
md2pdf report.md -o ~/Downloads/報告.pdf --open   # 指定輸出並開啟
md2pdf report.md --to html -o preview.html        # 只產生樣式化 HTML（不需瀏覽器）
md2pdf 變更紀錄.md --landscape                     # 寬表格用橫向紙張
```

### 瀏覽器搜尋順序

1. `--browser` 指定的路徑
2. 環境變數 `MD2PDF_BROWSER`
3. 各平台預設安裝位置（Chrome → Edge → Chromium → Brave）
4. `PATH` 上的 `google-chrome` / `chromium` / `microsoft-edge` 等命令

### 離開碼

| 碼 | 意義 |
|----|------|
| 0 | 成功 |
| 1 | 未給任何引數（已印出說明） |
| 2 | 參數錯誤 |
| 3 | 找不到來源檔或自訂樣式表 |
| 4 | 找不到可用的瀏覽器 |
| 5 | 瀏覽器列印失敗 |
| 6 | 檔案存取失敗或權限不足 |

---

## 已知限制

- **目標機器需安裝瀏覽器**：PDF 由系統上的 Chrome / Edge / Chromium / Brave 以無頭模式列印。
  這是刻意的取捨——不自帶瀏覽器核心，產物才能小到幾百 KB。
  只用 `--to html` 時則完全不需要瀏覽器。
- **不做頁碼**：Chrome 命令列只能「全開」或「全關」預設的頁首頁尾，全開會連檔案 URL、
  系統日期一起印在紙上。兩者相權，v1 選擇全關，因此輸出的 PDF **沒有頁碼**。
- **正文的軟換行不強制斷行**：遵循標準 Markdown 語意，段落內的單一換行會被合併成同一段、
  由版面自動折行。**唯一例外是文件開頭的資訊卡**（用途 / 單位 / 人力 / 更新日期那一區），
  該區每行都是獨立欄位，故視為硬斷行。若正文某處需要強制斷行，請用 Markdown 的行尾兩個空白。
- **`--css` 是完全取代而非疊加**：指定自訂樣式表後，內建樣式（含中文字型設定、表格跨頁
  重複表頭）**不會**保留，需自行補齊。

---

## 授權

[MIT](LICENSE) © jeff377

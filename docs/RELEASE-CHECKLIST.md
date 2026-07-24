# Release checklist

1. [ ] `dotnet build L1R-Viewer.slnx -c Release` → 0 errors  
2. [ ] `dotnet test tests\L1R.Shared.Tests -c Release`  
3. [ ] `$env:L1R_CLIENT=...; .\tests\regression.ps1`  
4. [ ] Shell opens: `.\Launch-L1R-Viewer.ps1`  
5. [ ] doctor bad path fails; good path ok  
6. [ ] MapViewer: load map → 匯出 PNG / 傳送點  
7. [ ] PakBrowser: search `167` → export to default folder  
8. [ ] `python mcp\smoke_test.py` (if client set)  
9. [ ] MCP tool list has **no** write tools  
10. [ ] README 進度表與 CHANGELOG 已更新  
11. [ ] Tag optional: `git tag v1.1.0 && git push origin v1.1.0`  

## Portable folder (optional)

```powershell
dotnet publish src\L1R.Shell\L1R.Shell.csproj -c Release -o dist\shell
dotnet publish src\L1R.MapViewer\L1MapViewerCore.csproj -c Release -o dist\map
dotnet publish src\L1R.PakBrowser\PakViewer.csproj -c Release -o dist\pak
dotnet publish src\L1R.Cli\PakViewer.Cli.csproj -c Release -o dist\cli
```

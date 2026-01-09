# 1. 下载依赖（只做一次，除非换电脑或删了缓存）

dotnet restore

# 2. 编译代码（每次改代码后都要做）

dotnet build

# 3. 运行（内部会自动调用 build，所以其实可以跳过第 2 步！）

dotnet run --project yt-dlp-gui

# 4. 打包（每次发布新版本时都要做）

dotnet publish --project yt-dlp-gui

using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using yt_dlp_gui;
using yt_dlp_gui.Wrappers;

namespace Libs {
    public static class DependencyManager {
        private static readonly HttpClient _httpClient = new HttpClient();
        private static readonly string _toolsDir = Path.Combine(App.AppPath, "tools");

        public static async Task EnsureDependencies() {
            try {
                if (!Directory.Exists(_toolsDir)) {
                    Directory.CreateDirectory(_toolsDir);
                }

                Debug.WriteLine($"[DependencyManager] Checking tools in {_toolsDir}...", "DependencyManager");

                // 1. ffmpeg
                await EnsureFFmpeg();

                // 2. yt-dlp
                await EnsureYtDlp();

                // 3. JS Runtime (Deno, etc.)
                await EnsureJSRuntime();

            } catch (Exception ex) {
                Debug.WriteLine($"[DependencyManager] Error: {ex.Message}", "DependencyManager");
            }
        }

        private static async Task EnsureFFmpeg() {
            string ffmpegExe = "ffmpeg.exe";
            string localPath = Path.Combine(_toolsDir, ffmpegExe);
            
            // Check finding in local folder first
            if (CheckFileExistsRecursively(_toolsDir, ffmpegExe, out string foundPath)) {
                Debug.WriteLine($"[DependencyManager] ffmpeg found at {foundPath}", "DependencyManager");
                FFMPEG.Path_FFMPEG = foundPath;
                DLP.Path_FFMPEG = foundPath;
                return;
            }

            // Check system PATH
            string systemPath = FindInPath(ffmpegExe);
            if (!string.IsNullOrEmpty(systemPath)) {
                Debug.WriteLine($"[DependencyManager] ffmpeg found in PATH at {systemPath}", "DependencyManager");
                FFMPEG.Path_FFMPEG = systemPath;
                DLP.Path_FFMPEG = systemPath;
                return;
            }

            // Download
            Debug.WriteLine("[DependencyManager] ffmpeg not found. Downloading...", "DependencyManager");
            string downloadUrl = "https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip";
            string zipPath = Path.Combine(_toolsDir, "ffmpeg.zip");

            await DownloadFile(downloadUrl, zipPath);

            Debug.WriteLine("[DependencyManager] Unzipping ffmpeg...", "DependencyManager");
            try {
                ZipFile.ExtractToDirectory(zipPath, _toolsDir, true);
                if (CheckFileExistsRecursively(_toolsDir, ffmpegExe, out foundPath)) {
                     Debug.WriteLine($"[DependencyManager] ffmpeg installed at {foundPath}", "DependencyManager");
                     FFMPEG.Path_FFMPEG = foundPath;
                     DLP.Path_FFMPEG = foundPath;
                } else {
                     Debug.WriteLine("[DependencyManager] Failed to locate ffmpeg.exe after unzip.", "DependencyManager");
                }
            } catch (Exception ex) {
                 Debug.WriteLine($"[DependencyManager] Unzip failed: {ex.Message}", "DependencyManager");
            } finally {
                if (File.Exists(zipPath)) File.Delete(zipPath);
            }
        }

        private static async Task EnsureYtDlp() {
            string exeName = "yt-dlp.exe";
            string localPath = Path.Combine(_toolsDir, exeName);

            if (File.Exists(localPath)) {
                 Debug.WriteLine($"[DependencyManager] yt-dlp found at {localPath}", "DependencyManager");
                 DLP.Path_DLP = localPath;
                 return;
            }

            string systemPath = FindInPath(exeName);
            if (!string.IsNullOrEmpty(systemPath)) {
                Debug.WriteLine($"[DependencyManager] yt-dlp found in PATH at {systemPath}", "DependencyManager");
                DLP.Path_DLP = systemPath;
                return;
            }

            Debug.WriteLine("[DependencyManager] yt-dlp not found. Downloading...", "DependencyManager");
            string url = "https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp.exe";
            await DownloadFile(url, localPath);
            DLP.Path_DLP = localPath;
        }

        private static async Task EnsureJSRuntime() {
            // Check for deno, node, bun, quickjs in order
            string[] runtimes = { "deno", "node", "bun", "qjs" };
            
            foreach (var runtime in runtimes) {
                string exeName = runtime + ".exe";
                string systemPath = FindInPath(exeName);
                if (!string.IsNullOrEmpty(systemPath)) {
                     Debug.WriteLine($"[DependencyManager] JS Runtime {runtime} found at {systemPath}", "DependencyManager");
                     DLP.Path_JS = systemPath; 
                     return;
                }
            }
            
            // Check local folder for Deno
             string localDeno = Path.Combine(_toolsDir, "deno.exe");
             if (File.Exists(localDeno)) {
                 Debug.WriteLine($"[DependencyManager] Deno found locally at {localDeno}", "DependencyManager");
                 DLP.Path_JS = localDeno;
                 return;
             }

            // Defaults to downloading Deno
            Debug.WriteLine("[DependencyManager] No JS Runtime found. Downloading Deno...", "DependencyManager");
            string url = "https://github.com/denoland/deno/releases/latest/download/deno-x86_64-pc-windows-msvc.zip";
            string zipPath = Path.Combine(_toolsDir, "deno.zip");

            await DownloadFile(url, zipPath);
            
            Debug.WriteLine("[DependencyManager] Unzipping Deno...", "DependencyManager");
            try {
                ZipFile.ExtractToDirectory(zipPath, _toolsDir, true);
                if (File.Exists(localDeno)) {
                    Debug.WriteLine($"[DependencyManager] Deno installed at {localDeno}", "DependencyManager");
                    DLP.Path_JS = localDeno;
                }
            } catch(Exception ex) {
                 Debug.WriteLine($"[DependencyManager] Deno unzip failed: {ex.Message}", "DependencyManager");
            } finally {
                if (File.Exists(zipPath)) File.Delete(zipPath);
            }
        }

        private static async Task DownloadFile(string url, string outputPath) {
            try {
                Debug.WriteLine($"[DependencyManager] Downloading {url} to {outputPath} ...", "DependencyManager");
                using (var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead)) {
                    response.EnsureSuccessStatusCode();
                    using (var stream = await response.Content.ReadAsStreamAsync())
                    using (var fileStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None)) {
                         // Simple progress reporting could be added here
                         await stream.CopyToAsync(fileStream);
                    }
                }
                Debug.WriteLine($"[DependencyManager] Download complete.", "DependencyManager");
            } catch (Exception ex) {
                Debug.WriteLine($"[DependencyManager] Download failed: {ex.Message}", "DependencyManager");
                throw;
            }
        }

        private static string FindInPath(string fileName) {
            var paths = Environment.GetEnvironmentVariable("PATH")?.Split(Path.PathSeparator);
            if (paths == null) return null;

            foreach (var path in paths) {
                try {
                    string fullPath = Path.Combine(path, fileName);
                    if (File.Exists(fullPath)) return fullPath;
                } catch { }
            }
            return null;
        }

        private static bool CheckFileExistsRecursively(string rootDir, string filename, out string foundPath) {
            foundPath = null;
            try {
                var file = Directory.EnumerateFiles(rootDir, filename, SearchOption.AllDirectories).FirstOrDefault();
                if (file != null) {
                    foundPath = file;
                    return true;
                }
            } catch { } // Ignore permission errors etc
            return false;
        }
    }
}

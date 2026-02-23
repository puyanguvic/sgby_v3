using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace IBaye.UnityBridge
{
    public sealed class IBayeHost : MonoBehaviour
    {
        public struct CityStats
        {
            public int Belong;
            public int Satrap;
            public int Money;
            public int Food;
            public int MothballArms;
            public int Population;
            public int Devotion;
            public int Farming;
            public int Commerce;
            public int State;
            public int Persons;
        }

        public struct RuntimeState
        {
            public int PlayerKing;
            public int CityCursor;
            public int CitySetX;
            public int CitySetY;
            public int FightMode;
            public int FightCity;
            public int FightOver;
            public int FightBout;
            public int FightWeather;
            public int FightActive;
        }

        [Header("Paths")]
        [Tooltip("Relative paths are resolved from the Unity project root.")]
        public string DatPath = "../dist-win/dat.lib";
        public string FontDir = "../dist-win";
        [Tooltip("Empty will fallback to Application.persistentDataPath/save.")]
        public string SaveDir = "";

        [Header("Engine")]
        public int ScreenWidth = 208;
        public int ScreenHeight = 128;
        public bool AutoStart = true;

        private byte[] _frameBuffer = Array.Empty<byte>();
        private uint _latestFrameId;
        private uint _consumedFrameId;
        private int _frameWidth;
        private int _frameHeight;
        private bool _startedByHost;
        private string _status = "idle";
        private bool _nativeChecked;
        private bool _nativeAvailable = true;
        private string _nativeError = string.Empty;

        private static readonly string[] RequiredFontFiles =
        {
            "font.bin",
            "font24.cn.1",
            "font24.cn.2",
            "font24.cn.3",
            "font24.cn.4",
            "font24.en.1",
            "font24.en.2"
        };

        public bool IsRunning => EngineState == IBayeNative.EngineStateRunning;
        public bool IsStarted => _startedByHost || IsRunning;
        public int EngineState => SafeGetEngineState();
        public int FrameWidth => _frameWidth;
        public int FrameHeight => _frameHeight;
        public string StatusText => _status;
        public int CurrentPeriod => SafeGetCurrentPeriod();
        public bool NativeAvailable => EnsureNativeAvailable();
        public string NativeIssue => _nativeError;

        public event Action<int> KeySent;
        public event Action<int, int, int> TouchSent;

        private void Start()
        {
            EnsureNativeAvailable();
            if (AutoStart)
            {
                StartEngine();
            }
        }

        private void Update()
        {
            UpdateStatus();
            PollFrame();
        }

        public bool StartEngine()
        {
            if (_startedByHost)
            {
                return true;
            }

            if (!Preflight(out string preflightReport))
            {
                _status = "preflight failed: " + preflightReport;
                Debug.LogError(_status);
                return false;
            }

            if (!EnsureNativeAvailable())
            {
                return false;
            }

            string datPath = ResolvePath(DatPath);
            string fontDir = ResolvePath(FontDir);
            string saveDir = ResolvePath(SaveDir);
            if (string.IsNullOrWhiteSpace(saveDir))
            {
                saveDir = Path.Combine(Application.persistentDataPath, "save");
                SaveDir = saveDir;
            }

            if (!string.IsNullOrEmpty(datPath) || !string.IsNullOrEmpty(fontDir) || !string.IsNullOrEmpty(saveDir))
            {
                int setPathRc = IBayeNative.ibaye_unity_set_paths(
                    string.IsNullOrEmpty(datPath) ? null : datPath,
                    string.IsNullOrEmpty(fontDir) ? null : fontDir,
                    string.IsNullOrEmpty(saveDir) ? null : saveDir
                );
                if (setPathRc != 0)
                {
                    _status = "set_paths failed: " + IBayeNative.LastError();
                    Debug.LogError(_status);
                    return false;
                }
            }

            if (ScreenWidth > 0 && ScreenHeight > 0)
            {
                int sizeRc = IBayeNative.ibaye_unity_set_screen_size(ScreenWidth, ScreenHeight);
                if (sizeRc != 0)
                {
                    _status = "set_screen_size failed: " + IBayeNative.LastError();
                    Debug.LogError(_status);
                    return false;
                }
            }

            int startRc = IBayeNative.ibaye_unity_start();
            if (startRc != 0)
            {
                _status = "start failed: " + IBayeNative.LastError();
                Debug.LogError(_status);
                return false;
            }

            _startedByHost = true;
            _status = "booting";
            return true;
        }

        public bool LoadPeriod(int period)
        {
            int rc = IBayeNative.ibaye_unity_load_period(period);
            if (rc != 0)
            {
                _status = "load_period failed: " + IBayeNative.LastError();
                Debug.LogError(_status);
                return false;
            }

            if (!_startedByHost)
            {
                return StartEngine();
            }

            return true;
        }

        public void SendKey(int key)
        {
            IBayeNative.ibaye_unity_send_key(key);
            KeySent?.Invoke(key);
        }

        public void SendTouch(int evt, int x, int y)
        {
            IBayeNative.ibaye_unity_send_touch(evt, x, y);
            TouchSent?.Invoke(evt, x, y);
        }

        public bool TryConsumeLatestFrame(out byte[] frameRgba, out int width, out int height)
        {
            frameRgba = Array.Empty<byte>();
            width = 0;
            height = 0;

            if (_latestFrameId == 0 || _latestFrameId == _consumedFrameId)
            {
                return false;
            }

            _consumedFrameId = _latestFrameId;
            frameRgba = _frameBuffer;
            width = _frameWidth;
            height = _frameHeight;
            return true;
        }

        public bool TryGetRuntimeState(out RuntimeState state)
        {
            state = default;
            if (!IsRunning)
            {
                return false;
            }

            try
            {
                int rc = IBayeNative.ibaye_unity_get_runtime_state(
                    out int playerKing,
                    out int cityCursor,
                    out int citySetX,
                    out int citySetY,
                    out int fightMode,
                    out int fightCity,
                    out int fightOver,
                    out int fightBout,
                    out int fightWeather,
                    out int fightActive
                );
                if (rc != 0)
                {
                    return false;
                }

                state.PlayerKing = playerKing;
                state.CityCursor = cityCursor;
                state.CitySetX = citySetX;
                state.CitySetY = citySetY;
                state.FightMode = fightMode;
                state.FightCity = fightCity;
                state.FightOver = fightOver;
                state.FightBout = fightBout;
                state.FightWeather = fightWeather;
                state.FightActive = fightActive;
                return true;
            }
            catch (Exception ex)
            {
                _status = "runtime_state failed: " + ex.Message;
                Debug.LogError(_status);
                return false;
            }
        }

        public bool TryGetCityCount(out int count)
        {
            count = 0;
            try
            {
                count = IBayeNative.ibaye_unity_get_city_count();
                return count >= 0;
            }
            catch (Exception ex)
            {
                _status = "city_count failed: " + ex.Message;
                Debug.LogError(_status);
                return false;
            }
        }

        public string GetCityName(byte cityIndex)
        {
            byte[] nameBytes = new byte[64];
            try
            {
                int n = IBayeNative.ibaye_unity_get_city_name_bytes(cityIndex, nameBytes, nameBytes.Length);
                if (n <= 0)
                {
                    return string.Empty;
                }
                return IBayeNative.DecodeGbk(nameBytes, n);
            }
            catch (Exception ex)
            {
                _status = "city_name failed: " + ex.Message;
                Debug.LogError(_status);
                return string.Empty;
            }
        }

        public bool TryGetCityStats(byte cityIndex, out CityStats stats)
        {
            stats = default;
            try
            {
                int rc = IBayeNative.ibaye_unity_get_city_stats(
                    cityIndex,
                    out int belong,
                    out int satrap,
                    out int money,
                    out int food,
                    out int mothballArms,
                    out int population,
                    out int devotion,
                    out int farming,
                    out int commerce,
                    out int state,
                    out int persons
                );
                if (rc != 0)
                {
                    return false;
                }

                stats.Belong = belong;
                stats.Satrap = satrap;
                stats.Money = money;
                stats.Food = food;
                stats.MothballArms = mothballArms;
                stats.Population = population;
                stats.Devotion = devotion;
                stats.Farming = farming;
                stats.Commerce = commerce;
                stats.State = state;
                stats.Persons = persons;
                return true;
            }
            catch (Exception ex)
            {
                _status = "city_stats failed: " + ex.Message;
                Debug.LogError(_status);
                return false;
            }
        }

        public bool Preflight(out string report)
        {
            List<string> issues = new List<string>();
            if (_nativeChecked && !_nativeAvailable)
            {
                _nativeChecked = false;
                _nativeError = string.Empty;
            }
            if (!EnsureNativeAvailable())
            {
                issues.Add(_nativeError);
            }

            string datPath = ResolvePath(DatPath);
            if (string.IsNullOrWhiteSpace(datPath) || !File.Exists(datPath))
            {
                issues.Add("dat.lib 路径无效，请在 IBayeHost.DatPath 指向 dat.lib");
            }
            else if (!IsDatLibLikelyValid(datPath, out string datIssue))
            {
                issues.Add(datIssue);
            }

            string fontDir = ResolvePath(FontDir);
            string fontIssue = GetFontDirIssue(fontDir);
            if (!string.IsNullOrEmpty(fontIssue))
            {
                issues.Add(fontIssue);
            }

            string saveDir = ResolvePath(SaveDir);
            if (string.IsNullOrWhiteSpace(saveDir))
            {
                saveDir = Path.Combine(Application.persistentDataPath, "save");
                SaveDir = saveDir;
            }

            try
            {
                Directory.CreateDirectory(saveDir);
            }
            catch (Exception ex)
            {
                issues.Add("存档目录不可写: " + ex.Message);
            }

            report = issues.Count == 0 ? "启动检查通过" : string.Join("\n", issues);
            return issues.Count == 0;
        }

        public bool ApplyAutoFixes(out string report)
        {
            List<string> logs = new List<string>();

            if (!HasValidDatPath())
            {
                string candidate = FindDatLibPath();
                if (!string.IsNullOrEmpty(candidate))
                {
                    DatPath = candidate;
                    logs.Add("已自动定位 dat.lib: " + candidate);
                }
            }

            if (!HasValidFontDir())
            {
                string candidate = FindFontDir();
                if (!string.IsNullOrEmpty(candidate))
                {
                    FontDir = candidate;
                    logs.Add("已自动定位字体目录: " + candidate);
                }
            }

            if (string.IsNullOrWhiteSpace(ResolvePath(SaveDir)))
            {
                SaveDir = Path.Combine(Application.persistentDataPath, "save");
                logs.Add("已设置存档目录: " + SaveDir);
            }

            string saveDir = ResolvePath(SaveDir);
            if (!string.IsNullOrEmpty(saveDir))
            {
                try
                {
                    Directory.CreateDirectory(saveDir);
                }
                catch (Exception ex)
                {
                    logs.Add("创建存档目录失败: " + ex.Message);
                }
            }

            bool ok = Preflight(out string preflightReport);
            if (logs.Count == 0)
            {
                report = preflightReport;
            }
            else
            {
                report = string.Join("\n", logs) + "\n" + preflightReport;
            }
            return ok;
        }

        private bool EnsureNativeAvailable()
        {
            if (_nativeChecked)
            {
                return _nativeAvailable;
            }

            _nativeChecked = true;
            try
            {
                IBayeNative.ibaye_unity_is_running();
                _nativeAvailable = true;
                _nativeError = string.Empty;
                return true;
            }
            catch (Exception ex)
            {
                MarkNativeFailure("加载 ibaye_unity_bridge 失败", ex);
                return false;
            }
        }

        private void MarkNativeFailure(string action, Exception ex)
        {
            _nativeAvailable = false;
            _nativeChecked = true;
            _nativeError = action + ": " + ex.GetType().Name + " - " + ex.Message;
            _status = _nativeError;
            Debug.LogError(_nativeError);
        }

        private bool HasValidDatPath()
        {
            string datPath = ResolvePath(DatPath);
            return !string.IsNullOrWhiteSpace(datPath) && File.Exists(datPath) && IsDatLibLikelyValid(datPath, out _);
        }

        private bool HasValidFontDir()
        {
            string fontDir = ResolvePath(FontDir);
            return string.IsNullOrEmpty(GetFontDirIssue(fontDir));
        }

        private static string FindDatLibPath()
        {
            string projectRoot = GetProjectRoot();
            string[] candidates =
            {
                Path.Combine(projectRoot, "dist-win", "dat.lib"),
                Path.Combine(projectRoot, "dat.lib"),
                Path.Combine(projectRoot, "data", "dat.lib"),
                Path.Combine(projectRoot, "assets", "dat.lib")
            };

            for (int i = 0; i < candidates.Length; i++)
            {
                if (File.Exists(candidates[i]) && IsDatLibLikelyValid(candidates[i], out _))
                {
                    return candidates[i];
                }
            }

            string exeDir = GetExecutableDir();
            if (!string.IsNullOrWhiteSpace(exeDir))
            {
                string[] runtimeCandidates =
                {
                    Path.Combine(exeDir, "dat.lib"),
                    Path.Combine(exeDir, "dist-win", "dat.lib"),
                    Path.Combine(exeDir, "data", "dat.lib")
                };

                for (int i = 0; i < runtimeCandidates.Length; i++)
                {
                    if (File.Exists(runtimeCandidates[i]) && IsDatLibLikelyValid(runtimeCandidates[i], out _))
                    {
                        return runtimeCandidates[i];
                    }
                }
            }

            return string.Empty;
        }

        private static string FindFontDir()
        {
            string projectRoot = GetProjectRoot();
            string[] candidates =
            {
                Path.Combine(projectRoot, "dist-win"),
                projectRoot,
                Path.Combine(projectRoot, "src"),
                Path.Combine(projectRoot, "data"),
                Path.Combine(projectRoot, "assets")
            };

            for (int i = 0; i < candidates.Length; i++)
            {
                if (string.IsNullOrEmpty(GetFontDirIssue(candidates[i])))
                {
                    return candidates[i];
                }
            }

            string exeDir = GetExecutableDir();
            if (!string.IsNullOrWhiteSpace(exeDir))
            {
                string[] runtimeCandidates =
                {
                    exeDir,
                    Path.Combine(exeDir, "dist-win"),
                    Path.Combine(exeDir, "data")
                };

                for (int i = 0; i < runtimeCandidates.Length; i++)
                {
                    if (string.IsNullOrEmpty(GetFontDirIssue(runtimeCandidates[i])))
                    {
                        return runtimeCandidates[i];
                    }
                }
            }

            return string.Empty;
        }

        private static string GetFontDirIssue(string fontDir)
        {
            if (string.IsNullOrWhiteSpace(fontDir) || !Directory.Exists(fontDir))
            {
                return "字体目录无效，请在 IBayeHost.FontDir 指向包含完整字体资源的目录";
            }

            List<string> missing = new List<string>();
            for (int i = 0; i < RequiredFontFiles.Length; i++)
            {
                if (!File.Exists(Path.Combine(fontDir, RequiredFontFiles[i])))
                {
                    missing.Add(RequiredFontFiles[i]);
                }
            }

            if (missing.Count == 0)
            {
                return string.Empty;
            }

            return "字体目录缺少文件: " + string.Join(", ", missing) + "；请使用包含完整字体资源的目录（例如 dist-win）";
        }

        private static bool IsDatLibLikelyValid(string datPath, out string issue)
        {
            issue = string.Empty;
            try
            {
                long len = new FileInfo(datPath).Length;
                if (len < 4096)
                {
                    issue = "dat.lib 文件过小，可能不是有效资源库: " + datPath;
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                issue = "读取 dat.lib 失败: " + ex.Message;
                return false;
            }
        }

        private int SafeGetEngineState()
        {
            try
            {
                return IBayeNative.ibaye_unity_get_engine_state();
            }
            catch (DllNotFoundException ex)
            {
                MarkNativeFailure("插件缺失", ex);
                return IBayeNative.EngineStateError;
            }
            catch (EntryPointNotFoundException ex)
            {
                MarkNativeFailure("插件入口缺失", ex);
                return IBayeNative.EngineStateError;
            }
            catch (Exception ex)
            {
                MarkNativeFailure("读取引擎状态失败", ex);
                return IBayeNative.EngineStateError;
            }
        }

        private void PollFrame()
        {
            if (!EnsureNativeAvailable())
            {
                return;
            }

            try
            {
                int needed = IBayeNative.ibaye_unity_get_frame_bytes();
                if (needed <= 0)
                {
                    return;
                }

                if (_frameBuffer.Length < needed)
                {
                    _frameBuffer = new byte[needed];
                }

                int rc = IBayeNative.ibaye_unity_copy_frame(
                    _frameBuffer,
                    _frameBuffer.Length,
                    out int width,
                    out int height,
                    out uint frameId
                );
                if (rc <= 0)
                {
                    return;
                }

                _frameWidth = width;
                _frameHeight = height;
                _latestFrameId = frameId;
            }
            catch (Exception ex)
            {
                MarkNativeFailure("拷贝帧缓冲失败", ex);
            }
        }

        private void UpdateStatus()
        {
            if (!EnsureNativeAvailable())
            {
                return;
            }

            int state = EngineState;
            if (state == IBayeNative.EngineStateError)
            {
                if (string.IsNullOrEmpty(_status) || _status == "error")
                {
                    string err = IBayeNative.LastError();
                    _status = string.IsNullOrEmpty(err) ? "error" : "error: " + err;
                }
                return;
            }

            string stateName = IBayeNative.EngineStateName();
            _status = string.IsNullOrEmpty(stateName) ? state.ToString() : stateName;
        }

        private int SafeGetCurrentPeriod()
        {
            try
            {
                return IBayeNative.ibaye_unity_get_current_period();
            }
            catch
            {
                return 0;
            }
        }

        private static string GetProjectRoot()
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        }

        private static string GetExecutableDir()
        {
            try
            {
                if (Application.isEditor)
                {
                    return string.Empty;
                }

                string dataDir = Application.dataPath;
                if (string.IsNullOrWhiteSpace(dataDir))
                {
                    return string.Empty;
                }

                string exeDir = Path.GetDirectoryName(dataDir);
                return exeDir ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string ResolvePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return string.Empty;
            }

            if (Path.IsPathRooted(path))
            {
                return path;
            }

            string projectRoot = GetProjectRoot();
            return Path.GetFullPath(Path.Combine(projectRoot, path));
        }
    }
}

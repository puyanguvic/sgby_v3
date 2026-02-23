using System;
using System.Runtime.InteropServices;
using System.Text;

namespace IBaye.UnityBridge
{
    internal static class IBayeNative
    {
        private const string DllName = "ibaye_unity_bridge";
        private static bool _encodingReady;

        public const int TouchDown = 1;
        public const int TouchUp = 2;
        public const int TouchMove = 3;
        public const int TouchCancel = 4;

        public const int KeyEnter = 0x27;
        public const int KeyExit = 0x28;
        public const int KeyUp = 0x22;
        public const int KeyDown = 0x23;
        public const int KeyLeft = 0x24;
        public const int KeyRight = 0x25;
        public const int KeyPgUp = 0x20;
        public const int KeyPgDn = 0x21;

        public const int EngineStateError = -1;
        public const int EngineStateIdle = 0;
        public const int EngineStateBooting = 1;
        public const int EngineStateReady = 2;
        public const int EngineStateRunning = 3;
        public const int EngineStateExited = 4;

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ibaye_unity_set_paths(string datPath, string fontDir, string dataDir);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ibaye_unity_set_screen_size(int width, int height);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ibaye_unity_start();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ibaye_unity_is_running();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr ibaye_unity_last_error();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ibaye_unity_get_engine_state();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr ibaye_unity_get_engine_state_name();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void ibaye_unity_send_key(int key);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void ibaye_unity_send_touch(int evt, int x, int y);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ibaye_unity_get_frame_bytes();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ibaye_unity_copy_frame(
            byte[] outRgba,
            int outLen,
            out int outWidth,
            out int outHeight,
            out uint outFrameId
        );

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ibaye_unity_get_current_period();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ibaye_unity_load_period(int period);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ibaye_unity_get_city_count();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ibaye_unity_get_city_name_bytes(byte cityIndex, byte[] outBuf, int outLen);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ibaye_unity_get_city_stats(
            byte cityIndex,
            out int outBelong,
            out int outSatrap,
            out int outMoney,
            out int outFood,
            out int outMothballArms,
            out int outPopulation,
            out int outPeopleDevotion,
            out int outFarming,
            out int outCommerce,
            out int outState,
            out int outPersons
        );

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ibaye_unity_get_runtime_state(
            out int outPlayerKing,
            out int outCityCursor,
            out int outCitySetX,
            out int outCitySetY,
            out int outFightMode,
            out int outFightCity,
            out int outFightOver,
            out int outFightBout,
            out int outFightWeather,
            out int outFightActive
        );

        public static string LastError()
        {
            IntPtr ptr = ibaye_unity_last_error();
            return ptr == IntPtr.Zero ? string.Empty : Marshal.PtrToStringAnsi(ptr) ?? string.Empty;
        }

        public static string EngineStateName()
        {
            IntPtr ptr = ibaye_unity_get_engine_state_name();
            return ptr == IntPtr.Zero ? string.Empty : Marshal.PtrToStringAnsi(ptr) ?? string.Empty;
        }

        public static string DecodeGbk(byte[] data, int len)
        {
            if (data == null || len <= 0)
            {
                return string.Empty;
            }

            EnsureEncodingProvider();
            try
            {
                return Encoding.GetEncoding(936).GetString(data, 0, len);
            }
            catch
            {
                return Encoding.UTF8.GetString(data, 0, len);
            }
        }

        private static void EnsureEncodingProvider()
        {
            if (_encodingReady)
            {
                return;
            }

            try
            {
                Type providerType = Type.GetType("System.Text.CodePagesEncodingProvider, System.Text.Encoding.CodePages");
                object provider = providerType?.GetProperty("Instance")?.GetValue(null);
                if (provider is EncodingProvider encodingProvider)
                {
                    Encoding.RegisterProvider(encodingProvider);
                }
            }
            catch
            {
                // Keep UTF-8 fallback in DecodeGbk.
            }

            _encodingReady = true;
        }
    }
}

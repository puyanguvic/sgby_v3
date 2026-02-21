using System;
using System.Runtime.InteropServices;
using System.Text;

namespace IBaye.GodotBridge
{
    internal static class BridgeNative
    {
        private static bool _encodingReady = false;

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

        [DllImport("ibaye_godot_bridge", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ibaye_godot_set_paths(string datPath, string fontDir, string dataDir);

        [DllImport("ibaye_godot_bridge", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ibaye_godot_set_screen_size(int width, int height);

        [DllImport("ibaye_godot_bridge", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ibaye_godot_start();

        [DllImport("ibaye_godot_bridge", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ibaye_godot_is_running();

        [DllImport("ibaye_godot_bridge", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr ibaye_godot_last_error();

        [DllImport("ibaye_godot_bridge", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ibaye_godot_get_engine_state();

        [DllImport("ibaye_godot_bridge", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr ibaye_godot_get_engine_state_name();

        [DllImport("ibaye_godot_bridge", CallingConvention = CallingConvention.Cdecl)]
        public static extern void ibaye_godot_send_key(int key);

        [DllImport("ibaye_godot_bridge", CallingConvention = CallingConvention.Cdecl)]
        public static extern void ibaye_godot_send_touch(int evt, int x, int y);

        [DllImport("ibaye_godot_bridge", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ibaye_godot_get_frame_bytes();

        [DllImport("ibaye_godot_bridge", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ibaye_godot_copy_frame(
            byte[] outRgba,
            int outLen,
            out int outWidth,
            out int outHeight,
            out uint outFrameId
        );

        [DllImport("ibaye_godot_bridge", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ibaye_godot_get_current_period();

        [DllImport("ibaye_godot_bridge", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ibaye_godot_load_period(int period);

        [DllImport("ibaye_godot_bridge", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ibaye_godot_get_city_count();

        [DllImport("ibaye_godot_bridge", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ibaye_godot_get_city_name_bytes(byte cityIndex, byte[] outBuf, int outLen);

        [DllImport("ibaye_godot_bridge", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ibaye_godot_get_city_stats(
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

        [DllImport("ibaye_godot_bridge", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ibaye_godot_get_runtime_state(
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
            var ptr = ibaye_godot_last_error();
            return ptr == IntPtr.Zero ? string.Empty : Marshal.PtrToStringAnsi(ptr) ?? string.Empty;
        }

        public static string EngineStateName()
        {
            var ptr = ibaye_godot_get_engine_state_name();
            return ptr == IntPtr.Zero ? string.Empty : Marshal.PtrToStringAnsi(ptr) ?? string.Empty;
        }

        public static string DecodeGbk(byte[] data, int len)
        {
            if (!_encodingReady)
            {
                try
                {
                    var providerType = Type.GetType("System.Text.CodePagesEncodingProvider, System.Text.Encoding.CodePages");
                    var instanceProp = providerType?.GetProperty("Instance");
                    var provider = instanceProp?.GetValue(null) as EncodingProvider;
                    if (provider != null)
                    {
                        Encoding.RegisterProvider(provider);
                    }
                }
                catch
                {
                    // Keep fallback behavior below.
                }
                _encodingReady = true;
            }
            if (len <= 0)
            {
                return string.Empty;
            }
            try
            {
                return Encoding.GetEncoding(936).GetString(data, 0, len);
            }
            catch
            {
                return Encoding.UTF8.GetString(data, 0, len);
            }
        }
    }
}

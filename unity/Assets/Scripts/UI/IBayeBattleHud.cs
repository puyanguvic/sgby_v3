using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace IBaye.UnityBridge.UI
{
    public sealed class IBayeBattleHud : MonoBehaviour
    {
        public IBayeHost Host;
        public Text ModeLabel;
        public Text PeriodLabel;
        public Text LastCommandLabel;
        public Text TempoLabel;
        public Slider TempoSlider;
        public Text AlertLabel;

        private readonly Queue<float> _inputTimes = new Queue<float>();
        private string _lastCommand = "无";

        private void Awake()
        {
            if (Host == null)
            {
                Host = FindObjectOfType<IBayeHost>();
            }

            SubscribeHostEvents();
            RenderHud();
        }

        private void OnEnable()
        {
            SubscribeHostEvents();
        }

        private void OnDisable()
        {
            UnsubscribeHostEvents();
        }

        private void Update()
        {
            TrimOldInput();
            RenderHud();
        }

        private void SubscribeHostEvents()
        {
            if (Host == null)
            {
                return;
            }

            Host.KeySent -= OnKeySent;
            Host.TouchSent -= OnTouchSent;
            Host.KeySent += OnKeySent;
            Host.TouchSent += OnTouchSent;
        }

        private void UnsubscribeHostEvents()
        {
            if (Host == null)
            {
                return;
            }

            Host.KeySent -= OnKeySent;
            Host.TouchSent -= OnTouchSent;
        }

        private void OnKeySent(int key)
        {
            PushInput();
            _lastCommand = "按键: " + KeyName(key);
        }

        private void OnTouchSent(int evt, int x, int y)
        {
            PushInput();
            _lastCommand = "触控: " + evt + " (" + x + "," + y + ")";
        }

        private void PushInput()
        {
            _inputTimes.Enqueue(Time.unscaledTime);
            TrimOldInput();
        }

        private void TrimOldInput()
        {
            float now = Time.unscaledTime;
            while (_inputTimes.Count > 0 && now - _inputTimes.Peek() > 8f)
            {
                _inputTimes.Dequeue();
            }
        }

        private void RenderHud()
        {
            int tempo = ComputeTempo();
            bool hasRuntime = Host != null && Host.TryGetRuntimeState(out IBayeHost.RuntimeState rt);
            bool fightActive = hasRuntime && rt.FightActive != 0;

            if (TempoSlider != null)
            {
                TempoSlider.minValue = 0f;
                TempoSlider.maxValue = 100f;
                TempoSlider.value = tempo;
            }
            if (TempoLabel != null)
            {
                TempoLabel.text = "节奏 " + tempo + "%";
            }
            if (LastCommandLabel != null)
            {
                LastCommandLabel.text = "最近指令: " + _lastCommand;
            }
            if (PeriodLabel != null && Host != null)
            {
                if (hasRuntime)
                {
                    PeriodLabel.text =
                        "剧本 " + Host.CurrentPeriod +
                        " / 势力 " + (rt.PlayerKing + 1) +
                        " / 光标城市 " + (rt.CityCursor + 1);
                }
                else
                {
                    PeriodLabel.text = "剧本 " + Host.CurrentPeriod;
                }
            }
            if (ModeLabel != null)
            {
                if (fightActive)
                {
                    ModeLabel.text = "战斗-" + FightModeName(rt.FightMode);
                }
                else
                {
                    ModeLabel.text = tempo >= 70 ? "交战" : (tempo >= 30 ? "接敌" : "布阵");
                }
            }
            if (AlertLabel != null)
            {
                if (fightActive)
                {
                    AlertLabel.text =
                        "警报: 战斗中 / 回合 " + rt.FightBout +
                        " / 天气 " + WeatherName(rt.FightWeather) +
                        " / 目标城 " + (rt.FightCity + 1);
                }
                else
                {
                    AlertLabel.text = tempo >= 80 ? "警报: 战况激烈" : (tempo <= 15 ? "警报: 低活跃" : "警报: 正常");
                }
            }
        }

        private int ComputeTempo()
        {
            float actionsPerSecond = _inputTimes.Count / 8f;
            return Mathf.Clamp(Mathf.RoundToInt(actionsPerSecond / 4f * 100f), 0, 100);
        }

        private static string KeyName(int key)
        {
            return key switch
            {
                IBayeNative.KeyEnter => "确认",
                IBayeNative.KeyExit => "返回",
                IBayeNative.KeyUp => "上",
                IBayeNative.KeyDown => "下",
                IBayeNative.KeyLeft => "左",
                IBayeNative.KeyRight => "右",
                IBayeNative.KeyPgUp => "上页",
                IBayeNative.KeyPgDn => "下页",
                _ => "0x" + key.ToString("X")
            };
        }

        private static string FightModeName(int mode)
        {
            return mode switch
            {
                0 => "防御",
                1 => "进攻",
                2 => "自动",
                _ => "未知(" + mode + ")"
            };
        }

        private static string WeatherName(int weather)
        {
            return weather switch
            {
                1 => "晴",
                2 => "阴",
                3 => "风",
                4 => "雨",
                5 => "雹",
                _ => "未知(" + weather + ")"
            };
        }
    }
}

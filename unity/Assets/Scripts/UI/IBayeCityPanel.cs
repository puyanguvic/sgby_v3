using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace IBaye.UnityBridge.UI
{
    public sealed class IBayeCityPanel : MonoBehaviour
    {
        public IBayeHost Host;

        [Header("UI")]
        public Dropdown CityDropdown;
        public Text DetailLabel;
        public Dropdown PeriodDropdown;
        public Button PeriodLoadButton;
        public Button DomesticButton;
        public Button MilitaryButton;
        public Button DiplomacyButton;
        public Text OpsLogText;

        [Header("Action Key Sequences")]
        public string DomesticKeySequence = "PGUP";
        public string MilitaryKeySequence = "PGDN";
        public string DiplomacyKeySequence = "ENTER";

        [Header("Refresh")]
        public float RefreshIntervalSeconds = 0.5f;

        private readonly List<int> _cityIndices = new List<int>();
        private float _refreshCooldown;

        private static readonly string[] PeriodOptions =
        {
            "董卓弄权 (1)",
            "曹操崛起 (2)",
            "赤壁之战 (3)",
            "三足鼎立 (4)"
        };

        private void Awake()
        {
            if (Host == null)
            {
                Host = FindObjectOfType<IBayeHost>();
            }

            if (PeriodDropdown != null && PeriodDropdown.options.Count == 0)
            {
                PeriodDropdown.ClearOptions();
                PeriodDropdown.AddOptions(new List<string>(PeriodOptions));
            }

            if (PeriodLoadButton != null)
            {
                PeriodLoadButton.onClick.AddListener(OnPeriodLoadPressed);
            }
            if (CityDropdown != null)
            {
                CityDropdown.onValueChanged.AddListener(OnCitySelected);
            }
            if (DomesticButton != null)
            {
                DomesticButton.onClick.AddListener(OnDomesticPressed);
            }
            if (MilitaryButton != null)
            {
                MilitaryButton.onClick.AddListener(OnMilitaryPressed);
            }
            if (DiplomacyButton != null)
            {
                DiplomacyButton.onClick.AddListener(OnDiplomacyPressed);
            }

            if (DetailLabel != null)
            {
                DetailLabel.text = "请先在顶部菜单中启动游戏，然后此处会显示城市数据。";
            }
            if (OpsLogText != null)
            {
                OpsLogText.text = "[城市操作日志]\n等待指令...";
            }

            RefreshCityList();
        }

        private void OnDestroy()
        {
            if (PeriodLoadButton != null)
            {
                PeriodLoadButton.onClick.RemoveListener(OnPeriodLoadPressed);
            }
            if (CityDropdown != null)
            {
                CityDropdown.onValueChanged.RemoveListener(OnCitySelected);
            }
            if (DomesticButton != null)
            {
                DomesticButton.onClick.RemoveListener(OnDomesticPressed);
            }
            if (MilitaryButton != null)
            {
                MilitaryButton.onClick.RemoveListener(OnMilitaryPressed);
            }
            if (DiplomacyButton != null)
            {
                DiplomacyButton.onClick.RemoveListener(OnDiplomacyPressed);
            }
        }

        private void Update()
        {
            if (Host == null || !Host.IsRunning)
            {
                return;
            }

            if (CityDropdown != null && CityDropdown.options.Count == 0)
            {
                RefreshCityList();
            }

            _refreshCooldown -= Time.deltaTime;
            if (_refreshCooldown <= 0f)
            {
                _refreshCooldown = Mathf.Max(0.1f, RefreshIntervalSeconds);
                RefreshDetail();
            }
        }

        private void OnPeriodLoadPressed()
        {
            if (Host == null)
            {
                return;
            }

            if (!Host.IsRunning)
            {
                if (DetailLabel != null)
                {
                    DetailLabel.text = "引擎尚未启动";
                }
                return;
            }

            int period = PeriodDropdown == null ? 1 : PeriodDropdown.value + 1;
            if (!Host.LoadPeriod(period))
            {
                if (DetailLabel != null)
                {
                    DetailLabel.text = "加载剧本失败: " + Host.StatusText;
                }
                return;
            }

            RefreshCityList();
            RefreshDetail();
        }

        private void OnCitySelected(int index)
        {
            RefreshDetail();
        }

        private void OnDomesticPressed()
        {
            ExecuteCityAction("内政", DomesticKeySequence, IBayeNative.KeyPgUp);
        }

        private void OnMilitaryPressed()
        {
            ExecuteCityAction("军备", MilitaryKeySequence, IBayeNative.KeyPgDn);
        }

        private void OnDiplomacyPressed()
        {
            ExecuteCityAction("外交", DiplomacyKeySequence, IBayeNative.KeyEnter);
        }

        private void ExecuteCityAction(string actionName, string keySequence, int fallbackKey)
        {
            if (Host == null || !Host.IsRunning)
            {
                AppendOpsLog("引擎未启动，无法执行 " + actionName);
                return;
            }

            int city = GetSelectedCityIndex();
            if (city < 0)
            {
                AppendOpsLog("请先选择城市");
                return;
            }

            string cityName = Host.GetCityName((byte)city);
            if (string.IsNullOrEmpty(cityName))
            {
                cityName = "城市 " + (city + 1).ToString("D2");
            }

            int[] keys = ParseKeySequence(keySequence, fallbackKey);
            for (int i = 0; i < keys.Length; i++)
            {
                Host.SendKey(keys[i]);
            }

            string runtime = string.Empty;
            if (Host.TryGetRuntimeState(out IBayeHost.RuntimeState rt))
            {
                runtime = " / 势力 " + (rt.PlayerKing + 1) + " / 光标城 " + (rt.CityCursor + 1);
            }

            AppendOpsLog(
                "[" + DateTime.Now.ToString("HH:mm:ss") + "] " +
                cityName + " 执行 " + actionName +
                "（已发送 " + keys.Length + " 个指令）" + runtime
            );
        }

        private void RefreshCityList()
        {
            if (CityDropdown == null)
            {
                return;
            }

            CityDropdown.ClearOptions();
            _cityIndices.Clear();

            if (Host == null || !Host.IsRunning)
            {
                return;
            }

            if (!Host.TryGetCityCount(out int count) || count <= 0)
            {
                return;
            }

            List<string> options = new List<string>(count);
            for (int i = 0; i < count; i++)
            {
                string name = Host.GetCityName((byte)i);
                if (string.IsNullOrEmpty(name))
                {
                    name = "城市 " + (i + 1).ToString("D2");
                }
                options.Add((i + 1).ToString("D2") + "  " + name);
                _cityIndices.Add(i);
            }

            CityDropdown.AddOptions(options);
            CityDropdown.value = 0;
            CityDropdown.RefreshShownValue();
        }

        private void RefreshDetail()
        {
            if (DetailLabel == null)
            {
                return;
            }

            if (Host == null || !Host.IsRunning)
            {
                DetailLabel.text = "引擎未启动。";
                return;
            }

            int city = GetSelectedCityIndex();
            if (city < 0)
            {
                DetailLabel.text = "无城市数据。";
                return;
            }

            if (!Host.TryGetCityStats((byte)city, out IBayeHost.CityStats stats))
            {
                DetailLabel.text = "城市数据不可用";
                return;
            }

            string name = Host.GetCityName((byte)city);
            if (string.IsNullOrEmpty(name))
            {
                name = "城市 " + (city + 1);
            }

            DetailLabel.text =
                name + "\n" +
                "时期: " + Host.CurrentPeriod + "\n" +
                "归属: " + stats.Belong + "  太守: " + stats.Satrap + "\n" +
                "金钱: " + stats.Money + "  粮草: " + stats.Food + "\n" +
                "后备兵: " + stats.MothballArms + "  人口: " + stats.Population + "\n" +
                "民忠: " + stats.Devotion + "  农业: " + stats.Farming + "  商业: " + stats.Commerce + "\n" +
                "状态: " + stats.State + "  武将数: " + stats.Persons;
        }

        private int GetSelectedCityIndex()
        {
            if (CityDropdown == null || _cityIndices.Count == 0)
            {
                return -1;
            }

            int idx = Mathf.Clamp(CityDropdown.value, 0, _cityIndices.Count - 1);
            return _cityIndices[idx];
        }

        private void AppendOpsLog(string line)
        {
            if (OpsLogText == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(OpsLogText.text))
            {
                OpsLogText.text = line;
                return;
            }

            OpsLogText.text += "\n" + line;
        }

        private static int[] ParseKeySequence(string spec, int fallbackKey)
        {
            if (string.IsNullOrWhiteSpace(spec))
            {
                return new[] { fallbackKey };
            }

            string[] parts = spec.Split(',');
            List<int> keys = new List<int>(parts.Length);
            for (int i = 0; i < parts.Length; i++)
            {
                string token = parts[i].Trim().ToUpperInvariant();
                int key = token switch
                {
                    "ENTER" => IBayeNative.KeyEnter,
                    "EXIT" => IBayeNative.KeyExit,
                    "UP" => IBayeNative.KeyUp,
                    "DOWN" => IBayeNative.KeyDown,
                    "LEFT" => IBayeNative.KeyLeft,
                    "RIGHT" => IBayeNative.KeyRight,
                    "PGUP" => IBayeNative.KeyPgUp,
                    "PGDN" => IBayeNative.KeyPgDn,
                    _ => 0
                };

                if (key != 0)
                {
                    keys.Add(key);
                }
            }

            if (keys.Count == 0)
            {
                keys.Add(fallbackKey);
            }

            return keys.ToArray();
        }
    }
}

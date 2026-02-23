using System;
using UnityEngine;
using UnityEngine.UI;

namespace IBaye.UnityBridge.UI
{
    public sealed class IBayeMainMenuPanel : MonoBehaviour
    {
        public IBayeHost Host;
        public Dropdown PeriodDropdown;
        public Button StartButton;
        public Button ContinueButton;
        public Text StatusLabel;

        public event Action<int> CampaignStarted;
        public event Action MenuClosed;

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
                PeriodDropdown.AddOptions(new System.Collections.Generic.List<string>(PeriodOptions));
            }

            if (StartButton != null)
            {
                StartButton.onClick.AddListener(OnStartPressed);
            }
            if (ContinueButton != null)
            {
                ContinueButton.onClick.AddListener(OnContinuePressed);
            }

            UpdateContinueButtonText();
            RefreshStatus();
        }

        private void Update()
        {
            RefreshStatus();
        }

        private void OnDestroy()
        {
            if (StartButton != null)
            {
                StartButton.onClick.RemoveListener(OnStartPressed);
            }
            if (ContinueButton != null)
            {
                ContinueButton.onClick.RemoveListener(OnContinuePressed);
            }
        }

        public void OpenMenu()
        {
            gameObject.SetActive(true);
            UpdateContinueButtonText();
            RefreshStatus();
        }

        private void OnStartPressed()
        {
            if (Host == null)
            {
                SetStatus("未找到 IBayeHost");
                return;
            }

            int period = PeriodDropdown == null ? 1 : PeriodDropdown.value + 1;
            if (!Host.LoadPeriod(period))
            {
                SetStatus("启动失败: " + Host.StatusText);
                return;
            }

            SetStatus("已进入剧本 " + period);
            gameObject.SetActive(false);
            CampaignStarted?.Invoke(period);
        }

        private void OnContinuePressed()
        {
            if (Host == null)
            {
                SetStatus("未找到 IBayeHost");
                return;
            }

            if (!Host.IsRunning)
            {
                SetStatus("引擎未启动，请先开始游戏");
                return;
            }

            gameObject.SetActive(false);
            MenuClosed?.Invoke();
        }

        private void RefreshStatus()
        {
            if (StatusLabel == null || Host == null)
            {
                return;
            }

            string s = Host.IsRunning ? "引擎运行中" : "引擎未启动";
            if (Host.CurrentPeriod > 0)
            {
                s += " / 剧本 " + Host.CurrentPeriod;
            }
            StatusLabel.text = s;
            UpdateContinueButtonText();
        }

        private void SetStatus(string text)
        {
            if (StatusLabel != null)
            {
                StatusLabel.text = text;
            }
        }

        private void UpdateContinueButtonText()
        {
            if (ContinueButton == null)
            {
                return;
            }

            Text txt = ContinueButton.GetComponentInChildren<Text>();
            if (txt != null)
            {
                txt.text = Host != null && Host.IsRunning ? "继续" : "关闭";
            }
        }
    }
}

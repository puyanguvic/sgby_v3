using UnityEngine;
using UnityEngine.UI;

namespace IBaye.UnityBridge.UI
{
    public sealed class IBayeBootGuardPanel : MonoBehaviour
    {
        public IBayeHost Host;
        public Text MessageLabel;
        public Button FixButton;
        public Button RetryButton;
        public Button ContinueButton;

        private void Awake()
        {
            if (Host == null)
            {
                Host = FindObjectOfType<IBayeHost>();
            }

            if (FixButton != null)
            {
                FixButton.onClick.AddListener(OnFixPressed);
            }
            if (RetryButton != null)
            {
                RetryButton.onClick.AddListener(RefreshCheck);
            }
            if (ContinueButton != null)
            {
                ContinueButton.onClick.AddListener(OnContinuePressed);
            }

            RefreshCheck();
        }

        private void OnDestroy()
        {
            if (FixButton != null)
            {
                FixButton.onClick.RemoveListener(OnFixPressed);
            }
            if (RetryButton != null)
            {
                RetryButton.onClick.RemoveListener(RefreshCheck);
            }
            if (ContinueButton != null)
            {
                ContinueButton.onClick.RemoveListener(OnContinuePressed);
            }
        }

        public void RefreshCheck()
        {
            if (Host == null)
            {
                gameObject.SetActive(true);
                SetMessage("未找到 IBayeHost，请检查场景绑定。");
                return;
            }

            bool ok = Host.Preflight(out string report);
            if (ok)
            {
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);
            SetMessage("启动自检未通过\n" + report);
        }

        private void OnFixPressed()
        {
            if (Host == null)
            {
                SetMessage("未找到 IBayeHost");
                return;
            }

            Host.ApplyAutoFixes(out string report);
            SetMessage(report);
            RefreshCheck();
        }

        private void OnContinuePressed()
        {
            gameObject.SetActive(false);
        }

        private void SetMessage(string text)
        {
            if (MessageLabel != null)
            {
                MessageLabel.text = text;
            }
        }
    }
}

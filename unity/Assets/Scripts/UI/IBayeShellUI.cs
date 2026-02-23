using System;
using UnityEngine;
using UnityEngine.UI;

namespace IBaye.UnityBridge.UI
{
    public sealed class IBayeShellUI : MonoBehaviour
    {
        public IBayeHost Host;
        public Text StatusLabel;
        public Text ClockLabel;
        public Text FpsLabel;
        public IBayeMainMenuPanel MainMenu;
        public IBayeBootGuardPanel BootGuard;
        public GameObject GameplayRoot;
        public GameObject BottomBar;

        private bool _campaignStarted;

        private void Awake()
        {
            if (Host == null)
            {
                Host = FindObjectOfType<IBayeHost>();
            }
            if (MainMenu == null)
            {
                MainMenu = FindObjectOfType<IBayeMainMenuPanel>();
                if (MainMenu == null)
                {
                    IBayeMainMenuPanel[] allMenus = Resources.FindObjectsOfTypeAll<IBayeMainMenuPanel>();
                    if (allMenus != null && allMenus.Length > 0)
                    {
                        MainMenu = allMenus[0];
                    }
                }
            }
            if (BootGuard == null)
            {
                BootGuard = FindObjectOfType<IBayeBootGuardPanel>();
                if (BootGuard == null)
                {
                    IBayeBootGuardPanel[] allGuards = Resources.FindObjectsOfTypeAll<IBayeBootGuardPanel>();
                    if (allGuards != null && allGuards.Length > 0)
                    {
                        BootGuard = allGuards[0];
                    }
                }
            }

            if (MainMenu != null)
            {
                MainMenu.CampaignStarted += OnCampaignStarted;
                MainMenu.MenuClosed += OnMenuClosed;
            }

            _campaignStarted = Host != null && Host.IsRunning;
            SyncGameplayVisibility();
            if (!_campaignStarted)
            {
                if (Host != null && !Host.Preflight(out _))
                {
                    if (BootGuard != null)
                    {
                        BootGuard.gameObject.SetActive(true);
                        BootGuard.RefreshCheck();
                    }
                }
                else
                {
                    MainMenu?.OpenMenu();
                }
            }
        }

        private void OnDestroy()
        {
            if (MainMenu != null)
            {
                MainMenu.CampaignStarted -= OnCampaignStarted;
                MainMenu.MenuClosed -= OnMenuClosed;
            }
        }

        private void Update()
        {
            if (!_campaignStarted && Host != null && Host.IsRunning)
            {
                _campaignStarted = true;
                SyncGameplayVisibility();
            }

            if (Host != null && StatusLabel != null)
            {
                string uiState = _campaignStarted ? "战役中" : "启动菜单";
                StatusLabel.text = Host.StatusText + " | " + uiState + " | P:" + Host.CurrentPeriod;
            }
            if (ClockLabel != null)
            {
                ClockLabel.text = DateTime.Now.ToString("HH:mm:ss");
            }
            if (FpsLabel != null)
            {
                float fps = Time.unscaledDeltaTime > 0f ? 1f / Time.unscaledDeltaTime : 0f;
                FpsLabel.text = "FPS " + Mathf.RoundToInt(fps);
            }
        }

        public void OnEnterPressed()
        {
            Host?.SendKey(IBayeNative.KeyEnter);
        }

        public void OnExitPressed()
        {
            Host?.SendKey(IBayeNative.KeyExit);
        }

        public void OnUpPressed()
        {
            Host?.SendKey(IBayeNative.KeyUp);
        }

        public void OnDownPressed()
        {
            Host?.SendKey(IBayeNative.KeyDown);
        }

        public void OnLeftPressed()
        {
            Host?.SendKey(IBayeNative.KeyLeft);
        }

        public void OnRightPressed()
        {
            Host?.SendKey(IBayeNative.KeyRight);
        }

        public void OnPgUpPressed()
        {
            Host?.SendKey(IBayeNative.KeyPgUp);
        }

        public void OnPgDnPressed()
        {
            Host?.SendKey(IBayeNative.KeyPgDn);
        }

        public void OnMenuPressed()
        {
            if (Host != null && !Host.Preflight(out _))
            {
                if (BootGuard != null)
                {
                    BootGuard.gameObject.SetActive(true);
                    BootGuard.RefreshCheck();
                    return;
                }
            }
            MainMenu?.OpenMenu();
        }

        private void OnCampaignStarted(int period)
        {
            _campaignStarted = true;
            SyncGameplayVisibility();
        }

        private void OnMenuClosed()
        {
            SyncGameplayVisibility();
        }

        private void SyncGameplayVisibility()
        {
            if (GameplayRoot != null)
            {
                GameplayRoot.SetActive(_campaignStarted);
            }
            if (BottomBar != null)
            {
                BottomBar.SetActive(_campaignStarted);
            }
        }
    }
}

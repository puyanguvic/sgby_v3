#if UNITY_EDITOR
using System.IO;
using IBaye.UnityBridge;
using IBaye.UnityBridge.UI;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

public static class IBayeSceneBuilder
{
    [MenuItem("IBaye/Create Default Shell Scene")]
    public static void CreateDefaultShellScene()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var uiResources = new DefaultControls.Resources();

        EnsureEventSystem();

        var hostGo = new GameObject("IBayeHost");
        var host = hostGo.AddComponent<IBayeHost>();
        host.AutoStart = false;

        var shellGo = new GameObject("ShellRoot");
        var shell = shellGo.AddComponent<IBayeShellUI>();
        shell.Host = host;

        var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        var root = CreatePanel("Root", canvasGo.transform, new Color(0.09f, 0.11f, 0.14f, 1f));
        SetStretch((RectTransform)root.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        var topBar = CreatePanel("TopBar", root.transform, new Color(0.14f, 0.18f, 0.24f, 0.96f));
        SetStretch((RectTransform)topBar.transform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -44f), Vector2.zero);

        var statusText = CreateText("Status", topBar.transform, "Engine: idle", TextAnchor.MiddleLeft, 18);
        SetStretch((RectTransform)statusText.transform, new Vector2(0f, 0f), new Vector2(0.6f, 1f), new Vector2(16f, 0f), new Vector2(-8f, 0f));

        var clockText = CreateText("Clock", topBar.transform, "00:00:00", TextAnchor.MiddleCenter, 18);
        SetStretch((RectTransform)clockText.transform, new Vector2(0.6f, 0f), new Vector2(0.8f, 1f), new Vector2(8f, 0f), new Vector2(-8f, 0f));

        var fpsText = CreateText("FPS", topBar.transform, "FPS 0", TextAnchor.MiddleRight, 18);
        SetStretch((RectTransform)fpsText.transform, new Vector2(0.8f, 0f), new Vector2(1f, 1f), new Vector2(8f, 0f), new Vector2(-110f, 0f));

        var menuButton = CreateButton(uiResources, "MenuButton", topBar.transform, "菜单");
        SetStretch((RectTransform)menuButton.transform, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-100f, 6f), new Vector2(-10f, -6f));

        var bottomBar = CreatePanel("BottomBar", root.transform, new Color(0.14f, 0.18f, 0.24f, 0.96f));
        SetStretch((RectTransform)bottomBar.transform, Vector2.zero, new Vector2(1f, 0f), Vector2.zero, new Vector2(0f, 58f));

        var gameplayRoot = new GameObject("GameplayRoot", typeof(RectTransform));
        gameplayRoot.transform.SetParent(root.transform, false);
        SetStretch((RectTransform)gameplayRoot.transform, Vector2.zero, Vector2.one, new Vector2(0f, 58f), new Vector2(0f, -44f));

        var cityPanelGo = CreatePanel("CityPanel", gameplayRoot.transform, new Color(0.16f, 0.21f, 0.27f, 0.95f));
        SetStretch((RectTransform)cityPanelGo.transform, new Vector2(0f, 0f), new Vector2(0f, 1f), Vector2.zero, new Vector2(360f, 0f));

        var viewportContainer = CreatePanel("ViewportContainer", gameplayRoot.transform, new Color(0.08f, 0.08f, 0.08f, 1f));
        SetStretch((RectTransform)viewportContainer.transform, Vector2.zero, Vector2.one, new Vector2(370f, 8f), new Vector2(-370f, -8f));

        var viewportRaw = new GameObject("Viewport", typeof(RectTransform), typeof(RawImage), typeof(IBayeViewport));
        viewportRaw.transform.SetParent(viewportContainer.transform, false);
        SetStretch((RectTransform)viewportRaw.transform, Vector2.zero, Vector2.one, new Vector2(8f, 8f), new Vector2(-8f, -8f));
        var viewportImage = viewportRaw.GetComponent<RawImage>();
        viewportImage.color = Color.white;
        var viewport = viewportRaw.GetComponent<IBayeViewport>();
        viewport.Host = host;

        var hudPanelGo = CreatePanel("BattleHud", gameplayRoot.transform, new Color(0.16f, 0.21f, 0.27f, 0.95f));
        SetStretch((RectTransform)hudPanelGo.transform, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-360f, 0f), Vector2.zero);

        var mainMenuGo = CreatePanel("MainMenuPanel", root.transform, new Color(0.10f, 0.13f, 0.18f, 0.97f));
        SetStretch((RectTransform)mainMenuGo.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-280f, -180f), new Vector2(280f, 180f));

        var bootGuardGo = CreatePanel("BootGuardPanel", root.transform, new Color(0.11f, 0.09f, 0.09f, 0.98f));
        SetStretch((RectTransform)bootGuardGo.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-360f, -220f), new Vector2(360f, 220f));
        bootGuardGo.SetActive(false);

        BuildBottomBar(uiResources, bottomBar.transform, shell);
        BuildMainMenu(uiResources, mainMenuGo.transform, host, out var mainMenu);
        BuildCityPanel(uiResources, cityPanelGo.transform, host, out var cityPanel);
        BuildBattleHud(uiResources, hudPanelGo.transform, host, out var hud);
        BuildBootGuard(uiResources, bootGuardGo.transform, host, out var bootGuard);

        shell.MainMenu = mainMenu;
        shell.BootGuard = bootGuard;
        shell.GameplayRoot = gameplayRoot;
        shell.BottomBar = bottomBar;
        shell.StatusLabel = statusText;
        shell.ClockLabel = clockText;
        shell.FpsLabel = fpsText;

        UnityEventTools.AddPersistentListener(menuButton.onClick, shell.OnMenuPressed);

        var sceneDir = Path.Combine("Assets", "Scenes");
        if (!Directory.Exists(sceneDir))
        {
            Directory.CreateDirectory(sceneDir);
            AssetDatabase.Refresh();
        }

        const string scenePath = "Assets/Scenes/Main.unity";
        EditorSceneManager.SaveScene(scene, scenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);
        Selection.activeObject = sceneAsset;
        EditorGUIUtility.PingObject(sceneAsset);
        Debug.Log("Created Unity shell scene: " + scenePath);
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindObjectOfType<EventSystem>() != null)
        {
            return;
        }

        _ = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }

    private static void BuildBottomBar(DefaultControls.Resources resources, Transform parent, IBayeShellUI shell)
    {
        string[] labels = { "确认", "返回", "上", "下", "左", "右", "上页", "下页" };
        UnityAction[] actions =
        {
            shell.OnEnterPressed,
            shell.OnExitPressed,
            shell.OnUpPressed,
            shell.OnDownPressed,
            shell.OnLeftPressed,
            shell.OnRightPressed,
            shell.OnPgUpPressed,
            shell.OnPgDnPressed
        };

        for (int i = 0; i < labels.Length; i++)
        {
            var btn = CreateButton(resources, "Btn" + labels[i], parent, labels[i]);
            float minX = i / 8f;
            float maxX = (i + 1) / 8f;
            SetStretch((RectTransform)btn.transform, new Vector2(minX, 0f), new Vector2(maxX, 1f), new Vector2(6f, 6f), new Vector2(-6f, -6f));
            UnityEventTools.AddPersistentListener(btn.onClick, actions[i]);
        }
    }

    private static void BuildMainMenu(DefaultControls.Resources resources, Transform parent, IBayeHost host, out IBayeMainMenuPanel panel)
    {
        var title = CreateText("Title", parent, "战役主菜单", TextAnchor.MiddleCenter, 26);
        SetStretch((RectTransform)title.transform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(20f, -60f), new Vector2(-20f, -16f));

        var periodDropdownGo = DefaultControls.CreateDropdown(resources);
        periodDropdownGo.name = "PeriodDropdown";
        periodDropdownGo.transform.SetParent(parent, false);
        SetStretch((RectTransform)periodDropdownGo.transform, new Vector2(0.1f, 0.56f), new Vector2(0.9f, 0.72f), Vector2.zero, Vector2.zero);
        var periodDropdown = periodDropdownGo.GetComponent<Dropdown>();

        var startBtn = CreateButton(resources, "StartButton", parent, "开始");
        SetStretch((RectTransform)startBtn.transform, new Vector2(0.1f, 0.38f), new Vector2(0.45f, 0.52f), Vector2.zero, Vector2.zero);

        var continueBtn = CreateButton(resources, "ContinueButton", parent, "继续");
        SetStretch((RectTransform)continueBtn.transform, new Vector2(0.55f, 0.38f), new Vector2(0.9f, 0.52f), Vector2.zero, Vector2.zero);

        var status = CreateText("Status", parent, "状态", TextAnchor.UpperLeft, 18);
        SetStretch((RectTransform)status.transform, new Vector2(0.08f, 0.06f), new Vector2(0.92f, 0.32f), Vector2.zero, Vector2.zero);

        panel = parent.gameObject.AddComponent<IBayeMainMenuPanel>();
        panel.Host = host;
        panel.PeriodDropdown = periodDropdown;
        panel.StartButton = startBtn;
        panel.ContinueButton = continueBtn;
        panel.StatusLabel = status;
    }

    private static void BuildCityPanel(DefaultControls.Resources resources, Transform parent, IBayeHost host, out IBayeCityPanel panel)
    {
        var title = CreateText("Title", parent, "城池总览", TextAnchor.MiddleCenter, 24);
        SetStretch((RectTransform)title.transform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(10f, -48f), new Vector2(-10f, -8f));

        var periodDropdownGo = DefaultControls.CreateDropdown(resources);
        periodDropdownGo.name = "PeriodDropdown";
        periodDropdownGo.transform.SetParent(parent, false);
        SetStretch((RectTransform)periodDropdownGo.transform, new Vector2(0.04f, 0.84f), new Vector2(0.66f, 0.93f), Vector2.zero, Vector2.zero);
        var periodDropdown = periodDropdownGo.GetComponent<Dropdown>();

        var periodLoadBtn = CreateButton(resources, "PeriodLoadButton", parent, "加载时期");
        SetStretch((RectTransform)periodLoadBtn.transform, new Vector2(0.70f, 0.84f), new Vector2(0.96f, 0.93f), Vector2.zero, Vector2.zero);

        var cityDropdownGo = DefaultControls.CreateDropdown(resources);
        cityDropdownGo.name = "CityDropdown";
        cityDropdownGo.transform.SetParent(parent, false);
        SetStretch((RectTransform)cityDropdownGo.transform, new Vector2(0.04f, 0.73f), new Vector2(0.96f, 0.82f), Vector2.zero, Vector2.zero);
        var cityDropdown = cityDropdownGo.GetComponent<Dropdown>();

        var detail = CreateText("Detail", parent, "城市详情", TextAnchor.UpperLeft, 16);
        SetStretch((RectTransform)detail.transform, new Vector2(0.04f, 0.33f), new Vector2(0.96f, 0.71f), Vector2.zero, Vector2.zero);

        var domesticBtn = CreateButton(resources, "DomesticButton", parent, "内政");
        SetStretch((RectTransform)domesticBtn.transform, new Vector2(0.04f, 0.24f), new Vector2(0.32f, 0.31f), Vector2.zero, Vector2.zero);

        var militaryBtn = CreateButton(resources, "MilitaryButton", parent, "军备");
        SetStretch((RectTransform)militaryBtn.transform, new Vector2(0.36f, 0.24f), new Vector2(0.64f, 0.31f), Vector2.zero, Vector2.zero);

        var diplomacyBtn = CreateButton(resources, "DiplomacyButton", parent, "外交");
        SetStretch((RectTransform)diplomacyBtn.transform, new Vector2(0.68f, 0.24f), new Vector2(0.96f, 0.31f), Vector2.zero, Vector2.zero);

        var opsLog = CreateText("OpsLog", parent, "操作日志", TextAnchor.UpperLeft, 14);
        SetStretch((RectTransform)opsLog.transform, new Vector2(0.04f, 0.04f), new Vector2(0.96f, 0.22f), Vector2.zero, Vector2.zero);

        panel = parent.gameObject.AddComponent<IBayeCityPanel>();
        panel.Host = host;
        panel.CityDropdown = cityDropdown;
        panel.DetailLabel = detail;
        panel.PeriodDropdown = periodDropdown;
        panel.PeriodLoadButton = periodLoadBtn;
        panel.DomesticButton = domesticBtn;
        panel.MilitaryButton = militaryBtn;
        panel.DiplomacyButton = diplomacyBtn;
        panel.OpsLogText = opsLog;
    }

    private static void BuildBattleHud(DefaultControls.Resources resources, Transform parent, IBayeHost host, out IBayeBattleHud panel)
    {
        var title = CreateText("Title", parent, "战场态势", TextAnchor.MiddleCenter, 24);
        SetStretch((RectTransform)title.transform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(10f, -48f), new Vector2(-10f, -8f));

        var mode = CreateText("Mode", parent, "模式", TextAnchor.MiddleLeft, 18);
        SetStretch((RectTransform)mode.transform, new Vector2(0.06f, 0.80f), new Vector2(0.94f, 0.88f), Vector2.zero, Vector2.zero);

        var period = CreateText("Period", parent, "时期", TextAnchor.MiddleLeft, 16);
        SetStretch((RectTransform)period.transform, new Vector2(0.06f, 0.72f), new Vector2(0.94f, 0.80f), Vector2.zero, Vector2.zero);

        var lastCommand = CreateText("LastCommand", parent, "最近指令", TextAnchor.MiddleLeft, 16);
        SetStretch((RectTransform)lastCommand.transform, new Vector2(0.06f, 0.64f), new Vector2(0.94f, 0.72f), Vector2.zero, Vector2.zero);

        var tempo = CreateText("Tempo", parent, "节奏", TextAnchor.MiddleLeft, 16);
        SetStretch((RectTransform)tempo.transform, new Vector2(0.06f, 0.56f), new Vector2(0.94f, 0.64f), Vector2.zero, Vector2.zero);

        var sliderGo = DefaultControls.CreateSlider(resources);
        sliderGo.name = "TempoSlider";
        sliderGo.transform.SetParent(parent, false);
        SetStretch((RectTransform)sliderGo.transform, new Vector2(0.08f, 0.49f), new Vector2(0.92f, 0.55f), Vector2.zero, Vector2.zero);
        var slider = sliderGo.GetComponent<Slider>();

        var alert = CreateText("Alert", parent, "警报", TextAnchor.UpperLeft, 16);
        SetStretch((RectTransform)alert.transform, new Vector2(0.06f, 0.08f), new Vector2(0.94f, 0.45f), Vector2.zero, Vector2.zero);

        panel = parent.gameObject.AddComponent<IBayeBattleHud>();
        panel.Host = host;
        panel.ModeLabel = mode;
        panel.PeriodLabel = period;
        panel.LastCommandLabel = lastCommand;
        panel.TempoLabel = tempo;
        panel.TempoSlider = slider;
        panel.AlertLabel = alert;
    }

    private static void BuildBootGuard(DefaultControls.Resources resources, Transform parent, IBayeHost host, out IBayeBootGuardPanel panel)
    {
        var title = CreateText("Title", parent, "启动自检", TextAnchor.MiddleCenter, 28);
        SetStretch((RectTransform)title.transform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(20f, -64f), new Vector2(-20f, -10f));

        var message = CreateText("Message", parent, "诊断结果", TextAnchor.UpperLeft, 18);
        SetStretch((RectTransform)message.transform, new Vector2(0.06f, 0.30f), new Vector2(0.94f, 0.86f), Vector2.zero, Vector2.zero);

        var fixBtn = CreateButton(resources, "FixButton", parent, "自动修复");
        SetStretch((RectTransform)fixBtn.transform, new Vector2(0.06f, 0.08f), new Vector2(0.31f, 0.22f), Vector2.zero, Vector2.zero);

        var retryBtn = CreateButton(resources, "RetryButton", parent, "重新检测");
        SetStretch((RectTransform)retryBtn.transform, new Vector2(0.37f, 0.08f), new Vector2(0.62f, 0.22f), Vector2.zero, Vector2.zero);

        var continueBtn = CreateButton(resources, "ContinueButton", parent, "忽略继续");
        SetStretch((RectTransform)continueBtn.transform, new Vector2(0.68f, 0.08f), new Vector2(0.93f, 0.22f), Vector2.zero, Vector2.zero);

        panel = parent.gameObject.AddComponent<IBayeBootGuardPanel>();
        panel.Host = host;
        panel.MessageLabel = message;
        panel.FixButton = fixBtn;
        panel.RetryButton = retryBtn;
        panel.ContinueButton = continueBtn;
    }

    private static GameObject CreatePanel(string name, Transform parent, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var image = go.GetComponent<Image>();
        image.color = color;
        return go;
    }

    private static Text CreateText(string name, Transform parent, string text, TextAnchor anchor, int fontSize)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        var t = go.GetComponent<Text>();
        t.text = text;
        t.alignment = anchor;
        t.fontSize = fontSize;
        t.color = new Color(0.90f, 0.94f, 1f, 1f);
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Truncate;
        t.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        return t;
    }

    private static Button CreateButton(DefaultControls.Resources resources, string name, Transform parent, string label)
    {
        var go = DefaultControls.CreateButton(resources);
        go.name = name;
        go.transform.SetParent(parent, false);
        var button = go.GetComponent<Button>();
        var txt = go.GetComponentInChildren<Text>();
        if (txt != null)
        {
            txt.text = label;
            txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            txt.alignment = TextAnchor.MiddleCenter;
        }
        return button;
    }

    private static void SetStretch(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
    }
}
#endif

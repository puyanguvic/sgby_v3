using Godot;
using IBaye.GodotBridge;
using System;
using System.Collections.Generic;

public partial class CityPanel : Control
{
    [Export] public NodePath HostPath;
    [Export] public NodePath CityListPath;
    [Export] public NodePath DetailLabelPath;
    [Export] public NodePath PeriodOptionPath;
    [Export] public NodePath PeriodLoadButtonPath;
    [Export] public NodePath DomesticButtonPath;
    [Export] public NodePath MilitaryButtonPath;
    [Export] public NodePath DiplomacyButtonPath;
    [Export] public NodePath OpsLogPath;
    [Export] public string DomesticKeySequence = "PGUP";
    [Export] public string MilitaryKeySequence = "PGDN";
    [Export] public string DiplomacyKeySequence = "ENTER";

    private BridgeHost _host;
    private ItemList _cityList;
    private RichTextLabel _detail;
    private OptionButton _periodOption;
    private Button _periodLoadButton;
    private Button _domesticButton;
    private Button _militaryButton;
    private Button _diplomacyButton;
    private RichTextLabel _opsLog;
    private double _refreshCooldown = 0.0;

    public override void _Ready()
    {
        _host = GetNodeOrNull<BridgeHost>(HostPath);
        if (_host == null)
        {
            _host = GetTree().GetFirstNodeInGroup("bridge_host") as BridgeHost;
        }
        _cityList = GetNodeOrNull<ItemList>(CityListPath);
        _detail = GetNodeOrNull<RichTextLabel>(DetailLabelPath);
        _periodOption = GetNodeOrNull<OptionButton>(PeriodOptionPath);
        _periodLoadButton = GetNodeOrNull<Button>(PeriodLoadButtonPath);
        _domesticButton = GetNodeOrNull<Button>(DomesticButtonPath);
        _militaryButton = GetNodeOrNull<Button>(MilitaryButtonPath);
        _diplomacyButton = GetNodeOrNull<Button>(DiplomacyButtonPath);
        _opsLog = GetNodeOrNull<RichTextLabel>(OpsLogPath);

        if (_periodOption != null && _periodOption.ItemCount == 0)
        {
            _periodOption.AddItem("董卓弄权 (1)");
            _periodOption.AddItem("曹操崛起 (2)");
            _periodOption.AddItem("赤壁之战 (3)");
            _periodOption.AddItem("三足鼎立 (4)");
        }

        if (_periodLoadButton != null)
        {
            _periodLoadButton.Pressed += OnPeriodLoadPressed;
        }
        if (_cityList != null)
        {
            _cityList.ItemSelected += OnCitySelected;
        }
        if (_domesticButton != null)
        {
            _domesticButton.Pressed += OnDomesticPressed;
        }
        if (_militaryButton != null)
        {
            _militaryButton.Pressed += OnMilitaryPressed;
        }
        if (_diplomacyButton != null)
        {
            _diplomacyButton.Pressed += OnDiplomacyPressed;
        }

        if (_detail != null)
        {
            _detail.Text = "请先在顶部菜单中启动游戏，然后此处会显示城市数据。";
        }
        if (_opsLog != null)
        {
            _opsLog.Text = "[b]城市操作日志[/b]\n等待指令...";
        }
        RefreshCityList();
    }

    public override void _Process(double delta)
    {
        if (_host == null || !_host.IsRunning)
        {
            return;
        }

        if (_cityList != null && _cityList.ItemCount == 0)
        {
            RefreshCityList();
        }

        _refreshCooldown -= delta;
        if (_refreshCooldown <= 0.0)
        {
            _refreshCooldown = 0.5;
            RefreshDetail();
        }
    }

    private void OnPeriodLoadPressed()
    {
        if (_periodOption == null)
        {
            return;
        }
        if (_host == null || !_host.IsRunning)
        {
            if (_detail != null)
            {
                _detail.Text = "[color=yellow]引擎尚未启动[/color]";
            }
            return;
        }
        int period = _periodOption.GetSelectedId() + 1;
        if (!_host.LoadPeriod(period))
        {
            if (_detail != null)
            {
                _detail.Text = "[color=red]加载剧本失败: " + _host.StatusText + "[/color]";
            }
            return;
        }
        RefreshCityList();
        RefreshDetail();
    }

    private void OnCitySelected(long index)
    {
        RefreshDetail();
    }

    private void OnDomesticPressed()
    {
        ExecuteCityAction("内政", DomesticKeySequence, BridgeNative.KeyPgUp);
    }

    private void OnMilitaryPressed()
    {
        ExecuteCityAction("军备", MilitaryKeySequence, BridgeNative.KeyPgDn);
    }

    private void OnDiplomacyPressed()
    {
        ExecuteCityAction("外交", DiplomacyKeySequence, BridgeNative.KeyEnter);
    }

    private void ExecuteCityAction(string actionName, string keySequence, int fallbackKey)
    {
        if (_host == null || !_host.IsRunning)
        {
            AppendOpsLog("[color=yellow]引擎未启动，无法执行 " + actionName + "[/color]");
            return;
        }
        int city = _cityList != null && _cityList.GetSelectedItems().Length > 0 ? (int)_cityList.GetSelectedItems()[0] : -1;
        if (city < 0)
        {
            AppendOpsLog("[color=yellow]请先选择城市[/color]");
            return;
        }

        string cityName = GetCityName((byte)city);
        if (string.IsNullOrEmpty(cityName))
        {
            cityName = "城市 " + (city + 1).ToString("D2");
        }

        int[] keys = ParseKeySequence(keySequence, fallbackKey);
        for (int i = 0; i < keys.Length; i++)
        {
            _host.SendKey(keys[i]);
        }

        string runtime = string.Empty;
        if (_host.TryGetRuntimeState(out BridgeHost.RuntimeState rt))
        {
            runtime = " / 势力 " + (rt.PlayerKing + 1) + " / 光标城 " + (rt.CityCursor + 1);
        }
        AppendOpsLog(
            "[" + DateTime.Now.ToString("HH:mm:ss") + "] " +
            cityName + " 执行 " + actionName +
            "（已发送 " + keys.Length + " 个指令）" + runtime
        );
    }

    private void AppendOpsLog(string line)
    {
        if (_opsLog == null)
        {
            return;
        }
        if (string.IsNullOrWhiteSpace(_opsLog.Text))
        {
            _opsLog.Text = line;
            return;
        }
        _opsLog.Text = _opsLog.Text + "\n" + line;
    }

    private void RefreshCityList()
    {
        if (_cityList == null)
        {
            return;
        }
        if (_host == null || !_host.IsRunning)
        {
            _cityList.Clear();
            return;
        }

        _cityList.Clear();
        if (!TryGetCityCount(out int count))
        {
            return;
        }
        for (int i = 0; i < count; i++)
        {
            string name = GetCityName((byte)i);
            if (string.IsNullOrEmpty(name))
            {
                name = "城市 " + (i + 1).ToString("D2");
            }
            _cityList.AddItem((i + 1).ToString("D2") + "  " + name);
        }

        if (_cityList.ItemCount > 0 && _cityList.GetSelectedItems().Length == 0)
        {
            _cityList.Select(0);
        }
    }

    private void RefreshDetail()
    {
        if (_cityList == null || _detail == null || _cityList.ItemCount == 0)
        {
            return;
        }
        if (_host == null || !_host.IsRunning)
        {
            _detail.Text = "引擎未启动。";
            return;
        }

        if (!TryGetCurrentPeriod(out int period))
        {
            _detail.Text = "[color=red]读取时期失败[/color]";
            return;
        }
        int city = _cityList.GetSelectedItems().Length > 0 ? (int)_cityList.GetSelectedItems()[0] : 0;

        if (!TryGetCityStats(
                (byte)city,
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
            ))
        {
            _detail.Text = "[color=red]城市数据不可用[/color]";
            return;
        }

        string name = GetCityName((byte)city);
        if (string.IsNullOrEmpty(name))
        {
            name = "城市 " + (city + 1);
        }

        _detail.Text =
            "[b]" + name + "[/b]\n" +
            "时期: " + period + "\n" +
            "归属: " + belong + "  太守: " + satrap + "\n" +
            "金钱: " + money + "  粮草: " + food + "\n" +
            "后备兵: " + mothballArms + "  人口: " + population + "\n" +
            "民忠: " + devotion + "  农业: " + farming + "  商业: " + commerce + "\n" +
            "状态: " + state + "  武将数: " + persons;
    }

    private string GetCityName(byte index)
    {
        byte[] buf = new byte[64];
        int n;
        try
        {
            n = BridgeNative.ibaye_godot_get_city_name_bytes(index, buf, buf.Length);
        }
        catch (Exception ex)
        {
            ReportNativeException("读取城市名称失败", ex);
            return string.Empty;
        }
        if (n <= 0)
        {
            return string.Empty;
        }
        return BridgeNative.DecodeGbk(buf, n);
    }

    private bool TryGetCityCount(out int count)
    {
        count = 0;
        try
        {
            count = BridgeNative.ibaye_godot_get_city_count();
            return count >= 0;
        }
        catch (Exception ex)
        {
            ReportNativeException("读取城市数量失败", ex);
            return false;
        }
    }

    private bool TryGetCurrentPeriod(out int period)
    {
        period = 0;
        try
        {
            period = BridgeNative.ibaye_godot_get_current_period();
            return period > 0;
        }
        catch (Exception ex)
        {
            ReportNativeException("读取时期失败", ex);
            return false;
        }
    }

    private bool TryGetCityStats(
        byte city,
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
    )
    {
        belong = 0;
        satrap = 0;
        money = 0;
        food = 0;
        mothballArms = 0;
        population = 0;
        devotion = 0;
        farming = 0;
        commerce = 0;
        state = 0;
        persons = 0;
        try
        {
            return BridgeNative.ibaye_godot_get_city_stats(
                       city,
                       out belong,
                       out satrap,
                       out money,
                       out food,
                       out mothballArms,
                       out population,
                       out devotion,
                       out farming,
                       out commerce,
                       out state,
                       out persons
                   ) == 0;
        }
        catch (Exception ex)
        {
            ReportNativeException("读取城市详情失败", ex);
            return false;
        }
    }

    private void ReportNativeException(string action, Exception ex)
    {
        string message = action + ": " + ex.GetType().Name + " - " + ex.Message;
        AppendOpsLog("[color=red]" + message + "[/color]");
        GD.PushError(message);
    }

    private static int[] ParseKeySequence(string spec, int fallbackKey)
    {
        if (string.IsNullOrWhiteSpace(spec))
        {
            return new int[] { fallbackKey };
        }

        string[] parts = spec.Split(',', StringSplitOptions.RemoveEmptyEntries);
        var keys = new List<int>(parts.Length);
        for (int i = 0; i < parts.Length; i++)
        {
            string token = parts[i].Trim().ToUpperInvariant();
            int key = token switch
            {
                "ENTER" => BridgeNative.KeyEnter,
                "EXIT" => BridgeNative.KeyExit,
                "UP" => BridgeNative.KeyUp,
                "DOWN" => BridgeNative.KeyDown,
                "LEFT" => BridgeNative.KeyLeft,
                "RIGHT" => BridgeNative.KeyRight,
                "PGUP" => BridgeNative.KeyPgUp,
                "PGDN" => BridgeNative.KeyPgDn,
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

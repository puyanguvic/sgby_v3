extends Control

const MAIN_SCENE := "res://scenes/main/Main.tscn"
const PROBE_SCRIPT := "res://scripts/autoload/AppBootstrap.cs"

@onready var status_label: Label = $Center/Panel/Margin/VBox/Status
@onready var detail_text: RichTextLabel = $Center/Panel/Margin/VBox/Detail
@onready var retry_button: Button = $Center/Panel/Margin/VBox/Buttons/RetryButton
@onready var open_folder_button: Button = $Center/Panel/Margin/VBox/Buttons/OpenFolderButton

func _ready() -> void:
	retry_button.pressed.connect(_on_retry_pressed)
	open_folder_button.pressed.connect(_on_open_folder_pressed)
	_probe_and_enter_main()

func _probe_and_enter_main() -> void:
	status_label.text = "正在检查 C# 运行环境..."
	detail_text.text = ""

	var script: Script = load(PROBE_SCRIPT)
	if script == null:
		_show_failure([
			"未能加载 C# 脚本加载器（load 返回 null）。",
			"这通常是 .NET 运行时文件缺失、被拦截，或系统阻止加载 DLL。",
			"请先运行 Diagnose_Runtime.bat 并检查是否存在 [FAIL]。"
		])
		return

	var obj: Object = script.new()
	if obj == null:
		_show_failure([
			"C# 脚本对象实例化失败（script.new() 返回 null）。",
			"通常是 C# 运行时初始化失败（hostfxr/coreclr 未加载）。",
			"请运行 Diagnose_Runtime.bat，并尝试对解压目录执行 Unblock-File。"
		])
		return
	if obj is Node:
		(obj as Node).queue_free()

	var err: Error = get_tree().change_scene_to_file(MAIN_SCENE)
	if err != OK:
		_show_failure([
			"主场景切换失败: %s" % str(err),
			"请检查导出包完整性，并把 logs/ibaye_startup.log 发给开发者。"
		])

func _show_failure(lines: Array[String]) -> void:
	status_label.text = "启动失败：C# 运行环境不可用"
	detail_text.text = "\n".join(lines) + "\n\n建议操作：\n" \
		+ "1) 在程序目录运行 Diagnose_Runtime.bat\n" \
		+ "2) PowerShell 执行: Get-ChildItem -Recurse | Unblock-File\n" \
		+ "3) 再次运行 Run_With_Log.bat"

func _on_retry_pressed() -> void:
	_probe_and_enter_main()

func _on_open_folder_pressed() -> void:
	var app_dir := OS.get_executable_path().get_base_dir()
	if app_dir.is_empty():
		return
	OS.shell_open(app_dir)

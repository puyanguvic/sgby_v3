extends Node

var is_bootstrapped: bool = false

func _ready() -> void:
	is_bootstrapped = true
	print("[AppBootstrap.gd] ready")

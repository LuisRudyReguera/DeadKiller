extends SceneTree

var failures := 0

func _initialize():
	call_deferred("run_tests")

func check(condition: bool, label: String):
	if condition:
		print("PASS: ", label)
	else:
		push_error("FAIL: " + label)
		failures += 1

func actor(group: String) -> Node3D:
	var node := Node3D.new()
	node.add_to_group(group, true)
	var health = load("res://scripts/components/HealthComponent.cs").new()
	health.name = "Health"
	node.add_child(health)
	health.owner = node
	return node

func run_tests():
	var level := Node3D.new()
	var initial = actor("enemy")
	level.add_child(initial)
	var player = actor("player")
	level.add_child(player)
	var prototype = actor("enemy")
	var packed := PackedScene.new()
	check(packed.pack(prototype) == OK, "fixture packed")
	prototype.free()
	var spawner = load("res://scripts/level/EnemySpawner.cs").new()
	spawner.EnemyScene = packed
	spawner.PerPoint = 2
	level.add_child(spawner)
	var mission = load("res://scripts/missions/Mission.cs").new()
	var purge = load("res://scripts/missions/PurgeObjective.cs").new()
	mission.add_child(purge)
	level.add_child(mission)
	root.add_child(level)
	check(mission.TotalEnemies == 1 and mission.RemainingEnemies == 1, "initial roster")
	check(mission.PendingAmbushes == 1 and not mission.ExitOpen, "pending ambush locks exit")
	mission.Complete()
	check(not mission.IsOver, "cannot complete locked mission")
	initial.get_node("Health").TakeDamage(1000, true)
	check(mission.Kills == 1 and not purge.IsComplete and not mission.ExitOpen, "initial kill does not finish purge")
	spawner.Activate()
	check(mission.TotalEnemies == 3 and mission.RemainingEnemies == 2 and mission.PendingAmbushes == 0, "spawned wave registered")
	check(not mission.ExitOpen, "living wave locks exit")
	spawner.Activate()
	check(mission.TotalEnemies == 3, "one-shot activation does not duplicate wave")
	for enemy in get_nodes_in_group("enemy"):
		if enemy == initial:
			continue
		spawner.emit_signal("Spawned", enemy)
		enemy.get_node("Health").TakeDamage(1000, true)
		enemy.get_node("Health").emit_signal("Died")
	check(mission.Kills == 3 and mission.RemainingEnemies == 0, "unique deaths counted once")
	check(purge.IsComplete and mission.ExitOpen, "purge and exit unlock after final kill")
	mission.Complete()
	check(mission.IsOver, "unlocked mission completes")
	level.free()

	var empty_level := Node3D.new()
	empty_level.add_child(actor("player"))
	var empty_mission = load("res://scripts/missions/Mission.cs").new()
	empty_mission.add_child(load("res://scripts/missions/PurgeObjective.cs").new())
	empty_level.add_child(empty_mission)
	root.add_child(empty_level)
	check(empty_mission.ExitOpen, "empty purge without ambush completes")
	empty_level.free()
	packed = null
	print("Mission regression failures: ", failures)
	quit(0 if failures == 0 else 1)
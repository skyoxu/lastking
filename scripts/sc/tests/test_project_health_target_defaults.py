import sys
import unittest
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[2] / 'python'))

import project_health_knowledge as phk


class ProjectHealthTargetDefaultsTests(unittest.TestCase):
    def test_lastking_business_defaults_survive_upstream_alignment(self):
        binding = phk.DEFAULT_CONFIG['task_scene_bindings'][0]
        self.assertEqual(binding['task_id'], 54)
        self.assertEqual(binding['scene'], 'Game.Godot/Scenes/Screens/BattleMapScreen.tscn')
        self.assertEqual(binding['script'], 'Game.Godot/Scripts/Screens/BattleMapScreen.gd')
        self.assertEqual(binding['witness'], 'func _initialize_battle_screen() -> void:')
        self.assertEqual(phk.DEFAULT_CONFIG['query_aliases']['地图'], ['BattleMap'])
        self.assertEqual(phk.DEFAULT_CONFIG['query_aliases']['建筑'], ['Building'])


if __name__ == '__main__':
    unittest.main()

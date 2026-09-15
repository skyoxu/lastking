import json
import sys
import tempfile
import unittest
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[2] / 'python'))

from chapter6_knowledge import _capture_element_manifest


class ProjectHealthChapter6CaptureTests(unittest.TestCase):
    def test_capture_emits_elements_and_non_blocking_gaps(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            latest = root / 'logs/ci/project-health-knowledge/latest.json'
            latest.parent.mkdir(parents=True)
            latest.write_text(json.dumps({
                'revision': 'a' * 40,
                'scene_graph': {'nodes': {
                    'Game.Godot/Scenes/Screens/BattleMapScreen.tscn': {
                        'nodes': [{'name': 'Map', 'parent': '.', 'type': 'Control'}],
                        'functional_summary': {'scripts': ['Game.Godot/Scripts/Screens/BattleMapScreen.gd'], 'events': []}
                    }
                }}
            }), encoding='utf-8')
            links = root / 'docs/knowledge/generated/task-resource-links.json'
            links.parent.mkdir(parents=True)
            links.write_text(json.dumps({'generated': [{
                'task_id': 54,
                'path': 'Game.Godot/Scenes/Screens/BattleMapScreen.tscn',
                'kind': 'scene',
                'confidence': 'confirmed',
                'evidence': [{'focus': 'core', 'evidence_kind': 'task_scene_binding'}]
            }]}), encoding='utf-8')

            result = _capture_element_manifest(root, '54')
            payload = json.loads((root / result['path']).read_text(encoding='utf-8'))

            self.assertFalse(result['blocking'])
            self.assertEqual(payload['task_id'], '54')
            self.assertEqual(payload['elements'][0]['status'], 'verified')
            self.assertEqual(payload['elements'][0]['scripts'], ['Game.Godot/Scripts/Screens/BattleMapScreen.gd'])
            self.assertEqual(payload['documentation_gaps'], [])


if __name__ == '__main__':
    unittest.main()

import sys
import unittest
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[2] / 'python'))

from _godot_scene_graph import build_scene_graph


class GodotSceneGraphAlignmentTests(unittest.TestCase):
    def test_main_and_packed_child_are_reachable(self):
        graph = build_scene_graph({
            'project.godot': '[application]\nrun/main_scene="res://Main.tscn"\n',
            'Main.tscn': '[gd_scene]\n[ext_resource type="PackedScene" path="res://Child.tscn" id="1"]\n[node name="Main" type="Node"]\n[node name="Child" parent="." instance=ExtResource("1")]\n',
            'Child.tscn': '[gd_scene]\n[node name="Child" type="Control"]\n',
        })
        self.assertEqual(graph['nodes']['Main.tscn']['classification'], 'confirmed-reachable')
        self.assertEqual(graph['nodes']['Child.tscn']['classification'], 'confirmed-reachable')

    def test_literal_only_route_remains_possible(self):
        graph = build_scene_graph({
            'project.godot': '[application]\nrun/main_scene="res://Main.tscn"\n',
            'Main.tscn': '[gd_scene]\n[ext_resource path="res://Main.gd" type="Script" id="1"]\n[node name="Main" type="Node"]\nscript = ExtResource("1")\n',
            'Main.gd': 'const NEXT = "res://Other.tscn"\n',
            'Other.tscn': '[gd_scene]\n[node name="Other" type="Node"]\n',
        })
        edge = next(edge for edge in graph['edges'] if edge.get('kind') == 'script-reference')
        self.assertEqual(edge['evidence_level'], 'possible')
        self.assertEqual(graph['nodes']['Other.tscn']['classification'], 'unreachable-candidate')

    def test_explicit_switch_is_effective(self):
        graph = build_scene_graph({
            'project.godot': '[application]\nrun/main_scene="res://Main.tscn"\n',
            'Main.tscn': '[gd_scene]\n[ext_resource path="res://Main.gd" type="Script" id="1"]\n[node name="Main" type="Node"]\nscript = ExtResource("1")\n',
            'Main.gd': 'func go(): change_scene_to_file("res://Other.tscn")\n',
            'Other.tscn': '[gd_scene]\n[node name="Other" type="Node"]\n',
        })
        edge = next(edge for edge in graph['edges'] if edge.get('kind') == 'script-reference')
        self.assertEqual(edge['evidence_level'], 'effective')
        self.assertEqual(graph['nodes']['Other.tscn']['classification'], 'confirmed-reachable')


if __name__ == '__main__':
    unittest.main()

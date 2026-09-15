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

    def test_cycle_is_bounded_and_reported(self):
        graph = build_scene_graph({
            'project.godot': '[application]\nrun/main_scene="res://A.tscn"\n',
            'A.tscn': '[gd_scene]\n[ext_resource type="PackedScene" path="res://B.tscn" id="1"]\n[node name="A" type="Node"]\n[node name="B" parent="." instance=ExtResource("1")]\n',
            'B.tscn': '[gd_scene]\n[ext_resource type="PackedScene" path="res://A.tscn" id="1"]\n[node name="B" type="Node"]\n[node name="A" parent="." instance=ExtResource("1")]\n',
        })
        self.assertEqual(len(graph['nodes']), 2)
        self.assertTrue(any(item['kind'] == 'cycle' for item in graph['diagnostics']))

    def test_dynamic_load_is_unknown_and_does_not_confirm_scene(self):
        graph = build_scene_graph({
            'project.godot': '',
            'Unused.tscn': '[gd_scene]\n[node name="Unused" type="Node"]\n',
            'Loader.gd': 'var scene = load(scene_path)\n',
        })
        self.assertEqual(graph['nodes']['Unused.tscn']['classification'], 'unreachable-candidate')
        self.assertTrue(any(item['classification'] == 'dynamic-unknown' for item in graph['code_references']))

    def test_event_route_is_effective(self):
        graph = build_scene_graph({
            'project.godot': '[application]\nrun/main_scene="res://Main.tscn"\n',
            'Main.tscn': '[gd_scene]\n[ext_resource path="res://Menu.tscn" type="PackedScene" id="1"]\n[node name="Main" type="Node"]\n[node name="Menu" parent="." instance=ExtResource("1")]\n',
            'Menu.tscn': '[gd_scene]\n[ext_resource path="res://Menu.gd" type="Script" id="1"]\n[node name="Menu" type="Control"]\nscript = ExtResource("1")\n',
            'Menu.gd': 'func start():\n    EventBus.PublishSimple("ui.menu.start", "ui", "{}")\n',
            'Controller.gd': 'func handle(type):\n    if type == "ui.menu.start":\n        change_scene_to_file("res://Difficulty.tscn")\n',
            'Difficulty.tscn': '[gd_scene]\n[node name="Difficulty" type="Control"]\n',
        })
        route = next(edge for edge in graph['edges'] if edge.get('kind') == 'event-route')
        self.assertEqual(route['source'], 'Menu.tscn')
        self.assertEqual(route['target'], 'Difficulty.tscn')
        self.assertEqual(route['evidence_level'], 'effective')
        self.assertEqual(graph['nodes']['Difficulty.tscn']['classification'], 'confirmed-reachable')

    def test_controller_route_is_effective(self):
        graph = build_scene_graph({
            'project.godot': '[application]\nrun/main_scene="res://Main.tscn"\n',
            'Main.tscn': '[gd_scene]\n[ext_resource path="res://Main.gd" type="Script" id="1"]\n[node name="Main" type="Node"]\nscript = ExtResource("1")\n',
            'Main.gd': 'const COMBAT = "res://Combat.tscn"\nconst SHOP = "res://Shop.tscn"\nfunc StartRoute(kind):\n    var destination := ResolveRoute(kind)\n    _switch_to(nav, destination)\nfunc ResolveRoute(kind):\n    if kind == "combat":\n        return COMBAT\n    if kind == "shop":\n        return SHOP\n',
            'Map.tscn': '[gd_scene]\n[ext_resource path="res://Map.cs" type="Script" id="1"]\n[node name="Map" type="Control"]\nscript = ExtResource("1")\n',
            'Map.cs': 'void Start() { main.Call("StartRoute", "combat"); }\n',
            'Combat.tscn': '[gd_scene]\n[node name="Combat" type="Control"]\n',
            'Shop.tscn': '[gd_scene]\n[node name="Shop" type="Control"]\n',
        })
        targets = {edge['target'] for edge in graph['edges'] if edge.get('source') == 'Map.tscn' and edge.get('kind') == 'controller-route'}
        self.assertEqual(targets, {'Combat.tscn', 'Shop.tscn'})
        self.assertTrue(all(edge['evidence_level'] == 'effective' for edge in graph['edges'] if edge.get('kind') == 'controller-route'))

    def test_nested_project_resolves_plugin_res_paths(self):
        graph = build_scene_graph({
            'project.godot': '[application]\nrun/main_scene="res://Main.tscn"\n',
            'Main.tscn': '[gd_scene]\n[ext_resource path="res://Tests.Godot/tests/Smoke.tscn" type="PackedScene" id="1"]\n[node name="Main" type="Node"]\n[node name="Smoke" parent="." instance=ExtResource("1")]\n',
            'Tests.Godot/project.godot': '[application]\n',
            'Tests.Godot/tests/Smoke.tscn': '[gd_scene]\n[ext_resource path="res://addons/gdUnit4/src/ui/parts/InspectorTreePanel.tscn" type="PackedScene" id="1"]\n[node name="Smoke" type="Node"]\n[node name="Inspector" parent="." instance=ExtResource("1")]\n',
            'Tests.Godot/addons/gdUnit4/src/ui/parts/InspectorTreePanel.tscn': '[gd_scene]\n[node name="InspectorTreePanel" type="Control"]\n',
        })
        scene = graph['nodes']['Tests.Godot/tests/Smoke.tscn']
        expected = 'Tests.Godot/addons/gdUnit4/src/ui/parts/InspectorTreePanel.tscn'
        self.assertEqual(scene['external_resources']['1'], expected)
        self.assertEqual(scene['nodes'][1]['instance'], expected)

    def test_parse_error_is_preserved(self):
        graph = build_scene_graph({
            'project.godot': '',
            'Broken.tscn': '[node name="Broken" type="Node"]',
        })
        node = graph['nodes']['Broken.tscn']
        self.assertEqual(node['classification'], 'unreachable-candidate')
        self.assertIn('parse_error', node)

    def test_static_config_and_asset_references_are_preserved(self):
        graph = build_scene_graph({
            'project.godot': '[application]\nrun/main_scene="res://Main.tscn"\n',
            'Main.tscn': '[gd_scene]\n[ext_resource path="res://Main.gd" type="Script" id="1"]\n[node name="Main" type="Node"]\nscript = ExtResource("1")\n',
            'Main.gd': 'FileAccess.open("res://Game.Core/Data/config.json", FileAccess.READ)\nload("res://Game.Godot/Assets/icon.png")\n',
            'Game.Core/Data/config.json': '{}',
            'Game.Godot/Assets/icon.png': '',
        })
        refs = graph['code_references']
        self.assertTrue(any(item.get('target') == 'Game.Core/Data/config.json' and item.get('kind') == 'config-reference' for item in refs))
        self.assertTrue(any(item.get('target') == 'Game.Godot/Assets/icon.png' and item.get('kind') == 'asset-reference' for item in refs))


if __name__ == '__main__':
    unittest.main()

"""Real Git snapshots and separate workspace evidence (ADR-0035)."""
import json
import subprocess
import sys
import tempfile
import time
import unittest
from pathlib import Path
from unittest.mock import MagicMock, patch

sys.path.insert(0, str(Path(__file__).resolve().parents[3] / 'scripts/python'))
from _project_health_runtime_snapshot import _is_gdunit_import_cache, prepare_snapshot
from project_health_runtime import verify
from project_health_knowledge import base_dir, write_json, apply_runtime_results


class SnapshotTests(unittest.TestCase):
    def make_runtime_repository(self, root):
        subprocess.run(['git', 'init', '-b', 'main'], cwd=root, check=True, capture_output=True)
        subprocess.run(['git', 'config', 'user.email', 'test@example.invalid'], cwd=root, check=True)
        subprocess.run(['git', 'config', 'user.name', 'Test'], cwd=root, check=True)
        excluded = root / 'Tests.Godot/addons/gdUnit4/cache.svg.import'
        plugin_file = root / 'Tests.Godot/addons/gdUnit4/plugin.gd'
        project_import = root / 'Tests.Godot/project.svg.import'
        for path, content in ((excluded, 'generated'), (plugin_file, 'plugin'), (project_import, 'project')):
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_text(content, encoding='utf-8')
        subprocess.run(['git', 'add', '.'], cwd=root, check=True, capture_output=True)
        subprocess.run(['git', 'commit', '-m', 'initial'], cwd=root, check=True, capture_output=True)
        revision = subprocess.check_output(['git', 'rev-parse', 'HEAD'], cwd=root, text=True).strip()
        return revision, excluded, plugin_file, project_import

    def make_runtime_state(self, root, revision):
        write_json(base_dir(root) / 'latest.json', {'revision': revision, 'sources': {}, 'tasks': []})

    def passing_result(self, task, revision):
        return {'task_id': task['taskmaster_id'], 'source_revision': revision, 'status': 'passed',
                'test_refs': task['runtime_test_refs'], 'scenes': [], 'started_at': 's', 'finished_at': 'f',
                'evidence_path': 'logs/ci/project-health-knowledge/runtime/task.json'}

    def test_excluded_import_paths_still_reject_symlinks(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            subprocess.run(['git', 'init', '-b', 'main'], cwd=root, check=True, capture_output=True)
            subprocess.run(['git', 'config', 'user.email', 'test@example.invalid'], cwd=root, check=True)
            subprocess.run(['git', 'config', 'user.name', 'Test'], cwd=root, check=True)
            cache = root / 'Tests.Godot/addons/gdUnit4/cache.svg.import'
            cache.parent.mkdir(parents=True)
            cache.write_text('generated', encoding='utf-8')
            subprocess.run(['git', 'add', '.'], cwd=root, check=True, capture_output=True)
            subprocess.run(['git', 'commit', '-m', 'initial'], cwd=root, check=True, capture_output=True)
            revision = subprocess.check_output(['git', 'rev-parse', 'HEAD'], cwd=root, text=True).strip()
            (root / '.git/info/exclude').write_text('logs/\n', encoding='utf-8')

            with patch('_project_health_runtime_snapshot.stat.S_ISLNK', return_value=True):
                with self.assertRaisesRegex(ValueError, 'tracked symlinks'):
                    prepare_snapshot(root, root / 'logs/main/source', revision, 'main', time.monotonic() + 30)
            source = MagicMock()
            source.is_symlink.return_value = True
            with patch('_project_health_runtime_snapshot.safe_file', return_value=source):
                with self.assertRaisesRegex(ValueError, 'workspace symlinks'):
                    prepare_snapshot(root, root / 'logs/workspace/source', revision, 'workspace', time.monotonic() + 30)

    def test_failed_report_exposes_assertion_counts(self):
        import project_health_runtime as runtime
        with tempfile.TemporaryDirectory() as tmp:
            report = Path(tmp)
            write_json(report / 'run-summary.json', {'results': {'tests': 87, 'failures': 2, 'errors': 0}})
            self.assertTrue(hasattr(runtime, '_report_counts'))
            self.assertEqual(runtime._report_counts(report), {'tests': 87, 'failures': 2, 'errors': 0})
            self.assertEqual(runtime._report_counts(report / 'missing'), {})

    def test_main_uses_commit_and_workspace_uses_dirty_content(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            def git(*args):
                return subprocess.check_output(['git', '-C', str(root), *args], stderr=subprocess.DEVNULL).decode().strip()
            git('init', '-b', 'main')
            source = root / 'game.txt'
            source.write_text('committed', encoding='utf-8')
            git('add', '.')
            git('-c', 'user.name=Test', '-c', 'user.email=test@example.invalid', 'commit', '-m', 'initial')
            revision = git('rev-parse', 'HEAD')
            source.write_text('edited', encoding='utf-8')
            (root / 'new.txt').write_text('new', encoding='utf-8')
            baseline = git('status', '--porcelain')
            # Output must not become an untracked input in the workspace fixture.
            (root / '.git/info/exclude').write_text('logs/\n', encoding='utf-8')
            main = root / 'logs/main/source'
            work = root / 'logs/workspace/source'
            prepare_snapshot(root, main, revision, 'main', time.monotonic() + 30)
            self.assertEqual((main / 'game.txt').read_text(), 'committed')
            self.assertFalse((main / 'new.txt').exists())
            manifest = prepare_snapshot(root, work, revision, 'workspace', time.monotonic() + 30)
            self.assertEqual((work / 'game.txt').read_text(), 'edited')
            self.assertEqual((work / 'new.txt').read_text(), 'new')
            self.assertTrue(manifest['source_revision'].startswith('workspace:'))
            self.assertEqual(git('status', '--porcelain'), baseline)

    def test_snapshots_exclude_only_gdunit_import_caches(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            revision, excluded, plugin_file, project_import = self.make_runtime_repository(root)
            for mode in ('main', 'workspace'):
                snapshot = root / f'logs/{mode}/source'
                manifest = prepare_snapshot(root, snapshot, revision, mode, time.monotonic() + 30)
                excluded_name = excluded.relative_to(root).as_posix()
                plugin_name = plugin_file.relative_to(root).as_posix()
                project_import_name = project_import.relative_to(root).as_posix()
                self.assertFalse((snapshot / excluded_name).exists())
                self.assertNotIn(excluded_name, manifest['files'])
                self.assertEqual((snapshot / plugin_name).read_text(encoding='utf-8'), 'plugin')
                self.assertIn(plugin_name, manifest['files'])
                self.assertEqual((snapshot / project_import_name).read_text(encoding='utf-8'), 'project')
                self.assertIn(project_import_name, manifest['files'])

    def test_gdunit_import_cache_predicate_does_not_classify_traversal_paths(self):
        self.assertFalse(_is_gdunit_import_cache('Tests.Godot/addons/gdUnit4/../../evil.import'))

    def test_main_verification_remains_verified_when_only_excluded_cache_regenerates(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            revision, excluded, _, _ = self.make_runtime_repository(root)
            self.make_runtime_state(root, revision)
            task = {'taskmaster_id': '18', 'runtime_test_refs': ['Tests.Godot/test.gd'], 'static_godot': {}}

            def regenerate_cache(_, running_task, __, ___, source_revision):
                (Path(running_task['_execution_root']) / excluded.relative_to(root)).write_text('regenerated', encoding='utf-8')
                return self.passing_result(running_task, source_revision)

            with patch('project_health_runtime._gameplay_tasks', return_value=[task]), \
                 patch('project_health_runtime._run_task', side_effect=regenerate_cache):
                result = verify(root, 'godot.exe', 10, task_id='18', mode='main')
            self.assertTrue(result['tasks'][0]['runtime_verified'])
            self.assertEqual(result['tasks'][0]['status'], 'passed')

    def test_main_verification_becomes_unverified_when_retained_input_changes(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            revision, _, plugin_file, _ = self.make_runtime_repository(root)
            self.make_runtime_state(root, revision)
            task = {'taskmaster_id': '18', 'runtime_test_refs': ['Tests.Godot/test.gd'], 'static_godot': {}}

            def change_retained_input(_, running_task, __, ___, source_revision):
                target = Path(running_task['_execution_root']) / plugin_file.relative_to(root)
                target.write_text('changed', encoding='utf-8')
                return self.passing_result(running_task, source_revision)

            with patch('project_health_runtime._gameplay_tasks', return_value=[task]), \
                 patch('project_health_runtime._run_task', side_effect=change_retained_input):
                result = verify(root, 'godot.exe', 10, task_id='18', mode='main')
            self.assertFalse(result['tasks'][0]['runtime_verified'])
            self.assertEqual(result['tasks'][0]['status'], 'runtime_unverified')

    def test_main_verification_becomes_unverified_when_project_import_changes(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            revision, _, _, project_import = self.make_runtime_repository(root)
            self.make_runtime_state(root, revision)
            task = {'taskmaster_id': '18', 'runtime_test_refs': ['Tests.Godot/test.gd'], 'static_godot': {}}

            def change_project_import(_, running_task, __, ___, source_revision):
                target = Path(running_task['_execution_root']) / project_import.relative_to(root)
                target.write_text('changed', encoding='utf-8')
                return self.passing_result(running_task, source_revision)

            with patch('project_health_runtime._gameplay_tasks', return_value=[task]), \
                 patch('project_health_runtime._run_task', side_effect=change_project_import):
                result = verify(root, 'godot.exe', 10, task_id='18', mode='main')
            self.assertFalse(result['tasks'][0]['runtime_verified'])
            self.assertEqual(result['tasks'][0]['status'], 'runtime_unverified')

    def test_workspace_pass_does_not_replace_main_evidence(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            ref = 'Tests.Godot/tests/example.gd'
            revision = 'a' * 40
            write_json(base_dir(root) / 'latest.json', {'revision': revision, 'sources': {
                '.taskmaster/tasks/tasks_gameplay.json': json.dumps([{'taskmaster_id': 18, 'test_refs': [ref]}]), ref: 'pass'},
                'tasks': [{'task': {'id': 18}, 'godot': {}}]})
            main_index = {'source_revision': revision, 'tasks': []}
            write_json(base_dir(root) / 'runtime/latest.json', main_index)
            evidence = {'task_id': '18', 'source_revision': 'workspace:hash', 'status': 'passed',
                        'test_refs': [ref], 'scenes': [], 'started_at': 's', 'finished_at': 'f',
                        'evidence_path': 'logs/ci/project-health-knowledge/runtime/task.json'}
            with patch('project_health_runtime.prepare_snapshot', return_value={'source_revision': 'workspace:hash', 'files': {}}), patch('project_health_runtime._run_task', return_value=evidence):
                result = verify(root, 'godot.exe', 10, task_id='18', mode='workspace')
            self.assertFalse(result['tasks'][0]['runtime_verified'])
            self.assertTrue(result['tasks'][0]['workspace_verified'])
            self.assertEqual(json.loads((base_dir(root) / 'runtime/latest.json').read_text()), main_index)
            self.assertFalse((base_dir(root) / 'runtime/batch.lock').exists())
            state = {'revision': revision, 'tasks': [{'task': {'id': 18}, 'godot': {'status': 'candidate', 'runtime_verified': False}}]}
            apply_runtime_results(root, state)
            self.assertFalse(state['tasks'][0]['godot']['runtime_verified'])
            self.assertEqual(state['tasks'][0]['godot']['workspace_evidence']['status'], 'passed')


if __name__ == '__main__':
    unittest.main()

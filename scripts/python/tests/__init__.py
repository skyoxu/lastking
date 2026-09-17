"""Shared test-package setup for deterministic platform behavior."""
from __future__ import annotations

import os
import tempfile
from pathlib import Path


if os.name == "nt":
    # GitHub-hosted Windows runners may expose TEMP through an 8.3 alias such
    # as C:\\Users\\RUNNER~1. Cross-process path-containment tests can then
    # observe different short/long spellings of the same temporary repository.
    # Keep production alias handling under its dedicated tests, but make the
    # ordinary test fixture root deterministic before any TemporaryDirectory
    # instances are created.
    tempfile.tempdir = str(Path(tempfile.gettempdir()).resolve())

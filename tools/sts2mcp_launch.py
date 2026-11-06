"""Launcher for the STS2 Modding MCP server (Python 3.12 embeddable runtime).

The embeddable CPython uses python312._pth, which pins sys.path and ignores
PYTHONPATH, so put the vendored package on sys.path here before delegating to
its own run.py entry point.
"""
import runpy
import sys

VENDOR = r"D:\STS2\vendor\sts2-modding-mcp"
ENTRY = VENDOR + r"\run.py"

sys.path.insert(0, VENDOR)
sys.argv = [ENTRY]
runpy.run_path(ENTRY, run_name="__main__")

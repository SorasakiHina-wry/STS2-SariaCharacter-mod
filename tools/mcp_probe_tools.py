"""Exercise a few read-only MCP tools to confirm the server is fully wired."""
import json
import os
import subprocess
import sys
import threading
import time

PY = r"D:\STS2\.python312\python.exe"
ENV = dict(os.environ, TMP=r"D:\STS2\.tmp", TEMP=r"D:\STS2\.tmp",
           PYTHONUTF8="1", PYTHONIOENCODING="utf-8")

proc = subprocess.Popen(
    [PY, r"D:\STS2\tools\sts2mcp_launch.py"],
    stdin=subprocess.PIPE, stdout=subprocess.PIPE, stderr=subprocess.PIPE,
    env=ENV, cwd=r"D:\STS2", text=True, encoding="utf-8", errors="replace", bufsize=1,
)
responses = {}


def reader():
    for line in proc.stdout:
        line = line.strip()
        if not line:
            continue
        try:
            msg = json.loads(line)
        except json.JSONDecodeError:
            continue
        if "id" in msg:
            responses[msg["id"]] = msg


threading.Thread(target=reader, daemon=True).start()


def send(obj):
    proc.stdin.write(json.dumps(obj) + "\n")
    proc.stdin.flush()


def wait(i, timeout=120):
    end = time.time() + timeout
    while time.time() < end:
        if i in responses:
            return responses[i]
        if proc.poll() is not None:
            return None
        time.sleep(0.1)
    return None


send({"jsonrpc": "2.0", "id": 1, "method": "initialize",
      "params": {"protocolVersion": "2024-11-05", "capabilities": {},
                 "clientInfo": {"name": "probe", "version": "0"}}})
wait(1)
send({"jsonrpc": "2.0", "method": "notifications/initialized"})

calls = [
    ("list_entities", {"type": "card", "limit": 3}),
    ("search_game_code", {"query": "IsPlayPhase", "max_results": 3}),
    ("get_setup_status", {}),
]
for idx, (name, args) in enumerate(calls, start=2):
    send({"jsonrpc": "2.0", "id": idx, "method": "tools/call",
          "params": {"name": name, "arguments": args}})
    r = wait(idx, 180)
    if not r:
        print(f"{name}: NO RESPONSE")
        continue
    content = r.get("result", {}).get("content", [])
    text = "".join(c.get("text", "") for c in content if c.get("type") == "text")
    print(f"{name}: {len(text)} chars")
    print("  " + text[:300].replace("\n", " | "))

proc.stdin.close()
try:
    proc.wait(timeout=10)
except subprocess.TimeoutExpired:
    proc.kill()
err = proc.stderr.read().strip()
if err:
    print("--- stderr ---")
    print(err[-1000:])

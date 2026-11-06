"""Minimal stdio MCP probe: initialize, list tools, then exit."""
import json
import os
import subprocess
import sys
import threading
import time

PY = r"D:\STS2\.python312\python.exe"
LAUNCH = sys.argv[1:]
ENV = dict(os.environ, TMP=r"D:\STS2\.tmp", TEMP=r"D:\STS2\.tmp")

proc = subprocess.Popen(
    [PY, *LAUNCH],
    stdin=subprocess.PIPE,
    stdout=subprocess.PIPE,
    stderr=subprocess.PIPE,
    env=ENV,
    cwd=r"D:\STS2",
    text=True,
    encoding="utf-8",
    errors="replace",
    bufsize=1,
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


def wait(msg_id, timeout=90):
    end = time.time() + timeout
    while time.time() < end:
        if msg_id in responses:
            return responses[msg_id]
        if proc.poll() is not None:
            return None
        time.sleep(0.1)
    return None


send({"jsonrpc": "2.0", "id": 1, "method": "initialize",
      "params": {"protocolVersion": "2024-11-05", "capabilities": {},
                 "clientInfo": {"name": "probe", "version": "0"}}})
init = wait(1, 120)
if not init:
    print("INIT FAILED; stderr:")
    print(proc.stderr.read()[-4000:])
    proc.kill()
    sys.exit(1)

print("server:", init.get("result", {}).get("serverInfo"))
send({"jsonrpc": "2.0", "method": "notifications/initialized"})
send({"jsonrpc": "2.0", "id": 2, "method": "tools/list", "params": {}})
tools = wait(2, 120)
if not tools:
    print("TOOLS FAILED")
    proc.kill()
    sys.exit(1)

names = [t["name"] for t in tools["result"]["tools"]]
print("tool count:", len(names))
print("sample:", names[:12])

# exercise one read-only tool if present
if "get_game_info" in names:
    send({"jsonrpc": "2.0", "id": 3, "method": "tools/call",
          "params": {"name": "get_game_info", "arguments": {}}})
    r = wait(3, 120)
    if r:
        content = r.get("result", {}).get("content", [])
        text = "".join(c.get("text", "") for c in content if c.get("type") == "text")
        print("get_game_info ok, chars:", len(text))
        print(text[:400].replace("\n", " | "))
    else:
        print("get_game_info: no response")

proc.stdin.close()
try:
    proc.wait(timeout=10)
except subprocess.TimeoutExpired:
    proc.kill()
err = proc.stderr.read()
if err.strip():
    print("--- stderr ---")
    print(err[-2000:])

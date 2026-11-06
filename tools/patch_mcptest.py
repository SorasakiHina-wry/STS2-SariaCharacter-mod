"""One-off port of the bundled MCPTest bridge mod to the v0.111 game API.

Changes:
  1. PowerCmd.Apply<T>(...) gained a leading PlayerChoiceContext parameter.
  2. CombatManager.IsPlayPhase was removed; an extension shim is added.
  3. MerchantRoom.Inventory was replaced by GetLocalInventory().
"""
import re
from pathlib import Path

CODE = Path(r"D:\STS2\vendor\sts2-modding-mcp\test_mod\Code")


def load(path: Path):
    raw = path.read_bytes()
    bom = raw.startswith(b"\xef\xbb\xbf")
    return raw.decode("utf-8-sig"), bom


def save(path: Path, text: str, bom: bool):
    data = text.encode("utf-8")
    if bom:
        data = b"\xef\xbb\xbf" + data
    path.write_bytes(data)


def patch(path: Path, replacements):
    text, bom = load(path)
    original = text
    for pattern, repl in replacements:
        text = re.sub(pattern, repl, text)
    if text != original:
        save(path, text, bom)
        return True
    return False


results = {}

# 1. PowerCmd.Apply<T>(...) -> PowerCmd.Apply<T>(new ThrowingPlayerChoiceContext(), ...)
for name in ("Relics/TenRelics.cs", "Relics/McpTestRelic.cs"):
    p = CODE / name
    changed = patch(p, [
        (r"PowerCmd\.Apply<(\w+)>\(\s*", r"PowerCmd.Apply<\1>(new ThrowingPlayerChoiceContext(), "),
    ])
    results[name] = changed

# 2. IsPlayPhase property -> extension method call
for name in ("BridgeHandler.cs", "ScreenDetector.cs", "BreakpointManager.cs"):
    p = CODE / name
    changed = patch(p, [
        (r"\.IsPlayPhase\b(?!\s*\()", ".IsPlayPhase()"),
    ])
    results[name] = results.get(name, False) or changed

# 3. MerchantRoom.Inventory -> GetLocalInventory()
p = CODE / "BridgeHandler.cs"
text, bom = load(p)
if "merchantRoom.Inventory" in text:
    text = text.replace("merchantRoom.Inventory", "merchantRoom.GetLocalInventory()")
    save(p, text, bom)
    results["BridgeHandler.cs (merchant)"] = True

for k, v in results.items():
    print(f"{'changed' if v else 'unchanged'}: {k}")

"""Assemble workshop.json (plus cover / previews) from a language folder under `steam/`.

`steam/<lang>/` 是某一语言的文案来源；`steam/image.png` 与 `steam/previews/` 与语言无关，共享。

    steam/<lang>/title.txt        -> workshop.json.title
    steam/<lang>/description.txt  -> workshop.json.description
    steam/<lang>/change_note.txt  -> workshop.json.changeNote
    steam/image.png               -> <workspace>/image.png          (封面，< 1 MB)
    steam/previews/*.png          -> <workspace>/previews/*.png     (附加预览图，各自 < 1 MB)

⚠️ 官方 ModUploader 没有语言参数：它只会写 Steam 的**默认语言槽**（Steam 视其为 english）。
   因此 `--lang` 决定的是「这次上传要把哪一语言的文案塞进默认槽」，
   其它语言需要在 Steam 网页条目 Edit → 语言 里单独添加（见 steam/README.md）。

用法：
    python tools/sync_workshop.py                    # 默认 zh_cn
    python tools/sync_workshop.py --lang en
"""

from __future__ import annotations

import argparse
import json
import shutil
from pathlib import Path

ROOT = Path(__file__).resolve().parents[4]          # -> D:\STS2
DEFAULT_WORKSPACE = ROOT / "dist" / "workshop" / "Saria"


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("-w", "--workspace", type=Path, default=DEFAULT_WORKSPACE)
    parser.add_argument("--lang", default="zh_cn", help="steam/ 下的语言目录名，默认 zh_cn")
    args = parser.parse_args()

    workspace: Path = args.workspace
    steam = workspace / "steam"
    config_path = workspace / "workshop.json"
    lang = steam / args.lang

    for required in ("title.txt", "description.txt"):
        if not (lang / required).exists():
            print(f"missing source: {lang / required}")
            available = [p.name for p in steam.iterdir() if p.is_dir() and p.name != "previews"]
            print(f"available languages: {', '.join(sorted(available)) or '(none)'}")
            return 1

    config = json.loads(config_path.read_text(encoding="utf-8")) if config_path.exists() else {}
    config["title"] = (lang / "title.txt").read_text(encoding="utf-8").strip()
    config["description"] = (lang / "description.txt").read_text(encoding="utf-8").strip()

    note = lang / "change_note.txt"
    if note.exists():
        config["changeNote"] = note.read_text(encoding="utf-8").strip()

    config.setdefault("visibility", "private")
    config.setdefault("tags", [])
    config.setdefault("dependencies", [])
    config.setdefault("contentDescriptors", [])

    config_path.write_text(json.dumps(config, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    shutil.copyfile(steam / "image.png", workspace / "image.png")

    previews = steam / "previews"
    if previews.is_dir():
        target = workspace / "previews"
        target.mkdir(parents=True, exist_ok=True)
        for source in previews.glob("*.png"):
            shutil.copyfile(source, target / source.name)

    print(f"语言：{args.lang}")
    print(f"workshop.json 已更新：{config['title']}")
    print(f"封面已复制：{(workspace / 'image.png').stat().st_size / 1024:.0f} KB")
    if previews.is_dir():
        print(f"附加预览图：{len(list(previews.glob('*.png')))} 张")
    print("提醒：ModUploader 只能写 Steam 的默认语言槽；其它语言请在网页条目里添加。")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())

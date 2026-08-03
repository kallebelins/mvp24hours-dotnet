import sys
import xml.etree.ElementTree as ET
from pathlib import Path
from collections import defaultdict


def analyze(path: Path, targets: list[str]):
    root = ET.parse(path).getroot()
    stats = defaultdict(lambda: [0, 0])
    class_stats = defaultdict(lambda: [0, 0])
    for cls in root.findall(".//class"):
        fn = cls.get("filename", "").replace("\\", "/")
        asm = next((t for t in targets if t in fn), None)
        if not asm:
            continue
        for line in cls.findall("./lines/line"):
            if line.get("branch") == "true":
                continue
            hits = int(line.get("hits", "0"))
            stats[asm][1] += 1
            class_stats[fn][1] += 1
            if hits > 0:
                stats[asm][0] += 1
                class_stats[fn][0] += 1
    return stats, class_stats


def main():
    targets = [
        "Mvp24Hours.Infrastructure.RabbitMQ",
        "Mvp24Hours.Infrastructure.Data.MongoDb",
        "Mvp24Hours.Core",
        "Mvp24Hours.Infrastructure.Data.EFCore",
        "Mvp24Hours.Infrastructure.Pipe",
    ]
    base = Path(sys.argv[1] if len(sys.argv) > 1 else "TestResults/coverage-unit-run")
    mapping = {
        "RabbitMQ": "52c4cc3a-d39e-47b3-a451-f75098414a0c",
        "MongoDb": "f1a54197-bdd7-4fd9-9621-241b661f02b0",
        "Core": "f9a0d169-ee95-4f13-ad39-d005998065b0",
        "EFCore": "6e7e9ebb-150f-472f-8490-4dad72a04179",
        "Pipe": "e0775f38-67f3-4826-958a-4b2c1ea52aa8",
    }
    for name, guid in mapping.items():
        path = base / guid / "coverage.cobertura.xml"
        if not path.exists():
            print(f"Missing {path}")
            continue
        stats, class_stats = analyze(path, targets)
        print(f"=== {name} test project ===")
        for asm in targets:
            covered, total = stats.get(asm, [0, 0])
            if total == 0:
                continue
            short = asm.split(".")[-1]
            gap = int(total * 0.755 - covered)
            print(f"  {short}: {100 * covered / total:.1f}% ({covered}/{total}) gap75.5={gap}")
        rows = [(total - covered, covered, total, fn) for fn, (covered, total) in class_stats.items() if total - covered > 0]
        rows.sort(reverse=True)
        print("  Top uncovered:")
        for gap, covered, total, fn in rows[:10]:
            print(f"    {gap:4} {100 * covered / total:5.1f}% {fn.split('/')[-1]}")
        print()


if __name__ == "__main__":
    main()

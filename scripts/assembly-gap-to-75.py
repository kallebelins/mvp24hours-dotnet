import json
import sys

path = sys.argv[1] if len(sys.argv) > 1 else "test-results/coverage-report/Summary.json"
with open(path, encoding="utf-8") as f:
    d = json.load(f)

rows = []
for asm in d["coverage"]["assemblies"]:
    gap = int(asm["coverablelines"] * 0.75 - asm["coveredlines"])
    rows.append((gap, asm["coverage"], asm["coveredlines"], asm["coverablelines"], asm["name"]))

rows.sort(reverse=True)
print("Gap to 75% by assembly:")
for gap, cov, covered, coverable, name in rows:
    short = name.replace("Mvp24Hours.", "")
    print(f"{gap:5}  {cov:5.1f}%  {covered:5}/{coverable:5}  {short}")

summary = d["summary"]
need = int(summary["coverablelines"] * 0.75 - summary["coveredlines"])
print(f"\nTotal gap to 75%: {need} lines ({summary['linecoverage']}% current)")

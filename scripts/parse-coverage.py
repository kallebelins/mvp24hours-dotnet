import json
import sys

path = sys.argv[1] if len(sys.argv) > 1 else "test-results/coverage-report/Summary.json"
with open(path, encoding="utf-8") as f:
    d = json.load(f)

print(f"Total line coverage: {d['summary']['linecoverage']}%")
print(f"Coverable: {d['summary']['coverablelines']}, Covered: {d['summary']['coveredlines']}")
print()
for a in sorted(d["coverage"]["assemblies"], key=lambda x: x["coverage"]):
    gap = int(a["coverablelines"] * 0.95 - a["coveredlines"])
    print(f"{a['name']:45} {a['coverage']:5.1f}%  {a['coveredlines']:6}/{a['coverablelines']:6}  gap_to_95: {max(0,gap):5}")

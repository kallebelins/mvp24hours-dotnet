import json
import sys

path = sys.argv[1] if len(sys.argv) > 1 else "test-results/coverage-report/Summary.json"
with open(path, encoding="utf-8") as f:
    d = json.load(f)

classes = []
for asm in d["coverage"]["assemblies"]:
    asm_name = asm["name"]
    for cls in asm.get("classesinassembly", []):
        coverable = cls.get("coverablelines", 0)
        covered = cls.get("coveredlines", 0)
        cov = cls.get("coverage", 0)
        if coverable >= 50:
            gap = int(coverable * 0.95 - covered)
            classes.append((gap, cov, coverable, covered, asm_name, cls["name"]))

classes.sort(reverse=True)
print("Top 30 classes by gap to 95% (coverable >= 50):")
for gap, cov, coverable, covered, asm, name in classes[:30]:
    print(f"{gap:5}  {cov:5.1f}%  {covered:5}/{coverable:5}  {asm.split('.')[-1]:15}  {name.split('.')[-1]}")

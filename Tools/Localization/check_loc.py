"""LocTable.cs: tekrar eden anahtar var mi, ArtLabels.json'daki her yazinin cevirisi var mi?"""
import re, json, sys, collections
root = sys.argv[1]
s = open(f"{root}/Assets/Scripts/Localization/LocTable.cs", encoding="utf-8").read()
body = s[s.index("new Dictionary"):]
STR = r'"(?:[^"\\]|\\.)*"'
entries = re.findall(r"\{\s*((?:" + STR + r"\s*\+?\s*)+),\s*((?:" + STR + r"\s*\+?\s*)+)\}", body)


def cs(x):
    out = ""
    for p in re.findall(r'"((?:[^"\\]|\\.)*)"', x):
        p = re.sub(r"\\u([0-9a-fA-F]{4})", lambda m: chr(int(m.group(1), 16)), p)
        out += p.replace("\\n", "\n").replace('\\"', '"')
    return out


keys = [cs(a) for a, b in entries]
print(len(keys), "entries")
print("dups:", [k for k, c in collections.Counter(keys).items() if c > 1])
A = json.load(open(f"{root}/Assets/Localization/ArtLabels.json", encoding="utf-8"))
print("missing art:", [l["en"] for sp in A["sprites"] for l in sp["labels"] if l["en"] not in keys])
for extra in sys.argv[2:]:
    src = open(extra, encoding="utf-8").read()
    lits = [cs(m) for m in re.findall(STR, src)]
    print(extra.split("/")[-1], "has:", sum(1 for l in lits if l in keys))

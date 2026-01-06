import json

def search_altimeter_everywhere(obj, path=""):
    results = []
    if isinstance(obj, dict):
        for k, v in obj.items():
            new_path = f"{path}.{k}" if path else k
            if "altimeter" in k.lower():
                results.append((new_path, "KEY", v))
            if isinstance(v, str) and "altimeter" in v.lower():
                results.append((new_path, "VALUE", v))
            results.extend(search_altimeter_everywhere(v, new_path))
    elif isinstance(obj, list):
        for i, item in enumerate(obj):
            results.extend(search_altimeter_everywhere(item, f"{path}[{i}]"))
    return results

data = json.load(open("ZDC.json"))

# Search in autoAtcRules (we'll ignore this per instructions, but let's see what's there)
auto_atc = data.get("autoAtcRules", [])
auto_results = search_altimeter_everywhere(auto_atc, "autoAtcRules")
print(f"In autoAtcRules section: {len(auto_results)} occurrences (IGNORED per instructions)\n")

# Search in facility structure
facility = data.get("facility", {})
facility_results = search_altimeter_everywhere(facility, "facility")

print(f"In facility section: {len(facility_results)} occurrences\n")

# Filter to only starsConfiguration paths
stars_results = [r for r in facility_results if "starsConfiguration" in r[0]]
print(f"In starsConfiguration: {len(stars_results)} occurrences\n")

if stars_results:
    print("Altimeter fields in starsConfiguration:\n")
    print("=" * 80)
    for path, match_type, val in stars_results[:20]:
        print(f"\nPath: {path}")
        print(f"Match Type: {match_type}")
        val_str = str(val)[:200] if len(str(val)) > 200 else str(val)
        print(f"Value: {val_str}")
        print("-" * 80)

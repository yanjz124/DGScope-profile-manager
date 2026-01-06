import json

def search_altimeter(obj, path=""):
    results = []
    if isinstance(obj, dict):
        for k, v in obj.items():
            new_path = f"{path}.{k}" if path else k
            if "altimeter" in k.lower():
                results.append((new_path, v))
            results.extend(search_altimeter(v, new_path))
    elif isinstance(obj, list):
        for i, item in enumerate(obj):
            results.extend(search_altimeter(item, f"{path}[{i}]"))
    return results

data = json.load(open("ZDC.json"))
facility = data.get("facility", {})
child_facilities = facility.get("childFacilities", [])

print(f"Found {len(child_facilities)} child facilities\n")

for fac in child_facilities:
    fac_id = fac.get("id")
    stars = fac.get("starsConfiguration", {})
    if stars:
        alt_fields = search_altimeter(stars)
        if alt_fields:
            print(f"\n{fac_id} has {len(alt_fields)} altimeter field(s):")
            for path, val in alt_fields:
                print(f"  {path}: {str(val)[:100]}")
        else:
            print(f"{fac_id}: No altimeter fields found")

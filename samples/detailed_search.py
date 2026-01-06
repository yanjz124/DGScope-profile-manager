import json

def search_altimeter(obj, path="", depth=0):
    results = []
    if isinstance(obj, dict):
        for k, v in obj.items():
            new_path = f"{path}.{k}" if path else k
            if "altimeter" in k.lower():
                results.append((new_path, v))
            results.extend(search_altimeter(v, new_path, depth+1))
    elif isinstance(obj, list):
        for i, item in enumerate(obj):
            results.extend(search_altimeter(item, f"{path}[{i}]", depth+1))
    return results

data = json.load(open("ZDC.json"))
facility = data.get("facility", {})
child_facilities = facility.get("childFacilities", [])

print("Searching for 'altimeter' fields in TRACON starsConfiguration objects...\n")

acy = [f for f in child_facilities if f.get("id") == "ACY"][0]
pct = [f for f in child_facilities if f.get("id") == "PCT"][0]

print("=" * 60)
print("ACY TRACON - starsConfiguration")
print("=" * 60)
stars = acy.get("starsConfiguration", {})
alt_fields = search_altimeter(stars)
if alt_fields:
    print(f"\nFound {len(alt_fields)} altimeter field(s):\n")
    for path, val in alt_fields:
        val_str = json.dumps(val, indent=2) if isinstance(val, (dict, list)) else str(val)
        print(f"Path: {path}")
        print(f"Value: {val_str}\n")
else:
    print("\nNo fields containing 'altimeter' found in ACY starsConfiguration")

print("\n" + "=" * 60)
print("PCT TRACON - starsConfiguration")
print("=" * 60)
stars = pct.get("starsConfiguration", {})
alt_fields = search_altimeter(stars)
if alt_fields:
    print(f"\nFound {len(alt_fields)} altimeter field(s):\n")
    for path, val in alt_fields:
        val_str = json.dumps(val, indent=2) if isinstance(val, (dict, list)) else str(val)
        print(f"Path: {path}")
        print(f"Value: {val_str}\n")
else:
    print("\nNo fields containing 'altimeter' found in PCT starsConfiguration")

print("\n" + "=" * 60)
print("ALL TRACONs Summary")
print("=" * 60)
for fac in child_facilities:
    fac_id = fac.get("id")
    stars = fac.get("starsConfiguration", {})
    if stars:
        alt_fields = search_altimeter(stars)
        status = f"Found {len(alt_fields)} altimeter field(s)" if alt_fields else "No altimeter fields"
        print(f"{fac_id}: {status}")

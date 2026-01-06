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

print("=" * 80)
print("TRACON STARSCONFIG ALTIMETER FIELD SEARCH")
print("=" * 80)
print(f"\nFile: ZDC.json")
print(f"Total child facilities (TRACONs): {len(child_facilities)}")
print(f"\nFacility IDs: {[f.get('id') for f in child_facilities]}")

print("\n" + "=" * 80)
print("SEARCH RESULTS")
print("=" * 80)

# Check ACY
print("\nACY TRACON:")
acy = [f for f in child_facilities if f.get("id") == "ACY"]
if acy:
    stars = acy[0].get("starsConfiguration", {})
    alt_fields = search_altimeter(stars)
    if alt_fields:
        print(f"  Found {len(alt_fields)} field(s) containing 'altimeter'")
        for path, val in alt_fields:
            print(f"    - {path}")
    else:
        print("  No fields containing 'altimeter' found in starsConfiguration")
else:
    print("  ACY not found")

# Check PCT
print("\nPCT TRACON:")
pct = [f for f in child_facilities if f.get("id") == "PCT"]
if pct:
    stars = pct[0].get("starsConfiguration", {})
    alt_fields = search_altimeter(stars)
    if alt_fields:
        print(f"  Found {len(alt_fields)} field(s) containing 'altimeter'")
        for path, val in alt_fields:
            print(f"    - {path}")
    else:
        print("  No fields containing 'altimeter' found in starsConfiguration")
else:
    print("  PCT not found")

# Check all TRACONs
print("\n" + "=" * 80)
print("ALL TRACONs SUMMARY")
print("=" * 80)
total_with_altimeter = 0
for fac in child_facilities:
    fac_id = fac.get("id")
    stars = fac.get("starsConfiguration", {})
    if stars:
        alt_fields = search_altimeter(stars)
        if alt_fields:
            total_with_altimeter += 1
            print(f"  {fac_id}: {len(alt_fields)} altimeter field(s)")
        else:
            print(f"  {fac_id}: No altimeter fields")

print(f"\nTotal TRACONs with altimeter fields: {total_with_altimeter}/{len(child_facilities)}")

# Note about where altimeter DOES appear
print("\n" + "=" * 80)
print("NOTE")
print("=" * 80)
content = json.dumps(data)
total_count = content.lower().count("altimeter")
print(f"The word 'altimeter' appears {total_count} times in the entire file.")
print("Based on analysis, these occurrences are in the 'autoAtcRules' section,")
print("which was excluded from this search per user instructions.")

import os
import re

forbidden_words = ['tray', 'franchise']
comment_pattern = re.compile(r'//|/\*|\*/')

total_cs_files = 0
comment_violations = []
forbidden_violations = []

for root, dirs, files in os.walk('.'):
    if any(p in root for p in ['bin', 'obj', '.git', '.gemini', 'scratch']):
        continue
    for f in files:
        if f.endswith('.cs'):
            total_cs_files += 1
            fpath = os.path.join(root, f)
            with open(fpath, 'r', encoding='utf-8', errors='ignore') as fp:
                for line_num, line in enumerate(fp, 1):
                    # Check comments
                    # Note: strings might contain // in URLs like http://, so check if not inside quotes
                    stripped = line.strip()
                    # simple check: if line starts with // or has /* or */
                    if stripped.startswith('//') or '/*' in line or '*/' in line:
                        comment_violations.append((fpath, line_num, stripped))
                    elif '//' in line and not ('http://' in line or 'https://' in line):
                        # verify if outside quotes
                        parts = line.split('//')
                        # check if // is inside quotes
                        prefix = parts[0]
                        if prefix.count('"') % 2 == 0:
                            comment_violations.append((fpath, line_num, stripped))

                    # Check forbidden words (case-insensitive word boundary)
                    for word in forbidden_words:
                        if re.search(r'\b' + word + r'\b', line, re.IGNORECASE):
                            forbidden_violations.append((fpath, line_num, word, stripped))

print(f"Scanned {total_cs_files} C# files.")
print(f"Comment violations: {len(comment_violations)}")
for cv in comment_violations[:10]:
    print(f"  {cv[0]}:{cv[1]}: {cv[2]}")

print(f"Forbidden word violations: {len(forbidden_violations)}")
for fv in forbidden_violations[:10]:
    print(f"  {fv[0]}:{fv[1]}: {fv[2]} in: {fv[3]}")

if len(comment_violations) == 0 and len(forbidden_violations) == 0:
    print("ALL POLICY CHECKS PASSED: 0 comments and 0 forbidden terms!")
else:
    print("POLICY VIOLATIONS FOUND!")

import re

with open('Program.cs', 'r', encoding='utf-8') as f:
    text = f.read()

# Replace static void Main(string[] args) with static void Main()
text = re.sub(r'static void Main\(string\[\] args\)\s*\{\s*if \(args\.Length > 0 && args\[0\] == "--verify-refactoring"\)[\s\S]*?using \(Mutex', 'static void Main()\n        {\n            using (Mutex', text)

# Remove RunRefactoringVerification method
text = re.sub(r'private static void RunRefactoringVerification\(\)[\s\S]*?(?=\r?\n\s*\}\s*\r?\n\s*\}\s*\Z)', '', text)

# Clean up closing braces
text = text.rstrip() + '\n    }\n}\n'

with open('Program.cs', 'w', encoding='utf-8') as f:
    f.write(text)

print("Program.cs restored!")

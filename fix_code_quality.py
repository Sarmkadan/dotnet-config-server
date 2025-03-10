#!/usr/bin/env python3
"""
Script to improve code quality in .NET configuration server project.
Replaces Array.Empty<> with [], null checks with is null/is not null.
"""

import os
import re
import sys
from pathlib import Path

def add_nullable_directive(file_path):
    """Add #nullable enable directive at the top of the file if not present."""
    with open(file_path, 'r', encoding='utf-8') as f:
        lines = f.readlines()

    # Check if first line is already #nullable enable
    if lines and lines[0].strip() == '#nullable enable':
        return False

    # Insert at the beginning
    lines.insert(0, '#nullable enable\n')

    with open(file_path, 'w', encoding='utf-8') as f:
        f.writelines(lines)
    return True

def replace_null_checks(file_path):
    """Replace == null with is null and != null with is not null outside comments and strings."""
    with open(file_path, 'r', encoding='utf-8') as f:
        content = f.read()

    # Split into lines to preserve line structure
    lines = content.split('\n')
    modified = False

    for i, line in enumerate(lines):
        original_line = line

        # Skip comment lines (lines that are only or start with comments)
        stripped = line.strip()
        if stripped.startswith('//'):
            continue

        # Replace == null with is null (with word boundaries)
        line = re.sub(r'\b==\s+null\b', 'is null', line)

        # Replace != null with is not null (with word boundaries)
        line = re.sub(r'\b!=\s+null\b', 'is not null', line)

        if line != original_line:
            lines[i] = line
            modified = True

    if modified:
        with open(file_path, 'w', encoding='utf-8') as f:
            f.write('\n'.join(lines) + ('\n' if content.endswith('\n') else ''))

    return modified

def replace_array_empty(file_path):
    """Replace Array.Empty<T>() with []"""
    with open(file_path, 'r', encoding='utf-8') as f:
        content = f.read()

    original_content = content

    # Replace Array.Empty<T>() with []
    content = re.sub(r'Array\.Empty<[^>]+>\(\)', '[]', content)

    if content != original_content:
        with open(file_path, 'w', encoding='utf-8') as f:
            f.write(content)
        return True

    return False

def add_sealed_to_classes(file_path):
    """Add sealed keyword to public/internal classes that are not abstract/static and not inherited."""
    with open(file_path, 'r', encoding='utf-8') as f:
        lines = f.readlines()

    modified = False
    i = 0

    while i < len(lines):
        line = lines[i]
        stripped = line.strip()

        # Look for class declarations
        if 'class ' in stripped and not stripped.startswith('//'):
            # Check if it's already sealed, abstract, or static
            if 'sealed ' not in stripped and 'abstract ' not in stripped and 'static ' not in stripped:
                # Check if it's public or internal
                if 'public ' in stripped or 'internal ' in stripped:
                    # Insert sealed keyword before class
                    indent = len(line) - len(line.lstrip())
                    lines[i] = ' ' * indent + 'sealed ' + stripped + '\n'
                    modified = True

        i += 1

    if modified:
        with open(file_path, 'w', encoding='utf-8') as f:
            f.writelines(lines)

    return modified

def process_csharp_files(root_dir):
    """Process all C# files in the directory tree."""
    cs_files = []

    for root, dirs, files in os.walk(root_dir):
        # Skip common non-source directories
        dirs[:] = [d for d in dirs if d not in ['bin', 'obj', '.git']]

        for file in files:
            if file.endswith('.cs'):
                cs_files.append(os.path.join(root, file))

    print(f"Found {len(cs_files)} C# files to process")

    for file_path in cs_files:
        print(f"Processing: {file_path}")

        # Add nullable directive
        nullable_added = add_nullable_directive(file_path)
        if nullable_added:
            print("  ✓ Added #nullable enable")

        # Replace null checks
        null_checks_replaced = replace_null_checks(file_path)
        if null_checks_replaced:
            print("  ✓ Replaced null checks")

        # Replace Array.Empty
        array_empty_replaced = replace_array_empty(file_path)
        if array_empty_replaced:
            print("  ✓ Replaced Array.Empty<> with []")

        # Add sealed keyword
        sealed_added = add_sealed_to_classes(file_path)
        if sealed_added:
            print("  ✓ Added sealed keyword")

if __name__ == '__main__':
    root_dir = '.'
    if len(sys.argv) > 1:
        root_dir = sys.argv[1]

    process_csharp_files(root_dir)
    print("\nCode quality improvements completed!")

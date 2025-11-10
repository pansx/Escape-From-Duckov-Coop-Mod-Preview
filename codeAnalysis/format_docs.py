#!/usr/bin/env python3
"""
Documentation Formatting Script
Systematically formats all markdown documentation files for consistency
"""

import os
import re
from pathlib import Path

def format_markdown_file(filepath):
    """Format a single markdown file"""
    with open(filepath, 'r', encoding='utf-8') as f:
        content = f.read()
    
    original_content = content
    
    # 1. Standardize heading spacing (ensure blank line before and after headings)
    content = re.sub(r'\n(#{1,6} .+)\n(?!\n)', r'\n\1\n\n', content)
    
    # 2. Ensure consistent colon usage (Chinese colon after labels)
    content = re.sub(r'\*\*([^*]+)\*\*:\s*', r'**\1**：', content)
    content = re.sub(r'\*\*([^*]+)\*\*:\n', r'**\1**：\n', content)
    
    # 3. Add blank line before code blocks
    content = re.sub(r'([^\n])\n```', r'\1\n\n```', content)
    
    # 4. Add blank line after code blocks
    content = re.sub(r'```\n([^`\n])', r'```\n\n\1', content)
    
    # 5. Ensure blank line before lists
    content = re.sub(r'([^\n])\n([-*] )', r'\1\n\n\2', content)
    
    # 6. Ensure blank line after lists (before next paragraph)
    content = re.sub(r'([-*] .+)\n([^-*\n#])', r'\1\n\n\2', content)
    
    # 7. Standardize table formatting (ensure blank lines around tables)
    content = re.sub(r'([^\n])\n(\|.+\|)', r'\1\n\n\2', content)
    content = re.sub(r'(\|.+\|)\n([^|\n])', r'\1\n\n\2', content)
    
    # 8. Remove excessive blank lines (max 2 consecutive)
    content = re.sub(r'\n{4,}', '\n\n\n', content)
    
    # 9. Ensure file ends with single newline
    content = content.rstrip() + '\n'
    
    # Only write if content changed
    if content != original_content:
        with open(filepath, 'w', encoding='utf-8') as f:
            f.write(content)
        return True
    return False

def format_all_docs(root_dir):
    """Format all markdown files in directory tree"""
    root_path = Path(root_dir)
    formatted_count = 0
    total_count = 0
    
    for md_file in root_path.rglob('*.md'):
        # Skip certain directories
        if any(skip in str(md_file) for skip in ['.git', 'node_modules', 'Decompilation']):
            continue
            
        total_count += 1
        try:
            if format_markdown_file(md_file):
                formatted_count += 1
                print(f"✓ Formatted: {md_file.relative_to(root_path)}")
            else:
                print(f"  Unchanged: {md_file.relative_to(root_path)}")
        except Exception as e:
            print(f"✗ Error formatting {md_file}: {e}")
    
    print(f"\n{'='*60}")
    print(f"Formatting complete!")
    print(f"Total files: {total_count}")
    print(f"Formatted: {formatted_count}")
    print(f"Unchanged: {total_count - formatted_count}")
    print(f"{'='*60}")

if __name__ == '__main__':
    # Format all docs in duckovAPI directory
    format_all_docs('codeAnalysis/duckovAPI')
    
    # Also format root level docs
    root_docs = [
        'codeAnalysis/README.md',
        'codeAnalysis/Main_Analysis_Summary.md',
        'codeAnalysis/Net_Analysis_Summary.md',
        'codeAnalysis/Patch_Analysis_Summary.md',
        'codeAnalysis/Task3_Completion_Summary.md'
    ]
    
    print(f"\n{'='*60}")
    print("Formatting root-level documentation...")
    print(f"{'='*60}\n")
    
    for doc in root_docs:
        if os.path.exists(doc):
            try:
                if format_markdown_file(doc):
                    print(f"✓ Formatted: {doc}")
                else:
                    print(f"  Unchanged: {doc}")
            except Exception as e:
                print(f"✗ Error formatting {doc}: {e}")

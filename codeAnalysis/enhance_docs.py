#!/usr/bin/env python3
"""
Documentation Enhancement Script
Adds advanced formatting improvements to markdown files
"""

import os
import re
from pathlib import Path

def enhance_markdown_file(filepath):
    """Enhance a single markdown file with advanced formatting"""
    with open(filepath, 'r', encoding='utf-8') as f:
        content = f.read()
    
    original_content = content
    
    # 1. Improve table formatting - ensure proper alignment
    def format_table(match):
        table = match.group(0)
        lines = table.strip().split('\n')
        if len(lines) < 2:
            return table
        
        # Ensure separator line has proper format
        if len(lines) >= 2 and '|' in lines[1]:
            # Count columns from first line
            cols = len([c for c in lines[0].split('|') if c.strip()])
            # Create proper separator
            separator = '|' + '|'.join(['---' for _ in range(cols)]) + '|'
            lines[1] = separator
        
        return '\n'.join(lines)
    
    # Apply table formatting
    content = re.sub(r'(\|.+\|(?:\n\|.+\|)+)', format_table, content)
    
    # 2. Ensure consistent list formatting
    # Convert "- " to "- " (ensure space after dash)
    content = re.sub(r'\n-([^ \n])', r'\n- \1', content)
    
    # 3. Improve code block language tags
    # Ensure common languages are properly tagged
    content = re.sub(r'```\n(public |private |internal |class |interface |enum |namespace )', r'```csharp\n\1', content)
    content = re.sub(r'```\n(import |export |const |let |var |function |class )', r'```javascript\n\1', content)
    content = re.sub(r'```\n(def |class |import |from )', r'```python\n\1', content)
    
    # 4. Standardize emphasis markers
    # Ensure bold markers are consistent
    content = re.sub(r'\*\*([^*]+)\*\*', r'**\1**', content)
    
    # 5. Improve section headers - ensure proper capitalization for common terms
    replacements = {
        'Api': 'API',
        'Ai': 'AI',
        'Ui': 'UI',
        'Id': 'ID',
        'Rpc': 'RPC',
        'Udp': 'UDP',
        'Tcp': 'TCP',
        'Http': 'HTTP',
        'Json': 'JSON',
        'Xml': 'XML',
        'Sql': 'SQL',
        'Cpu': 'CPU',
        'Gpu': 'GPU',
        'Fps': 'FPS',
        'Npc': 'NPC',
        'Gc': 'GC',
    }
    
    for wrong, correct in replacements.items():
        # Only replace in headings and bold text
        content = re.sub(rf'(#{1,6} [^#\n]*){wrong}([^a-zA-Z])', rf'\1{correct}\2', content)
        content = re.sub(rf'(\*\*[^*]*){wrong}([^a-zA-Z][^*]*\*\*)', rf'\1{correct}\2', content)
    
    # 6. Ensure proper spacing around horizontal rules
    content = re.sub(r'([^\n])\n(---+)\n', r'\1\n\n\2\n\n', content)
    
    # 7. Improve numbered list formatting
    content = re.sub(r'\n(\d+)\. ', r'\n\1. ', content)
    
    # 8. Clean up excessive whitespace in tables
    content = re.sub(r'\|\s{3,}', '| ', content)
    content = re.sub(r'\s{3,}\|', ' |', content)
    
    # 9. Ensure consistent quote formatting
    content = re.sub(r'`([^`]+)`', r'`\1`', content)
    
    # 10. Remove trailing whitespace from lines
    lines = content.split('\n')
    lines = [line.rstrip() for line in lines]
    content = '\n'.join(lines)
    
    # 11. Ensure file ends with exactly one newline
    content = content.rstrip() + '\n'
    
    # Only write if content changed
    if content != original_content:
        with open(filepath, 'w', encoding='utf-8') as f:
            f.write(content)
        return True
    return False

def enhance_all_docs(root_dir):
    """Enhance all markdown files in directory tree"""
    root_path = Path(root_dir)
    enhanced_count = 0
    total_count = 0
    
    for md_file in root_path.rglob('*.md'):
        # Skip certain directories
        if any(skip in str(md_file) for skip in ['.git', 'node_modules', 'Decompilation']):
            continue
            
        total_count += 1
        try:
            if enhance_markdown_file(md_file):
                enhanced_count += 1
                print(f"✓ Enhanced: {md_file.relative_to(root_path)}")
            else:
                print(f"  Unchanged: {md_file.relative_to(root_path)}")
        except Exception as e:
            print(f"✗ Error enhancing {md_file}: {e}")
    
    print(f"\n{'='*60}")
    print(f"Enhancement complete!")
    print(f"Total files: {total_count}")
    print(f"Enhanced: {enhanced_count}")
    print(f"Unchanged: {total_count - enhanced_count}")
    print(f"{'='*60}")

if __name__ == '__main__':
    # Enhance all docs in duckovAPI directory
    enhance_all_docs('codeAnalysis/duckovAPI')
    
    # Also enhance root level docs
    root_docs = [
        'codeAnalysis/README.md',
        'codeAnalysis/Main_Analysis_Summary.md',
        'codeAnalysis/Net_Analysis_Summary.md',
        'codeAnalysis/Patch_Analysis_Summary.md',
        'codeAnalysis/Task3_Completion_Summary.md'
    ]
    
    print(f"\n{'='*60}")
    print("Enhancing root-level documentation...")
    print(f"{'='*60}\n")
    
    for doc in root_docs:
        if os.path.exists(doc):
            try:
                if enhance_markdown_file(doc):
                    print(f"✓ Enhanced: {doc}")
                else:
                    print(f"  Unchanged: {doc}")
            except Exception as e:
                print(f"✗ Error enhancing {doc}: {e}")

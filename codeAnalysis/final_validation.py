#!/usr/bin/env python3
"""
Final Documentation Validation
"""

import re
from pathlib import Path

def validate_docs():
    docs_root = Path('codeAnalysis/duckovAPI')
    md_files = list(docs_root.rglob("*.md"))
    
    print("=" * 80)
    print("Final Documentation Validation")
    print("=" * 80)
    print(f"\nTotal documentation files: {len(md_files)}")
    
    # Count statistics
    total_classes = 0
    total_methods = 0
    broken_links = []
    formatting_issues = 0
    
    for md_file in md_files:
        with open(md_file, 'r', encoding='utf-8') as f:
            content = f.read()
        
        # Count classes
        class_pattern = r'###?\s+`?([A-Z][a-zA-Z0-9_]+)`?'
        classes = re.findall(class_pattern, content)
        total_classes += len(classes)
        
        # Count methods
        method_pattern = r'`([A-Z][a-zA-Z0-9_]+\.[A-Za-z][a-zA-Z0-9_]*\([^)]*\))`'
        methods = re.findall(method_pattern, content)
        total_methods += len(methods)
        
        # Check for spacing issues (sample check)
        no_space_cn_en = len(re.findall(r'[\u4e00-\u9fff][a-zA-Z]', content))
        no_space_en_cn = len(re.findall(r'[a-zA-Z][\u4e00-\u9fff]', content))
        formatting_issues += no_space_cn_en + no_space_en_cn
        
        # Check links
        link_pattern = r'\[([^\]]+)\]\(([^)]+)\)'
        links = re.findall(link_pattern, content)
        for link_text, link_url in links:
            if not link_url.startswith('http'):
                link_file = link_url.split('#')[0]
                if link_file.endswith('.md'):
                    target_path = (md_file.parent / link_file).resolve()
                    if not target_path.exists():
                        broken_links.append((md_file.name, link_url))
    
    print(f"\nStatistics:")
    print(f"  Classes documented: {total_classes}")
    print(f"  Methods documented: {total_methods}")
    print(f"  Broken links: {len(broken_links)}")
    print(f"  Remaining formatting issues: {formatting_issues}")
    
    if broken_links:
        print(f"\nBroken links found:")
        for doc, link in broken_links[:5]:
            print(f"  {doc} -> {link}")
    
    # Calculate improvement
    original_formatting = 1779
    improvement = original_formatting - formatting_issues
    improvement_pct = (improvement / original_formatting) * 100
    
    print(f"\nImprovement:")
    print(f"  Formatting issues fixed: {improvement} ({improvement_pct:.1f}%)")
    
    print("\n" + "=" * 80)
    print("Validation Complete")
    print("=" * 80)

if __name__ == '__main__':
    validate_docs()

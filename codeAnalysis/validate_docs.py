#!/usr/bin/env python3
"""
Documentation Validation Script
Validates markdown files for common formatting issues
"""

import os
import re
from pathlib import Path
from collections import defaultdict

class DocValidator:
    def __init__(self):
        self.issues = defaultdict(list)
        self.stats = {
            'total_files': 0,
            'total_lines': 0,
            'total_headings': 0,
            'total_code_blocks': 0,
            'total_tables': 0,
            'total_lists': 0,
        }
    
    def validate_file(self, filepath):
        """Validate a single markdown file"""
        with open(filepath, 'r', encoding='utf-8') as f:
            content = f.read()
            lines = content.split('\n')
        
        self.stats['total_files'] += 1
        self.stats['total_lines'] += len(lines)
        
        # Check for common issues
        issues = []
        
        # 1. Check for trailing whitespace
        for i, line in enumerate(lines, 1):
            if line != line.rstrip():
                issues.append(f"Line {i}: Trailing whitespace")
        
        # 2. Check for multiple consecutive blank lines
        blank_count = 0
        for i, line in enumerate(lines, 1):
            if not line.strip():
                blank_count += 1
                if blank_count > 2:
                    issues.append(f"Line {i}: More than 2 consecutive blank lines")
            else:
                blank_count = 0
        
        # 3. Count elements
        self.stats['total_headings'] += len(re.findall(r'^#{1,6} ', content, re.MULTILINE))
        self.stats['total_code_blocks'] += len(re.findall(r'```', content)) // 2
        self.stats['total_tables'] += len(re.findall(r'^\|.+\|$', content, re.MULTILINE))
        self.stats['total_lists'] += len(re.findall(r'^[-*] ', content, re.MULTILINE))
        
        # 4. Check for unclosed code blocks
        code_block_count = content.count('```')
        if code_block_count % 2 != 0:
            issues.append("Unclosed code block detected")
        
        # 5. Check for proper heading hierarchy
        headings = re.findall(r'^(#{1,6}) (.+)$', content, re.MULTILINE)
        prev_level = 0
        for i, (hashes, title) in enumerate(headings):
            level = len(hashes)
            if level > prev_level + 1 and prev_level > 0:
                issues.append(f"Heading hierarchy skip: {title} (level {level} after level {prev_level})")
            prev_level = level
        
        # 6. Check for empty headings
        if re.search(r'^#{1,6}\s*$', content, re.MULTILINE):
            issues.append("Empty heading detected")
        
        # 7. Check for broken links (basic check)
        broken_links = re.findall(r'\[([^\]]+)\]\(\)', content)
        if broken_links:
            issues.append(f"Broken links detected: {', '.join(broken_links[:3])}")
        
        return issues
    
    def validate_all(self, root_dir):
        """Validate all markdown files"""
        root_path = Path(root_dir)
        
        for md_file in root_path.rglob('*.md'):
            # Skip certain directories
            if any(skip in str(md_file) for skip in ['.git', 'node_modules', 'Decompilation']):
                continue
            
            try:
                issues = self.validate_file(md_file)
                if issues:
                    self.issues[str(md_file.relative_to(root_path))] = issues
            except Exception as e:
                self.issues[str(md_file.relative_to(root_path))] = [f"Error: {e}"]
    
    def print_report(self):
        """Print validation report"""
        print("="*70)
        print("DOCUMENTATION VALIDATION REPORT")
        print("="*70)
        print()
        
        # Print statistics
        print("📊 Statistics:")
        print(f"  Total files:       {self.stats['total_files']}")
        print(f"  Total lines:       {self.stats['total_lines']:,}")
        print(f"  Total headings:    {self.stats['total_headings']}")
        print(f"  Total code blocks: {self.stats['total_code_blocks']}")
        print(f"  Total tables:      {self.stats['total_tables']}")
        print(f"  Total lists:       {self.stats['total_lists']}")
        print()
        
        # Print issues
        if self.issues:
            print(f"⚠️  Issues found in {len(self.issues)} files:")
            print()
            for filepath, issues in sorted(self.issues.items()):
                print(f"  {filepath}:")
                for issue in issues[:5]:  # Limit to first 5 issues per file
                    print(f"    - {issue}")
                if len(issues) > 5:
                    print(f"    ... and {len(issues) - 5} more issues")
                print()
        else:
            print("✅ No issues found! All documentation is properly formatted.")
        
        print("="*70)
        print(f"Validation complete: {self.stats['total_files']} files checked")
        print("="*70)

if __name__ == '__main__':
    validator = DocValidator()
    
    # Validate duckovAPI directory
    validator.validate_all('codeAnalysis/duckovAPI')
    
    # Validate root docs
    root_docs = [
        'codeAnalysis/README.md',
        'codeAnalysis/Main_Analysis_Summary.md',
        'codeAnalysis/Net_Analysis_Summary.md',
        'codeAnalysis/Patch_Analysis_Summary.md',
        'codeAnalysis/Task3_Completion_Summary.md'
    ]
    
    for doc in root_docs:
        if os.path.exists(doc):
            try:
                issues = validator.validate_file(doc)
                if issues:
                    validator.issues[doc] = issues
            except Exception as e:
                validator.issues[doc] = [f"Error: {e}"]
    
    # Print report
    validator.print_report()

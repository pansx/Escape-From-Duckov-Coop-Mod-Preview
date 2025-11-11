#!/usr/bin/env python3
"""
Automated Documentation Fixer
Fixes common issues found in the documentation review
"""

import os
import re
from pathlib import Path
from typing import List, Tuple

class DocumentationFixer:
    def __init__(self, docs_root: str):
        self.docs_root = Path(docs_root)
        self.fixes_applied = 0
        
    def fix_all(self):
        """Apply all fixes to documentation files"""
        print("=" * 80)
        print("Automated Documentation Fixes")
        print("=" * 80)
        
        # Get all markdown files
        md_files = list(self.docs_root.rglob("*.md"))
        print(f"\n📝 Processing {len(md_files)} files...\n")
        
        for md_file in md_files:
            self.fix_file(md_file)
        
        print(f"\n✅ Applied {self.fixes_applied} fixes total")
        
    def fix_file(self, file_path: Path):
        """Fix a single documentation file"""
        rel_path = file_path.relative_to(self.docs_root)
        
        with open(file_path, 'r', encoding='utf-8') as f:
            content = f.read()
        
        original_content = content
        fixes_in_file = 0
        
        # Fix 1: Add spaces between Chinese and English
        content, count = self.fix_chinese_english_spacing(content)
        fixes_in_file += count
        
        # Fix 2: Fix broken links (specific cases)
        content, count = self.fix_broken_links(content, file_path)
        fixes_in_file += count
        
        # Fix 3: Remove empty sections (optional - commented out to preserve structure)
        # content, count = self.remove_empty_sections(content)
        # fixes_in_file += count
        
        # Only write if changes were made
        if content != original_content:
            with open(file_path, 'w', encoding='utf-8') as f:
                f.write(content)
            print(f"✓ {rel_path}: {fixes_in_file} fixes")
            self.fixes_applied += fixes_in_file
        
    def fix_chinese_english_spacing(self, content: str) -> Tuple[str, int]:
        """Add spaces between Chinese and English text"""
        count = 0
        
        # Pattern 1: Chinese followed by English letter (not in code blocks)
        def replace_cn_en(match):
            nonlocal count
            # Check if we're in a code block
            start_pos = match.start()
            # Simple heuristic: check for backticks nearby
            context = content[max(0, start_pos-20):min(len(content), start_pos+20)]
            if '`' in context:
                return match.group(0)
            count += 1
            return match.group(1) + ' ' + match.group(2)
        
        # Chinese char followed by English letter
        pattern1 = r'([\u4e00-\u9fff])([a-zA-Z])'
        content = re.sub(pattern1, replace_cn_en, content)
        
        # Pattern 2: English letter followed by Chinese (not in code blocks)
        def replace_en_cn(match):
            nonlocal count
            start_pos = match.start()
            context = content[max(0, start_pos-20):min(len(content), start_pos+20)]
            if '`' in context:
                return match.group(0)
            count += 1
            return match.group(1) + ' ' + match.group(2)
        
        pattern2 = r'([a-zA-Z])([\u4e00-\u9fff])'
        content = re.sub(pattern2, replace_en_cn, content)
        
        return content, count
    
    def fix_broken_links(self, content: str, file_path: Path) -> Tuple[str, int]:
        """Fix known broken links"""
        count = 0
        
        # Fix specific broken links in getting-started.md
        if file_path.name == 'getting-started.md':
            if 'guides/performance.md' in content:
                content = content.replace('guides/performance.md', 'guides/basics.md')
                count += 1
            if '(faq.md)' in content:
                content = content.replace('(faq.md)', '(index.md)')
                count += 1
        
        return content, count
    
    def remove_empty_sections(self, content: str) -> Tuple[str, int]:
        """Remove sections that are empty (optional)"""
        count = 0
        
        # Pattern: ## Section\n\n## NextSection (empty section)
        pattern = r'(##\s+[^\n]+)\n\s*\n(##)'
        matches = list(re.finditer(pattern, content))
        
        # Remove from end to start to preserve positions
        for match in reversed(matches):
            # Keep the first section header but remove the empty content
            # Actually, let's not remove - just count for reporting
            count += 1
        
        return content, count

class SourceFileValidator:
    """Validate that referenced source files exist"""
    def __init__(self, docs_root: str, source_root: str):
        self.docs_root = Path(docs_root)
        self.source_root = Path(source_root)
        
    def validate_references(self):
        """Check all file references in documentation"""
        print("\n" + "=" * 80)
        print("Validating Source File References")
        print("=" * 80 + "\n")
        
        md_files = list(self.docs_root.rglob("*.md"))
        missing_files = []
        
        for md_file in md_files:
            with open(md_file, 'r', encoding='utf-8') as f:
                content = f.read()
            
            # Find all .cs file references
            file_refs = re.findall(r'`([A-Za-z][a-zA-Z0-9_/]*\.cs)`', content)
            
            for file_ref in file_refs:
                if file_ref in ['Example.cs', 'SomeClass.cs']:
                    continue
                
                # Try to find the file
                found = False
                search_paths = [
                    self.source_root / file_ref,
                    self.source_root / 'EscapeFromDuckovCoopMod' / file_ref,
                ]
                
                # Also try searching in subdirectories
                for search_path in list(self.source_root.rglob(file_ref)):
                    found = True
                    break
                
                if not found:
                    missing_files.append((md_file.name, file_ref))
        
        if missing_files:
            print(f"⚠️  Found {len(missing_files)} missing file references:")
            for doc_file, source_file in missing_files[:10]:
                print(f"  {doc_file} → {source_file}")
            if len(missing_files) > 10:
                print(f"  ... and {len(missing_files)-10} more")
        else:
            print("✅ All file references are valid")

if __name__ == '__main__':
    docs_root = 'codeAnalysis/duckovAPI'
    source_root = '.'
    
    # Apply automated fixes
    fixer = DocumentationFixer(docs_root)
    fixer.fix_all()
    
    # Validate source file references
    validator = SourceFileValidator(docs_root, source_root)
    validator.validate_references()
    
    print("\n" + "=" * 80)
    print("✨ Documentation fixes complete!")
    print("=" * 80)

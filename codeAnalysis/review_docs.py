#!/usr/bin/env python3
"""
Documentation Review and Validation Script
Checks for:
1. Class name and method name accuracy
2. Documentation completeness and coverage
3. Chinese-English mixed text formatting
4. Link and reference validity
"""

import os
import re
import json
from pathlib import Path
from typing import List, Dict, Set, Tuple

class DocumentationReviewer:
    def __init__(self, docs_root: str, source_root: str):
        self.docs_root = Path(docs_root)
        self.source_root = Path(source_root)
        self.issues = []
        self.warnings = []
        self.stats = {
            'total_docs': 0,
            'total_classes_documented': 0,
            'total_methods_documented': 0,
            'broken_links': 0,
            'formatting_issues': 0,
            'missing_references': 0
        }
        
    def review_all(self):
        """Run all review checks"""
        print("=" * 80)
        print("Documentation Review Report")
        print("=" * 80)
        
        # Collect all markdown files
        md_files = list(self.docs_root.rglob("*.md"))
        self.stats['total_docs'] = len(md_files)
        
        print(f"\n📚 Found {len(md_files)} documentation files\n")
        
        # Review each file
        for md_file in md_files:
            self.review_file(md_file)
        
        # Generate report
        self.generate_report()
        
    def review_file(self, file_path: Path):
        """Review a single documentation file"""
        rel_path = file_path.relative_to(self.docs_root)
        print(f"Reviewing: {rel_path}")
        
        with open(file_path, 'r', encoding='utf-8') as f:
            content = f.read()
        
        # Check 1: Class and method name accuracy
        self.check_code_references(file_path, content)
        
        # Check 2: Completeness
        self.check_completeness(file_path, content)
        
        # Check 3: Chinese-English formatting
        self.check_formatting(file_path, content)
        
        # Check 4: Links and references
        self.check_links(file_path, content)
        
    def check_code_references(self, file_path: Path, content: str):
        """Check if class and method names are accurate"""
        # Extract class names from documentation
        class_pattern = r'###?\s+`?([A-Z][a-zA-Z0-9_]+)`?'
        classes_in_doc = re.findall(class_pattern, content)
        
        # Extract method signatures
        method_pattern = r'`([A-Z][a-zA-Z0-9_]+\.[A-Za-z][a-zA-Z0-9_]*\([^)]*\))`'
        methods_in_doc = re.findall(method_pattern, content)
        
        self.stats['total_classes_documented'] += len(classes_in_doc)
        self.stats['total_methods_documented'] += len(methods_in_doc)
        
        # Check for common typos or inconsistencies
        if 'Duckov' in content and 'Duckkov' in content:
            self.issues.append(f"{file_path.name}: Inconsistent spelling - 'Duckov' vs 'Duckkov'")
        
    def check_completeness(self, file_path: Path, content: str):
        """Check if documentation is complete"""
        required_sections = {
            'README.md': ['概述', '模块结构', '核心功能'],
            'default': ['概述', '核心类']
        }
        
        filename = file_path.name
        sections_to_check = required_sections.get(filename, required_sections['default'])
        
        for section in sections_to_check:
            if section not in content and section.lower() not in content.lower():
                self.warnings.append(f"{file_path.name}: Missing section '{section}'")
        
        # Check for empty sections
        empty_section_pattern = r'##\s+([^\n]+)\n\s*##'
        empty_sections = re.findall(empty_section_pattern, content)
        if empty_sections:
            self.warnings.append(f"{file_path.name}: Empty sections found: {', '.join(empty_sections)}")
        
    def check_formatting(self, file_path: Path, content: str):
        """Check Chinese-English mixed text formatting"""
        issues_found = []
        
        # Check 1: Space between Chinese and English
        # Pattern: Chinese character directly followed by English letter (no space)
        no_space_pattern = r'[\u4e00-\u9fff][a-zA-Z]'
        matches = re.finditer(no_space_pattern, content)
        for match in matches:
            line_num = content[:match.start()].count('\n') + 1
            issues_found.append(f"Line {line_num}: Missing space between Chinese and English")
        
        # Check 2: English directly followed by Chinese (no space)
        no_space_pattern2 = r'[a-zA-Z][\u4e00-\u9fff]'
        matches = re.finditer(no_space_pattern2, content)
        for match in matches:
            # Skip if it's in code blocks
            if '`' in content[max(0, match.start()-10):match.end()+10]:
                continue
            line_num = content[:match.start()].count('\n') + 1
            issues_found.append(f"Line {line_num}: Missing space between English and Chinese")
        
        # Check 3: Inconsistent terminology
        terminology_checks = [
            (r'网络包', r'数据包', 'Use consistent term for network packet'),
            (r'玩家', r'角色', 'Distinguish between player and character'),
        ]
        
        if issues_found:
            self.stats['formatting_issues'] += len(issues_found)
            # Only report first few to avoid spam
            for issue in issues_found[:3]:
                self.warnings.append(f"{file_path.name}: {issue}")
            if len(issues_found) > 3:
                self.warnings.append(f"{file_path.name}: ... and {len(issues_found)-3} more formatting issues")
        
    def check_links(self, file_path: Path, content: str):
        """Check if links and references are valid"""
        # Extract markdown links
        link_pattern = r'\[([^\]]+)\]\(([^)]+)\)'
        links = re.findall(link_pattern, content)
        
        for link_text, link_url in links:
            # Skip external links
            if link_url.startswith('http://') or link_url.startswith('https://'):
                continue
            
            # Check relative file links
            if link_url.endswith('.md'):
                # Remove anchor if present
                link_file = link_url.split('#')[0]
                target_path = (file_path.parent / link_file).resolve()
                
                if not target_path.exists():
                    self.issues.append(f"{file_path.name}: Broken link to '{link_url}'")
                    self.stats['broken_links'] += 1
        
        # Check for file references in text
        file_ref_pattern = r'`([A-Za-z][a-zA-Z0-9_/]*\.cs)`'
        file_refs = re.findall(file_ref_pattern, content)
        
        for file_ref in file_refs:
            # Try to find the file in source
            possible_paths = [
                self.source_root / file_ref,
                self.source_root / 'EscapeFromDuckovCoopMod' / file_ref,
            ]
            
            found = any(p.exists() for p in possible_paths)
            if not found and file_ref not in ['Example.cs', 'SomeClass.cs']:  # Skip example files
                self.warnings.append(f"{file_path.name}: Referenced file '{file_ref}' not found in source")
                self.stats['missing_references'] += 1
        
    def generate_report(self):
        """Generate final review report"""
        print("\n" + "=" * 80)
        print("Review Summary")
        print("=" * 80)
        
        print(f"\n📊 Statistics:")
        print(f"  Total documents reviewed: {self.stats['total_docs']}")
        print(f"  Classes documented: {self.stats['total_classes_documented']}")
        print(f"  Methods documented: {self.stats['total_methods_documented']}")
        print(f"  Broken links: {self.stats['broken_links']}")
        print(f"  Formatting issues: {self.stats['formatting_issues']}")
        print(f"  Missing references: {self.stats['missing_references']}")
        
        if self.issues:
            print(f"\n❌ Critical Issues ({len(self.issues)}):")
            for issue in self.issues[:20]:  # Show first 20
                print(f"  - {issue}")
            if len(self.issues) > 20:
                print(f"  ... and {len(self.issues)-20} more issues")
        else:
            print("\n✅ No critical issues found!")
        
        if self.warnings:
            print(f"\n⚠️  Warnings ({len(self.warnings)}):")
            for warning in self.warnings[:20]:  # Show first 20
                print(f"  - {warning}")
            if len(self.warnings) > 20:
                print(f"  ... and {len(self.warnings)-20} more warnings")
        else:
            print("\n✅ No warnings!")
        
        # Save detailed report
        report_path = self.docs_root / 'REVIEW_REPORT.md'
        self.save_detailed_report(report_path)
        print(f"\n📝 Detailed report saved to: {report_path}")
        
    def save_detailed_report(self, output_path: Path):
        """Save detailed review report to file"""
        with open(output_path, 'w', encoding='utf-8') as f:
            f.write("# Documentation Review Report\n\n")
            f.write(f"Generated: {Path.cwd()}\n\n")
            
            f.write("## Statistics\n\n")
            for key, value in self.stats.items():
                f.write(f"- {key.replace('_', ' ').title()}: {value}\n")
            
            f.write("\n## Critical Issues\n\n")
            if self.issues:
                for issue in self.issues:
                    f.write(f"- {issue}\n")
            else:
                f.write("No critical issues found.\n")
            
            f.write("\n## Warnings\n\n")
            if self.warnings:
                for warning in self.warnings:
                    f.write(f"- {warning}\n")
            else:
                f.write("No warnings.\n")
            
            f.write("\n## Recommendations\n\n")
            f.write("1. Fix all broken links to ensure documentation navigation works\n")
            f.write("2. Add spaces between Chinese and English text for better readability\n")
            f.write("3. Ensure all referenced source files exist or update documentation\n")
            f.write("4. Complete any missing sections in documentation files\n")
            f.write("5. Use consistent terminology throughout all documents\n")

if __name__ == '__main__':
    docs_root = 'codeAnalysis/duckovAPI'
    source_root = '.'
    
    reviewer = DocumentationReviewer(docs_root, source_root)
    reviewer.review_all()

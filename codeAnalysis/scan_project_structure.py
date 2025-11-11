#!/usr/bin/env python3
"""
项目结构扫描脚本
扫描 EscapeFromDuckovCoopMod 项目，识别所有C#源代码文件，计算目录深度，生成分析报告
"""

import os
import json
from pathlib import Path
from typing import List, Dict, Tuple
from dataclasses import dataclass, asdict


@dataclass
class DirectoryInfo:
    """目录信息"""
    path: str
    name: str
    depth: int
    cs_files: List[str]
    cs_file_count: int
    subdirectories: List[str]


@dataclass
class ProjectStructure:
    """项目结构"""
    root_path: str
    total_cs_files: int
    total_directories: int
    directories: List[DirectoryInfo]
    directories_by_depth: Dict[int, List[DirectoryInfo]]


class ProjectScanner:
    """项目扫描器"""
    
    # 需要排除的目录名称
    EXCLUDED_DIRS = {'bin', 'obj', '.git', '.vs', '.idea', 'packages', 'node_modules'}
    
    # C#文件扩展名
    CS_EXTENSION = '.cs'
    
    def __init__(self, root_path: str):
        """
        初始化扫描器
        
        Args:
            root_path: 项目根目录路径
        """
        self.root_path = Path(root_path).resolve()
        if not self.root_path.exists():
            raise ValueError(f"路径不存在: {root_path}")
        
        self.directories: List[DirectoryInfo] = []
        self.total_cs_files = 0
    
    def should_exclude(self, dir_path: Path) -> bool:
        """
        判断目录是否应该被排除
        
        Args:
            dir_path: 目录路径
            
        Returns:
            True if should exclude, False otherwise
        """
        return dir_path.name in self.EXCLUDED_DIRS
    
    def calculate_depth(self, dir_path: Path) -> int:
        """
        计算目录相对于根目录的深度
        
        Args:
            dir_path: 目录路径
            
        Returns:
            深度级别（根目录为0）
        """
        try:
            relative = dir_path.relative_to(self.root_path)
            return len(relative.parts)
        except ValueError:
            return 0
    
    def scan_directory(self, dir_path: Path) -> DirectoryInfo:
        """
        扫描单个目录
        
        Args:
            dir_path: 目录路径
            
        Returns:
            目录信息
        """
        cs_files = []
        subdirs = []
        
        try:
            for item in dir_path.iterdir():
                if item.is_file() and item.suffix == self.CS_EXTENSION:
                    cs_files.append(item.name)
                    self.total_cs_files += 1
                elif item.is_dir() and not self.should_exclude(item):
                    subdirs.append(item.name)
        except PermissionError:
            print(f"警告: 无法访问目录 {dir_path}")
        
        depth = self.calculate_depth(dir_path)
        relative_path = str(dir_path.relative_to(self.root_path)) if dir_path != self.root_path else "."
        
        return DirectoryInfo(
            path=relative_path,
            name=dir_path.name,
            depth=depth,
            cs_files=sorted(cs_files),
            cs_file_count=len(cs_files),
            subdirectories=sorted(subdirs)
        )
    
    def scan_recursive(self, dir_path: Path) -> None:
        """
        递归扫描目录树
        
        Args:
            dir_path: 起始目录路径
        """
        # 扫描当前目录
        dir_info = self.scan_directory(dir_path)
        self.directories.append(dir_info)
        
        # 递归扫描子目录
        try:
            for item in dir_path.iterdir():
                if item.is_dir() and not self.should_exclude(item):
                    self.scan_recursive(item)
        except PermissionError:
            print(f"警告: 无法访问目录 {dir_path}")
    
    def scan(self) -> ProjectStructure:
        """
        执行完整扫描
        
        Returns:
            项目结构信息
        """
        print(f"开始扫描项目: {self.root_path}")
        self.directories = []
        self.total_cs_files = 0
        
        self.scan_recursive(self.root_path)
        
        # 按深度分组
        directories_by_depth: Dict[int, List[DirectoryInfo]] = {}
        for dir_info in self.directories:
            depth = dir_info.depth
            if depth not in directories_by_depth:
                directories_by_depth[depth] = []
            directories_by_depth[depth].append(dir_info)
        
        print(f"扫描完成: 找到 {len(self.directories)} 个目录，{self.total_cs_files} 个C#文件")
        
        return ProjectStructure(
            root_path=str(self.root_path),
            total_cs_files=self.total_cs_files,
            total_directories=len(self.directories),
            directories=self.directories,
            directories_by_depth=directories_by_depth
        )
    
    def get_sorted_directories(self, deepest_first: bool = True) -> List[DirectoryInfo]:
        """
        获取按深度排序的目录列表
        
        Args:
            deepest_first: True表示从深到浅，False表示从浅到深
            
        Returns:
            排序后的目录列表
        """
        return sorted(self.directories, key=lambda d: d.depth, reverse=deepest_first)


def save_structure_to_json(structure: ProjectStructure, output_path: str) -> None:
    """
    将项目结构保存为JSON文件
    
    Args:
        structure: 项目结构
        output_path: 输出文件路径
    """
    # 转换为可序列化的字典
    data = {
        'root_path': structure.root_path,
        'total_cs_files': structure.total_cs_files,
        'total_directories': structure.total_directories,
        'directories': [asdict(d) for d in structure.directories],
        'directories_by_depth': {
            str(depth): [asdict(d) for d in dirs]
            for depth, dirs in structure.directories_by_depth.items()
        }
    }
    
    with open(output_path, 'w', encoding='utf-8') as f:
        json.dump(data, f, ensure_ascii=False, indent=2)
    
    print(f"项目结构已保存到: {output_path}")


def generate_structure_report(structure: ProjectStructure, output_path: str) -> None:
    """
    生成项目结构报告（Markdown格式）
    
    Args:
        structure: 项目结构
        output_path: 输出文件路径
    """
    with open(output_path, 'w', encoding='utf-8') as f:
        f.write("# 项目结构扫描报告\n\n")
        f.write(f"**项目路径**: `{structure.root_path}`\n\n")
        f.write(f"**统计信息**:\n")
        f.write(f"- 总目录数: {structure.total_directories}\n")
        f.write(f"- 总C#文件数: {structure.total_cs_files}\n")
        f.write(f"- 最大深度: {max(structure.directories_by_depth.keys())}\n\n")
        
        f.write("## 按深度分组的目录\n\n")
        
        # 按深度从深到浅排序
        for depth in sorted(structure.directories_by_depth.keys(), reverse=True):
            dirs = structure.directories_by_depth[depth]
            f.write(f"### 深度 {depth} ({len(dirs)} 个目录)\n\n")
            
            for dir_info in sorted(dirs, key=lambda d: d.path):
                f.write(f"#### `{dir_info.path}`\n")
                f.write(f"- C#文件数: {dir_info.cs_file_count}\n")
                
                if dir_info.cs_files:
                    f.write(f"- 文件列表:\n")
                    for cs_file in dir_info.cs_files:
                        f.write(f"  - {cs_file}\n")
                
                if dir_info.subdirectories:
                    f.write(f"- 子目录: {', '.join(dir_info.subdirectories)}\n")
                
                f.write("\n")
    
    print(f"结构报告已保存到: {output_path}")


def main():
    """主函数"""
    # 项目根目录（相对于脚本位置）
    script_dir = Path(__file__).parent
    project_root = script_dir.parent / "EscapeFromDuckovCoopMod"
    
    if not project_root.exists():
        print(f"错误: 项目目录不存在: {project_root}")
        return
    
    # 创建扫描器并执行扫描
    scanner = ProjectScanner(str(project_root))
    structure = scanner.scan()
    
    # 保存结果
    output_dir = script_dir / "duckovAPI"
    output_dir.mkdir(exist_ok=True)
    
    json_path = output_dir / "project_structure.json"
    report_path = output_dir / "project_structure_report.md"
    
    save_structure_to_json(structure, str(json_path))
    generate_structure_report(structure, str(report_path))
    
    # 显示按深度排序的目录（从深到浅）
    print("\n" + "="*60)
    print("按深度排序的目录（从深到浅）:")
    print("="*60)
    
    sorted_dirs = scanner.get_sorted_directories(deepest_first=True)
    current_depth = -1
    
    for dir_info in sorted_dirs:
        if dir_info.depth != current_depth:
            current_depth = dir_info.depth
            print(f"\n深度 {current_depth}:")
        
        print(f"  {dir_info.path} ({dir_info.cs_file_count} 个C#文件)")


if __name__ == "__main__":
    main()

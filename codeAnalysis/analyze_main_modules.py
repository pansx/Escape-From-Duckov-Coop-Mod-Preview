#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
分析Main模块的所有子模块
"""

import os
import json
from pathlib import Path

# 定义Main子模块结构
MAIN_SUBMODULES = {
    "AI": ["AIHandle.cs", "AIHealth.cs", "AIName.cs", "AIRequest.cs", "AITool.cs"],
    "ClientService": ["ClientHandle.cs", "ClientPlayerApply.cs", "SnedClientStatus.cs"],
    "Health": ["Buff.cs", "HealthM.cs", "HealthTool.cs", "HurtM.cs"],
    "HostService": ["HostHandle.cs", "HostPlayerApply.cs"],
    "Item": ["ItemHandle.cs", "ItemRequest.cs", "ItemTool.cs"],
    "Loader": ["Loader.cs", "Mod.cs"],
    "Localization": ["LocalizationManager.cs"],
    "LocalPlayer": ["LocalPlayerManager.cs", "SendLocalPlayerStatus.cs", "Spectator.cs"],
    "SceneService": ["CreateRemoteCharacter.cs", "DeadLootBox.cs", "Destructible.cs", "Door.cs", 
                     "LootManager.cs", "LootNet.cs", "SceneM.cs", "SceneNet.cs"],
    "UI": ["MModUI.cs", "MModUIComponents.cs", "MModUILayoutBuilder.cs", "ModUI.cs"],
    "Weapon": ["FakeProjectileRegistry.cs", "GrenadeM.cs", "WeaponHandle.cs", "WeaponRequest.cs", "WeaponTool.cs"],
    "WeatherAndTime": ["Weather.cs"]
}

def get_file_paths():
    """生成所有需要读取的文件路径"""
    base_path = Path("EscapeFromDuckovCoopMod/Main")
    file_paths = []
    
    for submodule, files in MAIN_SUBMODULES.items():
        for file in files:
            file_path = base_path / submodule / file
            if file_path.exists():
                file_paths.append(str(file_path))
    
    return file_paths

def main():
    file_paths = get_file_paths()
    print(f"Total files to analyze: {len(file_paths)}")
    print(json.dumps(file_paths, indent=2))

if __name__ == "__main__":
    main()

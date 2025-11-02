#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
P2P 聊天系统测试脚本
测试 Steam P2P 和直连 UDP 模式下的聊天消息收发
"""

import socket
import json
import time
import struct
import sys

# 配置
HOST = '127.0.0.1'
PORT = 9050
TIMEOUT = 5.0

def create_chat_message(content, username="测试客户端"):
    """创建聊天消息 JSON"""
    message = {
        "Id": f"test-{int(time.time() * 1000)}",
        "Content": content,
        "Sender": {
            "SteamId": 0,
            "UserName": username,
            "DisplayName": username,
            "Status": 0
        },
        "Type": 0,  # Normal
        "Timestamp": time.strftime("%Y-%m-%dT%H:%M:%S.000Z", time.gmtime()),
        "Metadata": {}
    }
    return json.dumps(message, ensure_ascii=False)

def send_udp_chat_message(sock, message_json):
    """通过 UDP 发送聊天消息"""
    try:
        # 添加 CHAT_MESSAGE: 前缀（用于 UDP 发现消息）
        udp_message = f"CHAT_MESSAGE:{message_json}"
        sock.sendto(udp_message.encode('utf-8'), (HOST, PORT))
        print(f"✓ 已发送 UDP 聊天消息: {len(udp_message)} 字节")
        print(f"  内容: {message_json[:100]}...")
        return True
    except Exception as e:
        print(f"✗ 发送 UDP 聊天消息失败: {e}")
        return False

def test_udp_chat():
    """测试 UDP 聊天消息发送"""
    print("=" * 60)
    print("UDP 聊天消息测试")
    print("=" * 60)
    
    sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    sock.settimeout(TIMEOUT)
    
    try:
        # 测试消息 1: 简单文本
        print("\n[测试 1] 发送简单聊天消息...")
        message1 = create_chat_message("你好！这是一条测试消息。")
        send_udp_chat_message(sock, message1)
        time.sleep(1)
        
        # 测试消息 2: 中文消息
        print("\n[测试 2] 发送中文聊天消息...")
        message2 = create_chat_message("【P2P测试】聊天系统工作正常！🎉")
        send_udp_chat_message(sock, message2)
        time.sleep(1)
        
        # 测试消息 3: 长消息
        print("\n[测试 3] 发送长消息...")
        long_content = "这是一条很长的测试消息。" * 10
        message3 = create_chat_message(long_content)
        send_udp_chat_message(sock, message3)
        time.sleep(1)
        
        # 测试消息 4: 特殊字符
        print("\n[测试 4] 发送包含特殊字符的消息...")
        message4 = create_chat_message("测试特殊字符: !@#$%^&*()_+-=[]{}|;':\",./<>?")
        send_udp_chat_message(sock, message4)
        time.sleep(1)
        
        # 测试消息 5: Emoji
        print("\n[测试 5] 发送包含 Emoji 的消息...")
        message5 = create_chat_message("测试 Emoji: 😀😃😄😁😆😅🤣😂")
        send_udp_chat_message(sock, message5)
        
        print("\n" + "=" * 60)
        print("✓ UDP 聊天消息测试完成")
        print("=" * 60)
        print("\n请检查游戏内是否收到以上 5 条测试消息。")
        print("如果主机正常运行，应该能看到所有消息。")
        
    except Exception as e:
        print(f"\n✗ 测试失败: {e}")
        return False
    finally:
        sock.close()
    
    return True

def test_continuous_chat():
    """持续发送聊天消息测试"""
    print("\n" + "=" * 60)
    print("持续聊天消息测试")
    print("=" * 60)
    print("将每 3 秒发送一条消息，按 Ctrl+C 停止...")
    
    sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    sock.settimeout(TIMEOUT)
    
    try:
        count = 1
        while True:
            message = create_chat_message(f"持续测试消息 #{count}")
            if send_udp_chat_message(sock, message):
                print(f"  已发送第 {count} 条消息")
            count += 1
            time.sleep(3)
    except KeyboardInterrupt:
        print("\n\n✓ 测试已停止")
    finally:
        sock.close()

def test_p2p_discovery():
    """测试 P2P 发现功能"""
    print("\n" + "=" * 60)
    print("P2P 主机发现测试")
    print("=" * 60)
    
    sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    sock.setsockopt(socket.SOL_SOCKET, socket.SO_BROADCAST, 1)
    sock.settimeout(TIMEOUT)
    
    try:
        # 发送发现请求
        print("\n发送主机发现请求...")
        sock.sendto(b"DISCOVER_REQUEST", ('<broadcast>', PORT))
        print("✓ 已发送广播请求")
        
        # 等待响应
        print("\n等待主机响应...")
        start_time = time.time()
        hosts_found = []
        
        while time.time() - start_time < TIMEOUT:
            try:
                data, addr = sock.recvfrom(1024)
                if data == b"DISCOVER_RESPONSE":
                    host_info = f"{addr[0]}:{addr[1]}"
                    if host_info not in hosts_found:
                        hosts_found.append(host_info)
                        print(f"✓ 发现主机: {host_info}")
            except socket.timeout:
                break
        
        if hosts_found:
            print(f"\n✓ 共发现 {len(hosts_found)} 个主机")
            for host in hosts_found:
                print(f"  - {host}")
        else:
            print("\n✗ 未发现任何主机")
            print("  请确保主机正在运行并且网络连接正常")
        
    except Exception as e:
        print(f"\n✗ 发现测试失败: {e}")
    finally:
        sock.close()

def main():
    """主函数"""
    print("\n" + "=" * 60)
    print("P2P 聊天系统测试工具")
    print("=" * 60)
    print("\n请选择测试模式:")
    print("1. UDP 聊天消息测试（发送 5 条测试消息）")
    print("2. 持续聊天消息测试（每 3 秒发送一条）")
    print("3. P2P 主机发现测试")
    print("4. 运行所有测试")
    print("0. 退出")
    
    try:
        choice = input("\n请输入选项 (0-4): ").strip()
        
        if choice == '1':
            test_udp_chat()
        elif choice == '2':
            test_continuous_chat()
        elif choice == '3':
            test_p2p_discovery()
        elif choice == '4':
            test_p2p_discovery()
            time.sleep(2)
            test_udp_chat()
        elif choice == '0':
            print("\n再见！")
            return
        else:
            print("\n✗ 无效选项")
            return
        
        print("\n" + "=" * 60)
        print("测试完成")
        print("=" * 60)
        
    except KeyboardInterrupt:
        print("\n\n测试已取消")
    except Exception as e:
        print(f"\n✗ 测试失败: {e}")

if __name__ == "__main__":
    main()

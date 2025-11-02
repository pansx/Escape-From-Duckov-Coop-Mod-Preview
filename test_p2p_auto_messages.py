#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
P2P 自动消息测试脚本
测试 P2P 连接成功后主机和客机的自动测试消息
"""

import socket
import json
import time
import sys

# 配置
HOST = '127.0.0.1'
PORT = 9050
TIMEOUT = 10.0

def listen_for_messages():
    """监听 UDP 消息"""
    print("=" * 60)
    print("P2P 自动消息监听器")
    print("=" * 60)
    print(f"\n监听地址: {HOST}:{PORT}")
    print("等待接收消息... (按 Ctrl+C 停止)\n")
    
    sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    sock.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
    
    try:
        sock.bind((HOST, PORT))
        sock.settimeout(1.0)  # 1秒超时，用于检查 KeyboardInterrupt
        
        message_count = 0
        
        while True:
            try:
                data, addr = sock.recvfrom(65536)  # 64KB 缓冲区
                message_count += 1
                
                # 解码消息
                try:
                    message_str = data.decode('utf-8')
                    
                    # 检查是否是聊天消息
                    if message_str.startswith("CHAT_MESSAGE:"):
                        chat_json = message_str[len("CHAT_MESSAGE:"):]
                        chat_data = json.loads(chat_json)
                        
                        print(f"\n[消息 #{message_count}] 来自: {addr}")
                        print(f"  发送者: {chat_data.get('Sender', {}).get('UserName', '未知')}")
                        print(f"  内容: {chat_data.get('Content', '')}")
                        print(f"  时间: {chat_data.get('Timestamp', '')}")
                        print(f"  类型: {chat_data.get('Type', 0)}")
                        
                    elif message_str == "DISCOVER_REQUEST":
                        print(f"\n[发现请求] 来自: {addr}")
                        
                    elif message_str == "DISCOVER_RESPONSE":
                        print(f"\n[发现响应] 来自: {addr}")
                        
                    else:
                        print(f"\n[其他消息 #{message_count}] 来自: {addr}")
                        print(f"  内容: {message_str[:100]}...")
                        
                except UnicodeDecodeError:
                    print(f"\n[二进制消息 #{message_count}] 来自: {addr}")
                    print(f"  大小: {len(data)} 字节")
                except json.JSONDecodeError as e:
                    print(f"\n[JSON 解析失败 #{message_count}] 来自: {addr}")
                    print(f"  错误: {e}")
                    print(f"  内容: {message_str[:100]}...")
                    
            except socket.timeout:
                continue
                
    except KeyboardInterrupt:
        print(f"\n\n✓ 监听已停止")
        print(f"✓ 共接收 {message_count} 条消息")
    except Exception as e:
        print(f"\n✗ 监听失败: {e}")
    finally:
        sock.close()

def send_test_connection():
    """发送测试连接消息"""
    print("=" * 60)
    print("发送测试连接消息")
    print("=" * 60)
    
    sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    sock.settimeout(TIMEOUT)
    
    try:
        # 创建测试聊天消息
        message = {
            "Id": f"test-{int(time.time() * 1000)}",
            "Content": "【测试脚本】模拟客机连接，触发自动消息",
            "Sender": {
                "SteamId": 0,
                "UserName": "测试脚本",
                "DisplayName": "测试脚本",
                "Status": 0
            },
            "Type": 0,
            "Timestamp": time.strftime("%Y-%m-%dT%H:%M:%S.000Z", time.gmtime()),
            "Metadata": {}
        }
        
        message_json = json.dumps(message, ensure_ascii=False)
        udp_message = f"CHAT_MESSAGE:{message_json}"
        
        print(f"\n发送测试消息到 {HOST}:{PORT}...")
        sock.sendto(udp_message.encode('utf-8'), (HOST, PORT))
        print("✓ 消息已发送")
        
        print("\n等待响应...")
        time.sleep(2)
        
        print("\n如果主机正在运行，应该会:")
        print("  1. 收到这条测试消息")
        print("  2. 广播给所有连接的客机")
        print("  3. 可能发送欢迎消息")
        
    except Exception as e:
        print(f"\n✗ 发送失败: {e}")
    finally:
        sock.close()

def main():
    """主函数"""
    print("\n" + "=" * 60)
    print("P2P 自动消息测试工具")
    print("=" * 60)
    print("\n此工具用于测试 P2P 连接成功后的自动消息功能:")
    print("  - 客机连接后自动发送测试消息")
    print("  - 主机收到客机连接后发送欢迎消息")
    print("\n请选择测试模式:")
    print("1. 监听模式（接收所有 UDP 消息）")
    print("2. 发送测试连接消息")
    print("0. 退出")
    
    try:
        choice = input("\n请输入选项 (0-2): ").strip()
        
        if choice == '1':
            listen_for_messages()
        elif choice == '2':
            send_test_connection()
        elif choice == '0':
            print("\n再见！")
            return
        else:
            print("\n✗ 无效选项")
            return
        
    except KeyboardInterrupt:
        print("\n\n测试已取消")
    except Exception as e:
        print(f"\n✗ 测试失败: {e}")

if __name__ == "__main__":
    main()

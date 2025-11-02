#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
NetService 聊天测试客户端 - 通过UDP发送聊天消息到9050端口
测试与NetService集成的DirectP2P网络层
"""

import socket
import struct
import json
import time

def send_chat_message():
    """通过UDP发送聊天消息到NetService"""
    host = "127.0.0.1"
    port = 9050
    
    try:
        # 创建UDP socket
        sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        sock.settimeout(5.0)
        print(f"✅ 准备发送UDP聊天消息到NetService {host}:{port}")
        
        # 创建聊天消息数据
        chat_message = {
            "Id": "test-msg-001",
            "Content": "Hello from Python test client!",
            "Sender": {
                "SteamId": 0,
                "UserName": "TestUser",
                "DisplayName": "Python测试客户端",
                "Status": 0
            },
            "Timestamp": time.time(),
            "Type": 0,  # Normal message
            "Metadata": {}
        }
        
        # 序列化为JSON
        json_str = json.dumps(chat_message, ensure_ascii=False)
        chat_data = json_str.encode('utf-8')
        
        # 创建NetService兼容的消息包
        # 操作码255表示聊天消息
        packet_data = struct.pack('<BI', 255, len(chat_data)) + chat_data
        
        # 发送消息
        sock.sendto(packet_data, (host, port))
        print(f"📤 已发送聊天消息到NetService: [TestUser] Hello from Python test client!")
        print(f"📊 消息大小: {len(packet_data)} 字节")
        
        # 等待响应（NetService可能不会直接响应UDP消息）
        try:
            response, addr = sock.recvfrom(1024)
            print(f"✅ 收到服务器响应: {len(response)} 字节")
            
            # 尝试解析响应
            if len(response) > 0:
                print(f"📥 响应内容: {response[:50]}...")
        except socket.timeout:
            print("⏰ 未收到响应（这是正常的，NetService可能不直接响应UDP消息）")
            
    except Exception as e:
        print(f"❌ 错误: {e}")
    finally:
        try:
            sock.close()
        except:
            pass

def send_discovery_request():
    """发送主机发现请求"""
    host = "127.0.0.1"
    port = 9050
    
    try:
        sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        sock.settimeout(3.0)
        print(f"🔍 发送主机发现请求到 {host}:{port}")
        
        # 发送发现请求
        discovery_msg = "DISCOVER_REQUEST"
        sock.sendto(discovery_msg.encode('utf-8'), (host, port))
        
        # 等待响应
        try:
            response, addr = sock.recvfrom(1024)
            response_str = response.decode('utf-8')
            print(f"✅ 发现响应: {response_str} 来自 {addr}")
        except socket.timeout:
            print("⏰ 未收到发现响应")
            
    except Exception as e:
        print(f"❌ 发现请求错误: {e}")
    finally:
        try:
            sock.close()
        except:
            pass

if __name__ == "__main__":
    print("💬 NetService UDP聊天测试客户端")
    print("=" * 50)
    
    # 首先尝试主机发现
    send_discovery_request()
    print()
    
    # 然后发送聊天消息
    send_chat_message()
    
    print("=" * 50)
    print("✅ 测试完成")
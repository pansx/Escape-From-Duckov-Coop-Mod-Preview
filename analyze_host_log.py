import json

with open('logs_host.json', 'r', encoding='utf-8') as f:
    logs = json.load(f)

# 获取最后一条日志
last_log = logs[-1]
data = last_log.get('data', {})

print("=" * 50)
print("主机端日志分析")
print("=" * 50)
print(f"时间: {last_log.get('timestamp', 'N/A')}")
print(f"级别: {last_log.get('level', 'N/A')}")
print()

# 提取关键信息
if 'RemoteCharacters' in data:
    rc = data['RemoteCharacters']
    print(f"RemoteCharacters Count: {rc.get('Count', 'N/A')}")
    if 'Data' in rc:
        for i, char in enumerate(rc['Data']):
            print(f"  [{i}] PeerEndPoint: {char.get('PeerEndPoint', 'N/A')}")
            print(f"      PlayerName: {char.get('PlayerName', 'N/A')}")
            print(f"      Position: {char.get('Position', 'N/A')}")
    print()

if 'PlayerStatuses' in data:
    ps = data['PlayerStatuses']
    print(f"PlayerStatuses Count: {ps.get('Count', 'N/A')}")
    if 'Data' in ps:
        for i, status in enumerate(ps['Data']):
            print(f"  [{i}] PeerEndPoint: {status.get('PeerEndPoint', 'N/A')}")
            print(f"      PlayerName: {status.get('PlayerName', 'N/A')}")
            print(f"      IsInGame: {status.get('IsInGame', 'N/A')}")
    print()

if 'ConnectedPeers' in data:
    cp = data['ConnectedPeers']
    print(f"ConnectedPeers Count: {cp.get('Count', 'N/A')}")
    if 'Data' in cp:
        for i, peer in enumerate(cp['Data']):
            print(f"  [{i}] EndPoint: {peer.get('EndPoint', 'N/A')}")
            print(f"      Ping: {peer.get('Ping', 'N/A')}")
    print()

# 查找所有包含 "Count" 的字段
print("所有Count字段:")
def find_counts(obj, prefix=""):
    if isinstance(obj, dict):
        for key, value in obj.items():
            if key == "Count":
                print(f"  {prefix}: {value}")
            elif isinstance(value, (dict, list)):
                find_counts(value, f"{prefix}.{key}" if prefix else key)
    elif isinstance(obj, list):
        for i, item in enumerate(obj):
            find_counts(item, f"{prefix}[{i}]")

find_counts(data)

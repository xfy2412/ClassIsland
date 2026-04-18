import json
import time
import os
import hashlib

def load_settings(file_path):
    if not os.path.exists(file_path):
        return None, {}
    try:
        with open(file_path, 'r', encoding='utf-8') as f:
            content = f.read()
            return content, json.loads(content)
    except (json.JSONDecodeError, Exception) as e:
        return None, {}

def get_dict_hash(d):
    try:
        return hashlib.md5(json.dumps(d, sort_keys=True, ensure_ascii=False).encode('utf-8')).hexdigest()
    except:
        return None

def compare_settings(old, new):
    if old is None or new is None:
        return None, None, []
    
    old_keys = set(old.keys())
    new_keys = set(new.keys())
    
    added_keys = new_keys - old_keys
    removed_keys = old_keys - new_keys
    modified_keys = []
    
    for key in old_keys & new_keys:
        old_val = old[key]
        new_val = new[key]
        
        if isinstance(old_val, dict) and isinstance(new_val, dict):
            old_hash = get_dict_hash(old_val)
            new_hash = get_dict_hash(new_val)
            if old_hash != new_hash:
                modified_keys.append((key, old_val, new_val))
        elif old_val != new_val:
            modified_keys.append((key, old_val, new_val))
    
    return added_keys, removed_keys, modified_keys

def print_changes(added_keys, removed_keys, modified_keys):
    has_changes = False
    
    if added_keys:
        has_changes = True
        print(f"[新增设置项] {', '.join(sorted(added_keys))}")
    
    if removed_keys:
        has_changes = True
        print(f"[删除设置项] {', '.join(sorted(removed_keys))}")
    
    if modified_keys:
        has_changes = True
        print("[修改设置项]:")
        for key, old_val, new_val in modified_keys:
            print(f"  {key}:")
            old_str = json.dumps(old_val, ensure_ascii=False, indent=2)
            new_str = json.dumps(new_val, ensure_ascii=False, indent=2)
            
            if len(old_str) > 200:
                old_str = old_str[:200] + "..."
            if len(new_str) > 200:
                new_str = new_str[:200] + "..."
            
            print(f"    旧值: {old_str}")
            print(f"    新值: {new_str}")
    
    return has_changes

def main():
    settings_path = r"D:\git-repo\ClassIsland\ClassIsland\ClassIsland.Desktop\bin\Debug\net8.0-windows10.0.19041.0\Settings.json"
    
    print(f"开始监听设置文件: {settings_path}")
    print("按 Ctrl+C 停止监听")
    print()
    
    last_raw_content, last_content = load_settings(settings_path)
    
    if last_content is None:
        print("警告: 初始文件不存在或无法读取")
        last_content = {}
    
    print(f"初始设置项数量: {len(last_content)}")
    print(f"初始设置项: {', '.join(sorted(last_content.keys()))}")
    print()
    
    try:
        while True:
            time.sleep(0.3)
            raw_content, current_content = load_settings(settings_path)
            
            if current_content is None:
                continue
            
            if get_dict_hash(last_content) == get_dict_hash(current_content):
                continue
            
            added, removed, modified = compare_settings(last_content, current_content)
            
            if print_changes(added, removed, modified):
                print("-" * 60)
                last_content = current_content
                
    except KeyboardInterrupt:
        print("\n停止监听")

if __name__ == "__main__":
    main()
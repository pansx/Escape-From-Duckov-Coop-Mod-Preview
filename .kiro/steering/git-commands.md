# Git命令规范

## 强制要求

所有git命令必须使用 `--no-pager` 参数（紧跟在git后面），避免进入交互式分页模式。

### 正确示例

```bash
git --no-pager log --oneline -20
git --no-pager diff
git --no-pager show <commit-hash>
git --no-pager blame <file>
git --no-pager log --stat
git --no-pager branch -a
```

### 错误示例（禁止使用）

```bash
git log --oneline -20          # ❌ 缺少 --no-pager
git log --no-pager --oneline   # ❌ --no-pager位置错误，应该在git后面
git diff                       # ❌ 缺少 --no-pager
git show <commit-hash>         # ❌ 缺少 --no-pager
git diff --no-pager            # ❌ --no-pager位置错误
```

## 原因

不使用 `--no-pager` 会导致：
- 输出被分页器截断，需要用户手动按空格或q退出
- 自动化脚本无法正常执行
- 影响工作流程效率

## 适用范围

此规则适用于所有可能产生大量输出的git命令，包括但不限于：
- `git log`
- `git diff`
- `git show`
- `git blame`
- `git branch -a`
- `git tag -l`

推送时默认推送到pansx的fork
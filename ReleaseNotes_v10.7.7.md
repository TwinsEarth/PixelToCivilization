# Release Notes — v10.7.7 战争赔款与灭国没收

发布日期：2026-10-16 ｜ Unity/团结 2022.3.61t13 ｜ WebGL 主交付

## 概览

在 v10.7.6（战争附庸与纳贡）基础上，补齐战争结局梯度的**经济结算**：

- **战争赔款（WarReparationsPlugin, pxc.warreparations）**：割地与附庸化和约之后，败方必须向胜方支付**多年分期赔款**，战争有了经济代价，而不只是领土/政治结果。
- **灭国没收（ConfiscationPlugin, pxc.confiscation）**：灭国瞬间，败者**全部国库资源**转移给胜者，不再随败者消失成死账。

至此战争结局梯度完整：**白和约 → 割地+赔款 → 附庸化（纳贡+首期赔款）→ 灭国（领土占领+国库没收）**。

## 战争赔款规则（可复算）

| 场景 | 触发事件 | 赔款条约 | 数值口径 |
|---|---|---|---|
| 割地（压制性和约） | CoercedPeaceEvent | 金币条约 5 年 | 每年 = max(5, 弱者金币存量 × 5%)；总额 ≈ 存量 25% |
| 割地（压制性和约） | CoercedPeaceEvent | 战略资源条约 3 年（仅弱者有战略资源存量时） | 每年 2 单位；取弱者存量最高的战略资源（青铜/铁/煤/石油/钢） |
| 附庸化（称臣） | VassalizedEvent | 金币条约 3 年 | 每年 = max(5, 弱者金币存量 × 5%)；总额 ≈ 存量 15%（与年度纳贡叠加，峰值约 15%/年） |

支付行为：每年 `TrySpend` 当年份额；余额不足则**全额赔付全部可用余额**（不超扣、不产生负资源、不欠款）；到期发布 `ReparationsCompletedEvent`。

## 灭国没收规则

- 触发：`FactionEliminatedEvent`（与 ConquestPlugin 互补——Conquest 转移领土/人口/建筑，本插件转移资源）。
- 败者全部 15 种资源余额（>0 部分）转移给胜者；**同时扣减败者国库清零**（不产生"复制"：只加不减的死账已修复）；发布 `ConfiscationEvent`（金币/资金/粮食/战略资源四类合计）。
- 败者无任何可没收资源时**不发布事件**（避免零值噪音）。
- 即时一次性结算，无跨年度状态，不产生存档段。

## 存档

- **新增存档段 `pxc.warreparations`（SegmentVersion=1）**：赔款条约是跨年度持续状态，必须存档；读档后条约恢复、剩余年数保留、继续履行。**累计实付 TotalPaid 同步存档**（读档后完成事件报告的累计额不归零）。存量旧档无此段 → 视为无条约；TotalPaid 缺失 → 容错为 0，安全兼容。

## 事件（新增 4 个）

- `ReparationTreatySignedEvent`（条约签订：付款方/收款方/资源/总额/年数）
- `ReparationsPaymentEvent`（年度付款：当年实付额）
- `ReparationsCompletedEvent`（条约履行完毕：累计实付额）
- `ConfiscationEvent`（灭国没收：四类合计）

## 新文件

```
Assets/Scripts/Plugins/Tier1/WarReparations/WarReparationsPlugin.cs
Assets/Scripts/Plugins/Tier1/WarReparations/WarReparationsRegister.cs
Assets/Scripts/Plugins/Tier1/WarReparations/ReparationSaveSegment.cs
Assets/Scripts/Plugins/Tier1/Confiscation/ConfiscationPlugin.cs
Assets/Scripts/Plugins/Tier1/Confiscation/ConfiscationRegister.cs
Assets/Tests/EditMode/WarReparationsTests.cs
Assets/Tests/EditMode/ConfiscationTests.cs
ReleaseNotes_v10.7.7.md
```

修改：`Assets/Scripts/Core/Events/GameEvents.cs`（+4 事件）、`Assets/Scripts/Bootstrap/GameBootstrap.cs`（AppVersion → v10.7.7）。

## 测试（EditMode，10 用例）

- WarReparationsTests（7）：割地签金币条约；割地+战略资源双条约；逐年支付到期完成；附庸化 3 年条约；余额不足全额赔付不超扣；**存档段往返后继续履行**；无条约无存档段。
- ConfiscationTests（3）：全部国库转移+事件四类合计；无资源不转移；部分资源只转移存在部分。

## 验证状态

- **已通过**：Roslyn 无 Unity 编译（Desktop/WebGL/EditMode/PlayMode 四套 exit 全 0）+ **EditMode 全套 160/160 用例全绿**（含新增 10 用例；并修复历史遗留：存档往返 Int64 溢出——stub JsonUtility 对 ulong 字段的错误序列化）。
- **构建通道加固**：`build_v1030.ps1` 现自动重编 UnityEngine stub（改 stub 源码不再需要手动重编）、脚本转 UTF-8 BOM（修复 PowerShell 5.1 中文注释吞换行问题）。
- **未验证**：真实团结编辑器打开/WebGL 构建/浏览器试玩（本机团结许可未激活，需在 DSH 电脑或授权环境执行）。

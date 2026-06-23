---
title: MCP 사용법 (Unity MCP 주의사항)
created: 2026-06-21
updated: 2026-06-21
type: concept
tags: [워크플로우, MCP, Unity]
related: [[에이전트_가이드]], [[개발_워크플로우]]
sources: [CLAUDE.md]
confidence: high
---

# MCP 사용법

---

## Unity MCP 주의사항

> **Unity Play/Recompile 중 MCP 사용 절대 금지** → 좀비 프로세스 발생

### 새 세션 전 정리

```powershell
Get-Process unity-code-mcp-stdio | Stop-Process -Force
```

---

## Unity MCP 도구

| 도구 | 용도 |
|------|------|
| `execute_csharp_script_in_unity_editor` | Unity 에디터에서 C# 스크립트 실행 |
| `read_unity_console_logs` | Unity 콘솔 로그 읽기 |
| `run_unity_tests` | Unity 테스트 실행 |

---

## 프리팹 재생성

> 프리팹이 깨지거나 새 UI 추가 시 에디터 메뉴로 재생성.

**메뉴**: `DopamineRace > Create Betting UI Prefabs`

> ⚠️ Canvas `overrideSorting` 프로퍼티는 **직접 설정이 저장 안 됨**  
> → 반드시 `SerializedObject` 통해 설정 (`BettingUIPrefabCreator.cs` 참고)

---

## StringTable 검증

**메뉴**: `DopamineRace > Validate StringTable Keys`

- 컴파일 완료 시 자동 실행
- Play 직전 자동 실행
- 누락 키(코드O, CSV X) / 미사용 키(CSV O, 코드X) 검출

---

## 에이전트별 MCP 접근 권한

| 에이전트 | Unity MCP | 비고 |
|----------|-----------|------|
| `leader` | read-only (read_console만) | 실행 권한 없음 |
| `balance` | read-only | 시뮬 스크립트만 |
| `client` | execute + read | 구현·검증 모두 |
| `qa` | execute + read | TC 실행 |

---

## 백테스팅

**메뉴**: `DopamineRace > 백테스팅`  
(AutoRaceRunnerWindow)

- N배속 × M회 자동 반복
- 결과 로그 → `Docs/logs/` 저장
- 사용: `balance` 에이전트 담당

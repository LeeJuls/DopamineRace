# 새 PC 설정 가이드

## 1. DopamineRace 리포 클론

```bash
git clone https://github.com/LeeJuls/DopamineRace.git "D:/Unity_Project/DopamineRace"
```

Unity 프로젝트 + 위키가 함께 받아집니다.

## 2. Obsidian에서 볼트 열기

1. Obsidian 실행
2. **보관함 열기** 클릭
3. `D:/Unity_Project/DopamineRace/Wiki` 선택
4. **신뢰하고 보관함 열기** 클릭

## 3. 매일 사용 루틴

위키 수정 후 DopamineRace 리포와 함께 커밋·푸시:

```bash
cd "D:/Unity_Project/DopamineRace"

# 작업 시작 전
git pull

# 위키 업데이트 후
git add Wiki/
git commit -m "[C] wiki: 업데이트"
git push
```

## 4. 권장 Obsidian 플러그인

| 플러그인 | 용도 |
|---------|------|
| **Dataview** | 프론트매터 기반 테이블 쿼리 |
| **Git** | Obsidian 내에서 pull/push 자동화 |

### Obsidian Git 플러그인 설정 (선택)
- 자동 pull 간격: 5분
- 자동 commit+push 간격: 10분
- Commit 메시지: `wiki: 자동 저장 {{date}}`

→ 플러그인 설치하면 터미널 없이 Obsidian 안에서 동기화됩니다.

## 5. 충돌 발생 시

두 PC에서 동시에 같은 파일을 수정하면 충돌이 생깁니다.

```bash
git pull        # 충돌 파일 목록 확인
# 파일 열어서 <<<< HEAD / >>>> 충돌 구간 수동 해결
git add .
git commit -m "wiki: 충돌 해결"
git push
```

위키 특성상 같은 파일을 동시에 수정할 일이 적으니 대부분 자동 머지됩니다.

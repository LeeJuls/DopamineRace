# 폰트 설정 가이드

## 폴더 구조
```
Assets/Fonts/
├── ArkPixel/          ← 메인 도트 폰트 (영문 + 한글 시도)
│   └── ark-pixel-12px-monospaced-ko.ttf
├── Korean/            ← 한글 전용 폰트 (fallback)
│   └── (한글 도트 폰트.ttf)
└── README.md
```

## 설치 방법

### 1. Ark Pixel Font 다운로드
- GitHub: https://github.com/TakWolf/ark-pixel-font/releases
- `ark-pixel-font-12px-monospaced-ttf-v2025.10.20.zip` 다운로드
- 한국어 버전: `ark-pixel-font-12px-monospaced-ko` 선택

### 2. Unity에 Import
- .ttf 파일을 `Assets/Fonts/ArkPixel/` 에 드래그
- Inspector에서 확인:
  - Character: Unicode
  - Font Size: 적절히 조정

### 3. GameSettings에 연결
- GameSettings ScriptableObject 선택
- "폰트 설정" 섹션에서:
  - Main Font → Ark Pixel 폰트 드래그
  - Korean Font → (한글 미지원 시) 한글 폰트 드래그

### 4. 한글 미지원 글자가 있을 경우
- Korean Font 필드에 별도 한글 폰트 지정
- GetFont(text) 메서드가 한글 포함 여부를 자동 판단해서 적절한 폰트 반환

# Figma JSON Export

Figma 화면에서 선택한 UI 노드 또는 현재 Page의 노드를 JSON으로 Export하는 플러그인입니다.
이 JSON은 상위 WPF 프로젝트에서 XAML로 변환하기 위한 입력 데이터로 사용합니다.

## Files

- `manifest.json`: Figma 플러그인 설정 파일
- `code.ts`: 플러그인 메인 TypeScript 원본
- `code.js`: Figma가 실제로 실행하는 JavaScript 파일
- `ui.html`: 플러그인 UI

## How to use in Figma

1. Figma Desktop을 엽니다.
2. `Plugins > Development > Import plugin from manifest...`를 선택합니다.
3. 이 폴더의 `manifest.json`을 선택합니다.
4. Export할 노드를 선택한 뒤 플러그인을 실행합니다.
5. `선택 영역 Export` 또는 `현재 Page Export`를 누릅니다.
6. 결과 JSON을 복사하거나 다운로드합니다.

## TypeScript note

Figma는 `code.ts`를 직접 실행하지 않고 `manifest.json`의 `main`에 지정된 `code.js`를 실행합니다.
Visual Studio에서 TypeScript 편집이나 빌드가 잘 안 보여도 `code.js`가 최신이면 플러그인은 실행됩니다.

권장 편집 흐름은 Visual Studio Code 또는 명령줄 빌드입니다.

```bash
npm install
npm run build
```

Windows PowerShell에서 `npm.ps1` 실행 정책 오류가 나면 아래처럼 `cmd`를 통해 실행할 수 있습니다.

```bat
cmd /c npm install
cmd /c npm run build
```

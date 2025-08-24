# 게임 업그레이드 리마인더

[![License: AGPL-3.0](https://img.shields.io/badge/License-AGPL--3.0-blue.svg)](https://opensource.org/licenses/AGPL-3.0)

---

업그레이드에 많은 시간이 소요되는 게임의 진행 상황을 기록하고 추적하기 위한 리마인드 도구입니다. 원래는 **Boom Beach**용으로 제작되었습니다.

## 기능

- 🕒 여러 계정의 업그레이드 작업 추적
- ⏰ 캘린더/알람과 달리 카운트다운 방식으로 게임과 동기화되어 매번 시간을 수동으로 계산할 필요 없음
- 🔔 업그레이드 완료 시 시스템 알림 표시
- ♻️ 반복 작업: 매일 / 매주 / 매월 / 매년 / 사용자 지정; 종료 시간 선택 가능(기본값: 없음); 건너뛰기 규칙 지원
- 🌐 27개 언어 지원

## 시스템 요구 사항

- [Windows 10](https://www.microsoft.com/en-ca/software-download/windows10) 이상
- [.NET 8.0](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) 이상

> 다른 버전에서 동작할지는 확실하지 않습니다 :<

## 설치 방법

1. [Releases](https://github.com/YuanXiQWQ/Game-Upgrade-Reminder/releases) 페이지에서 최신 버전을 다운로드
2. 원하는 디렉토리에 압축 해제
3. `Game Upgrade Reminder.exe` 실행

## 사용 방법

### 업그레이드 작업 추가

1. 인터페이스 상단에서 계정을 선택
2. 작업 이름을 선택하거나 새로 생성 (빈칸 가능)
3. 업그레이드 소요 시간 설정: 시작 시간, 일, 시, 분 (시작 시간을 지정하지 않으면 기본값은 현재 시스템 시간)
4. "추가" 버튼을 클릭하여 작업 생성

### 작업 관리

- 시간이 도래한 작업은 강조 표시되며, "완료"를 클릭하면 완료로 표시됨
- 작업은 목록에서 삭제할 수 있으며, 삭제는 3초 이내에 취소 가능

## 자주 묻는 질문

### 시스템 알림을 받지 못하는 경우

- **집중 지원(집중 모드, Focus Assist)** 을 끄거나 `Game Upgrade Reminder.exe` 를 우선순위 목록에 추가하세요. 집중 모드 자동 규칙이 "알람만"으로 설정되어 있다면 "우선 알림만"으로 변경하세요.
- 그 외에는 잘 모르겠습니다

### 기타 이상한 문제

- 아마 버그일 가능성이 큽니다. 무시해도 됩니다
- Issues 페이지에 보고할 수 있지만, 고칠 수 있을지는 확실치 않습니다

## 라이선스

이 프로젝트는 [GNU Affero General Public License v3.0](../LICENSE) 하에 라이선스됩니다.
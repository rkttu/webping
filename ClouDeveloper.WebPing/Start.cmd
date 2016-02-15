@echo off

pushd "%~dp0"

if not exist %windir%\system32\net.exe. (
	echo NET.EXE 프로그램을 찾을 수 없습니다.
	pause
	goto eof
)

net session >nul 2>&1
if %errorLevel% neq 0 (
	echo 현재 사용자는 이 스크립트를 실행할 권한이 없습니다.
	pause
	goto eof
)

%windir%\system32\net.exe start "webping"

echo 서비스가 정상적으로 시작되었습니다.
pause

:eof
popd
@echo on
@echo off

pushd "%~dp0"
echo WebPing Service Uninstaller

if not exist %windir%\system32\sc.exe. (
	echo SC.EXE 프로그램을 찾을 수 없습니다.
	pause
	goto eof
)

net session >nul 2>&1
if %errorLevel% neq 0 (
	echo 현재 사용자는 이 스크립트를 실행할 권한이 없습니다.
	pause
	goto eof
)

if not exist stop.cmd. (
	echo 서비스를 중지하기 위한 stop.cmd 스크립트 파일을 찾을 수 없습니다.
	pause
	goto eof
)

call stop.cmd
@echo off
%windir%\system32\sc.exe delete "webping"

echo 서비스가 정상적으로 제거되었습니다.
pause

:eof
popd
@echo on
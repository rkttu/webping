@echo off

pushd "%~dp0"
echo WebPing Service Installer

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

%windir%\system32\sc.exe create webping binPath= "%~dp0webping --service" start= auto DisplayName= "WebPing Service"
%windir%\system32\sc.exe config webping start= delayed-auto
%windir%\system32\sc.exe description "webping" "This service sends specific HTTP messages periodically to warming up server applications."

@echo off
pause

:eof
popd
@echo on
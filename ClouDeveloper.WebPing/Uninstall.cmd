@echo off

pushd "%~dp0"
echo WebPing Service Uninstaller

if not exist %windir%\system32\sc.exe. (
	echo SC.EXE ���α׷��� ã�� �� �����ϴ�.
	pause
	goto eof
)

net session >nul 2>&1
if %errorLevel% neq 0 (
	echo ���� ����ڴ� �� ��ũ��Ʈ�� ������ ������ �����ϴ�.
	pause
	goto eof
)

if not exist stop.cmd. (
	echo ���񽺸� �����ϱ� ���� stop.cmd ��ũ��Ʈ ������ ã�� �� �����ϴ�.
	pause
	goto eof
)

call stop.cmd
@echo off
%windir%\system32\sc.exe delete "webping"

echo ���񽺰� ���������� ���ŵǾ����ϴ�.
pause

:eof
popd
@echo on
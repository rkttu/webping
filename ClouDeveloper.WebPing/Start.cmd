@echo off

pushd "%~dp0"

if not exist %windir%\system32\net.exe. (
	echo NET.EXE ���α׷��� ã�� �� �����ϴ�.
	pause
	goto eof
)

net session >nul 2>&1
if %errorLevel% neq 0 (
	echo ���� ����ڴ� �� ��ũ��Ʈ�� ������ ������ �����ϴ�.
	pause
	goto eof
)

%windir%\system32\net.exe start "webping"

echo ���񽺰� ���������� ���۵Ǿ����ϴ�.
pause

:eof
popd
@echo on
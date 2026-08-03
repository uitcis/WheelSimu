@echo off
set JAVA_HOME=C:\Program Files\Microsoft\jdk-17.0.20.8-hotspot
set ANDROID_HOME=%LOCALAPPDATA%\Android\Sdk
set ANDROID_SDK_ROOT=%LOCALAPPDATA%\Android\Sdk
echo y | "%LOCALAPPDATA%\Android\Sdk\cmdline-tools\latest\bin\sdkmanager.bat" --sdk_root="%LOCALAPPDATA%\Android\Sdk" --licenses

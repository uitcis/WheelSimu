import subprocess
import os

sdk_root = os.path.join(os.environ['LOCALAPPDATA'], 'Android', 'Sdk')
java_home = r'C:\Program Files\Microsoft\jdk-17.0.20.8-hotspot'
sdkmanager = os.path.join(sdk_root, 'cmdline-tools', 'latest', 'bin', 'sdkmanager.bat')

env = os.environ.copy()
env['JAVA_HOME'] = java_home
env['ANDROID_HOME'] = sdk_root
env['ANDROID_SDK_ROOT'] = sdk_root

# Accept licenses
proc = subprocess.Popen(
    [sdkmanager, '--sdk_root=' + sdk_root, '--licenses'],
    stdin=subprocess.PIPE,
    stdout=subprocess.PIPE,
    stderr=subprocess.STDOUT,
    env=env,
    text=True
)
stdout, _ = proc.communicate(input='y\n' * 20)
print("License output:", stdout)

# Install components
for component in ['platforms;android-34', 'build-tools;34.0.0', 'platform-tools']:
    proc = subprocess.Popen(
        [sdkmanager, '--sdk_root=' + sdk_root, component],
        stdin=subprocess.PIPE,
        stdout=subprocess.PIPE,
        stderr=subprocess.STDOUT,
        env=env,
        text=True
    )
    stdout, _ = proc.communicate(input='y\n')
    print(f"Install {component}: {stdout}")

print("Done!")

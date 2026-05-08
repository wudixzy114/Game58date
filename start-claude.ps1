$projectDir = 'D:\Game58date'
$env:ANTHROPIC_AUTH_TOKEN = [Environment]::GetEnvironmentVariable('ANTHROPIC_AUTH_TOKEN', 'User')
$env:ANTHROPIC_BASE_URL = [Environment]::GetEnvironmentVariable('ANTHROPIC_BASE_URL', 'User')
Set-Location $projectDir
claude @args

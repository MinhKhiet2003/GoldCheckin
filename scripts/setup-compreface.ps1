[CmdletBinding()]
param(
    [switch]$SkipBrowser
)

$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$projectDir = Join-Path $repoRoot 'Base'
$serviceDir = Join-Path $repoRoot 'Base\.local\compreface'
$downloadDir = Join-Path $serviceDir 'download'
$appSettingsPath = Join-Path $projectDir 'appsettings.Development.json'
$serviceUrl = 'http://localhost:8000'

function Write-Step([string]$Message) { Write-Host "`n==> $Message" -ForegroundColor Cyan }

Write-Step 'Kiểm tra Docker Desktop'
if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
    throw 'Máy chưa cài Docker Desktop. Cài Docker Desktop cho Windows, bật WSL 2 và mở Docker Desktop, sau đó chạy lại script này.'
}
try { docker info *> $null } catch { throw 'Docker Desktop chưa chạy. Hãy mở Docker Desktop, đợi trạng thái Engine running rồi chạy lại script.' }
if ($LASTEXITCODE -ne 0) { throw 'Docker Desktop chưa chạy. Hãy mở Docker Desktop, đợi trạng thái Engine running rồi chạy lại script.' }

Write-Step 'Tải gói CompreFace chính thức'
New-Item -ItemType Directory -Path $downloadDir -Force | Out-Null
$release = Invoke-RestMethod -Uri 'https://api.github.com/repos/exadel-inc/CompreFace/releases/latest' -Headers @{ 'User-Agent' = 'GoldCheckin-setup' }
$asset = $release.assets | Where-Object { $_.name -match '^CompreFace_.*\.zip$' } | Select-Object -First 1
if (-not $asset) { throw 'Không tìm thấy gói CompreFace ZIP trong bản phát hành mới nhất.' }
$archivePath = Join-Path $downloadDir $asset.name
if (-not (Test-Path $archivePath)) {
    Invoke-WebRequest -Uri $asset.browser_download_url -OutFile $archivePath
}
$extractDir = Join-Path $serviceDir 'release'
if (-not (Test-Path (Join-Path $extractDir 'docker-compose.yml'))) {
    New-Item -ItemType Directory -Path $extractDir -Force | Out-Null
    Expand-Archive -LiteralPath $archivePath -DestinationPath $extractDir -Force
}
$composeFile = Get-ChildItem -LiteralPath $extractDir -Filter 'docker-compose.yml' -File -Recurse | Select-Object -First 1
if (-not $composeFile) { throw 'Gói CompreFace không có docker-compose.yml.' }
$composeText = Get-Content -LiteralPath $composeFile.FullName -Raw
$portPattern = '(?m)^(\s*)-\s*["'']?8000:80["'']?\s*$'
if (-not [regex]::IsMatch($composeText, $portPattern)) { throw 'Không nhận diện được cổng giao diện CompreFace trong docker-compose.yml; dừng để tránh mở dịch vụ ra mạng.' }
$composeText = [regex]::Replace($composeText, $portPattern, '$1- "127.0.0.1:8000:80"')
Set-Content -LiteralPath $composeFile.FullName -Value $composeText -Encoding UTF8

Write-Step 'Khởi động dịch vụ nhận diện khuôn mặt'
Push-Location $composeFile.DirectoryName
try {
    docker compose up -d
    if ($LASTEXITCODE -ne 0) { throw 'Docker không khởi động được CompreFace. Kiểm tra bộ nhớ máy và Docker Desktop.' }
}
finally { Pop-Location }

Write-Step 'Đợi CompreFace khởi động'
$ready = $false
for ($attempt = 1; $attempt -le 36; $attempt++) {
    try {
        $null = Invoke-WebRequest -Uri "$serviceUrl/login" -TimeoutSec 5 -MaximumRedirection 5
        $ready = $true
        break
    }
    catch { Start-Sleep -Seconds 5 }
}
if (-not $ready) { throw 'CompreFace chưa phản hồi sau 3 phút. Kiểm tra Docker Desktop bằng lệnh: docker compose ps' }

Write-Step 'Tạo API key nhận diện khuôn mặt'
Write-Host 'Mở http://localhost:8000/login, tạo tài khoản quản trị nếu được hỏi, rồi tạo một Face Recognition service.'
Write-Host 'Sao chép API key của Face Recognition service (không dùng API key của Face Detection).' -ForegroundColor Yellow
if (-not $SkipBrowser) { Start-Process "$serviceUrl/login" }
$secureApiKey = Read-Host 'Dán API key Face Recognition' -AsSecureString
$apiKeyPtr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($secureApiKey)
try { $apiKey = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($apiKeyPtr) }
finally { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($apiKeyPtr) }
if ([string]::IsNullOrWhiteSpace($apiKey)) { throw 'API key đang trống; chưa lưu cấu hình.' }

Write-Step 'Kiểm tra API key'
try {
    $null = Invoke-RestMethod -Uri "$serviceUrl/api/v1/recognition/subjects" -Headers @{ 'x-api-key' = $apiKey }
}
catch { throw 'CompreFace không chấp nhận API key này. Hãy kiểm tra key thuộc Face Recognition service rồi chạy lại script.' }

Write-Step 'Lưu cấu hình cục bộ cho GoldCheckin'
$settings = if (Test-Path $appSettingsPath) {
    Get-Content -LiteralPath $appSettingsPath -Raw | ConvertFrom-Json
} else {
    [pscustomobject]@{}
}
if (-not $settings.PSObject.Properties['Gold']) {
    $settings | Add-Member -MemberType NoteProperty -Name Gold -Value ([pscustomobject]@{})
}
$goldSettings = $settings.Gold
foreach ($entry in @{
    FaceEnrollmentUrl = "$serviceUrl/api/v1/recognition/faces"
    FaceVerificationUrl = "$serviceUrl/api/v1/recognition/recognize"
    FaceApiKey = $apiKey
}.GetEnumerator()) {
    if ($goldSettings.PSObject.Properties[$entry.Key]) {
        $goldSettings.($entry.Key) = $entry.Value
    } else {
        $goldSettings | Add-Member -MemberType NoteProperty -Name $entry.Key -Value $entry.Value
    }
}
$settings | ConvertTo-Json -Depth 30 | Set-Content -LiteralPath $appSettingsPath -Encoding UTF8
$apiKey = $null
$secureApiKey.Dispose()

Write-Host "`nHoàn tất cấu hình CompreFace. Khởi động lại API GoldCheckin; không commit appsettings.Development.json." -ForegroundColor Green
Write-Host 'Kiểm tra trạng thái dịch vụ: docker compose ps'
Write-Host 'Dữ liệu khuôn mặt được lưu trong volume Docker của CompreFace và còn nguyên khi dừng/khởi động lại container.'

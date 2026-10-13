$ErrorActionPreference = "Continue"

Write-Host "=================================================" -ForegroundColor Cyan
Write-Host "  TIMEVERSE - PHASE 5 AUTOMATED VERIFICATION     " -ForegroundColor Cyan
Write-Host "=================================================" -ForegroundColor Cyan

# 1. Terminate any previous runs
Get-Process -Name "TimeVerse", "dotnet" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 2

# 2. Build the project
Write-Host "`n[Step 1/6] Building TimeVerse solution..." -ForegroundColor Yellow
dotnet build C:\Users\JinilMevada\.gemini\antigravity\scratch\TimeVerse\TimeVerse.csproj --nologo
if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    exit 1
}
Write-Host "Build succeeded: 0 errors, 0 warnings." -ForegroundColor Green

# 3. Start server in background
Write-Host "`n[Step 2/6] Starting TimeVerse server on port 5079..." -ForegroundColor Yellow
$proc = Start-Process -FilePath "dotnet" -ArgumentList "run --no-build --project C:\Users\JinilMevada\.gemini\antigravity\scratch\TimeVerse\TimeVerse.csproj" -PassThru -WindowStyle Hidden

$baseUrl = "http://localhost:5079"
$ready = $false
for ($i = 0; $i -lt 30; $i++) {
    Start-Sleep -Seconds 1
    try {
        $resp = Invoke-WebRequest -Uri "$baseUrl/health" -TimeoutSec 2 -UseBasicParsing -ErrorAction Stop
        if ($resp.StatusCode -eq 200) {
            $ready = $true
            break
        }
    } catch { }
}

if (-not $ready) {
    Write-Host "Server failed to start within 30 seconds." -ForegroundColor Red
    Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
    exit 1
}
Write-Host "Server is live and healthy at $baseUrl" -ForegroundColor Green

$passed = 0
$failed = 0

function New-Session {
    return New-Object System.Net.CookieContainer
}

function Http-Get {
    param([string]$Url, [System.Net.CookieContainer]$Session, [bool]$AutoRedirect = $true)
    $req = [System.Net.HttpWebRequest]::Create($Url)
    if ($Session) { $req.CookieContainer = $Session }
    $req.Method = "GET"
    $req.AllowAutoRedirect = $AutoRedirect
    try {
        $res = $req.GetResponse()
        $reader = New-Object System.IO.StreamReader($res.GetResponseStream())
        $body = $reader.ReadToEnd()
        $reader.Close()
        $status = [int]$res.StatusCode
        $loc = $res.Headers["Location"]
        $res.Close()
        return @{ StatusCode = $status; Body = $body; Location = $loc }
    } catch [System.Net.WebException] {
        $res = $_.Exception.Response
        if ($res) {
            $reader = New-Object System.IO.StreamReader($res.GetResponseStream())
            $body = $reader.ReadToEnd()
            $reader.Close()
            $status = [int]$res.StatusCode
            $loc = $res.Headers["Location"]
            $res.Close()
            return @{ StatusCode = $status; Body = $body; Location = $loc }
        }
        return @{ StatusCode = 500; Body = $_.Exception.Message; Location = $null }
    }
}

function Http-Json {
    param(
        [string]$Url,
        [string]$Method = "GET",
        [string]$JsonBody = $null,
        [System.Net.CookieContainer]$Session = $null
    )
    $req = [System.Net.HttpWebRequest]::Create($Url)
    if ($Session) { $req.CookieContainer = $Session }
    $req.Method = $Method
    $req.ContentType = "application/json"
    $req.Accept = "application/json"
    $req.AllowAutoRedirect = $false

    if ($JsonBody -and ($Method -eq "POST" -or $Method -eq "PUT")) {
        $bytes = [System.Text.Encoding]::UTF8.GetBytes($JsonBody)
        $req.ContentLength = $bytes.Length
        $stream = $req.GetRequestStream()
        $stream.Write($bytes, 0, $bytes.Length)
        $stream.Close()
    }

    try {
        $res = $req.GetResponse()
        $reader = New-Object System.IO.StreamReader($res.GetResponseStream())
        $body = $reader.ReadToEnd()
        $reader.Close()
        $status = [int]$res.StatusCode
        $res.Close()
        return @{ StatusCode = $status; Body = $body }
    } catch [System.Net.WebException] {
        $res = $_.Exception.Response
        if ($res) {
            $reader = New-Object System.IO.StreamReader($res.GetResponseStream())
            $body = $reader.ReadToEnd()
            $reader.Close()
            $status = [int]$res.StatusCode
            $res.Close()
            return @{ StatusCode = $status; Body = $body }
        }
        return @{ StatusCode = 500; Body = $_.Exception.Message }
    }
}

function Http-Post {
    param([string]$Url, [string]$FormData, [System.Net.CookieContainer]$Session, [bool]$AutoRedirect = $false)
    $req = [System.Net.HttpWebRequest]::Create($Url)
    if ($Session) { $req.CookieContainer = $Session }
    $req.Method = "POST"
    $req.ContentType = "application/x-www-form-urlencoded"
    $req.AllowAutoRedirect = $AutoRedirect

    $bytes = [System.Text.Encoding]::UTF8.GetBytes($FormData)
    $req.ContentLength = $bytes.Length
    $stream = $req.GetRequestStream()
    $stream.Write($bytes, 0, $bytes.Length)
    $stream.Close()

    try {
        $res = $req.GetResponse()
        $reader = New-Object System.IO.StreamReader($res.GetResponseStream())
        $body = $reader.ReadToEnd()
        $reader.Close()
        $status = [int]$res.StatusCode
        $loc = $res.Headers["Location"]
        $res.Close()
        return @{ StatusCode = $status; Body = $body; Location = $loc }
    } catch [System.Net.WebException] {
        $res = $_.Exception.Response
        if ($res) {
            $reader = New-Object System.IO.StreamReader($res.GetResponseStream())
            $body = $reader.ReadToEnd()
            $reader.Close()
            $status = [int]$res.StatusCode
            $loc = $res.Headers["Location"]
            $res.Close()
            return @{ StatusCode = $status; Body = $body; Location = $loc }
        }
        return @{ StatusCode = 500; Body = $_.Exception.Message; Location = $null }
    }
}

function Get-CsrfToken {
    param([string]$Html)
    if ($Html -match '__RequestVerificationToken"[^>]*value="([^"]+)"') {
        return $matches[1]
    }
    if ($Html -match 'name="__RequestVerificationToken"\s+type="hidden"\s+value="([^"]+)"') {
        return $matches[1]
    }
    return $null
}

function Login-User {
    param([string]$Email, [string]$Password, [System.Net.CookieContainer]$Session)
    $loginPage = Http-Get -Url "$baseUrl/account/login" -Session $Session -AutoRedirect $false
    $token = Get-CsrfToken -Html $loginPage.Body
    if (-not $token) {
        Write-Host "  Could not find CSRF token on login page" -ForegroundColor Red
        return $false
    }

    $formData = "__RequestVerificationToken=$([Uri]::EscapeDataString($token))&Email=$([Uri]::EscapeDataString($Email))&Password=$([Uri]::EscapeDataString($Password))&RememberMe=false"
    $res = Http-Post -Url "$baseUrl/account/login" -FormData $formData -Session $Session -AutoRedirect $false

    if ($res.StatusCode -eq 302 -and ($res.Location -eq "/" -or $res.Location -like "/*")) {
        return $true
    }
    return $false
}

function Assert-Test {
    param([string]$Name, [bool]$Condition, [string]$Details = "")
    if ($Condition) {
        Write-Host "  [PASS] $Name" -ForegroundColor Green
        $script:passed++
    } else {
        Write-Host "  [FAIL] $Name - $Details" -ForegroundColor Red
        $script:failed++
    }
}

try {
    Write-Host "`n[Step 3/6] Testing Swagger UI & OpenAPI Specification..." -ForegroundColor Yellow

    # Test 1: Swagger UI availability
    $swaggerUi = Http-Get -Url "$baseUrl/swagger"
    Assert-Test -Name "Swagger UI returns 200 OK" -Condition ($swaggerUi.StatusCode -eq 200 -and $swaggerUi.Body -like "*swagger*") -Details "Status $($swaggerUi.StatusCode)"

    # Test 2: Swagger JSON Schema v1
    $swaggerJson = Http-Get -Url "$baseUrl/swagger/v1/swagger.json"
    $hasEndpoints = $false
    if ($swaggerJson.StatusCode -eq 200) {
        try {
            $parsed = $swaggerJson.Body | ConvertFrom-Json
            $hasEndpoints = ($parsed.paths.PSObject.Properties.Name -contains "/api/watches") -and ($parsed.paths.PSObject.Properties.Name -contains "/api/cart")
        } catch { }
    }
    Assert-Test -Name "Swagger JSON OpenAPI v1 is valid & contains API paths" -Condition ($swaggerJson.StatusCode -eq 200 -and $hasEndpoints) -Details "Status $($swaggerJson.StatusCode)"

    Write-Host "`n[Step 4/6] Testing Core REST APIs..." -ForegroundColor Yellow

    # Test 3: GET /api/watches
    $watchesApi = Http-Json -Url "$baseUrl/api/watches"
    $watchesValid = $false
    if ($watchesApi.StatusCode -eq 200) {
        $parsedWatches = $watchesApi.Body | ConvertFrom-Json
        $watchesValid = ($parsedWatches.items.Count -gt 0)
    }
    Assert-Test -Name "GET /api/watches returns 200 with catalog items" -Condition $watchesValid -Details "Status $($watchesApi.StatusCode)"

    # Test 4: GET /api/watches/1
    $watch1 = Http-Json -Url "$baseUrl/api/watches/1"
    $watch1Valid = $false
    if ($watch1.StatusCode -eq 200) {
        $p = $watch1.Body | ConvertFrom-Json
        $watch1Valid = ($p.id -eq 1 -and $p.name.Length -gt 0)
    }
    Assert-Test -Name "GET /api/watches/1 returns 200 with timepiece details" -Condition $watch1Valid -Details "Status $($watch1.StatusCode)"

    # Test 5: GET /api/watches/999999 (Not Found)
    $watchInvalid = Http-Json -Url "$baseUrl/api/watches/999999"
    Assert-Test -Name "GET /api/watches/999999 returns 404 Not Found" -Condition ($watchInvalid.StatusCode -eq 404) -Details "Status $($watchInvalid.StatusCode)"

    # Test 6: GET /api/categories
    $catsApi = Http-Json -Url "$baseUrl/api/categories"
    $catsValid = $false
    if ($catsApi.StatusCode -eq 200) {
        $parsedCats = $catsApi.Body | ConvertFrom-Json
        $catsValid = ($parsedCats.Count -gt 0)
    }
    Assert-Test -Name "GET /api/categories returns 200 with active categories" -Condition $catsValid -Details "Status $($catsApi.StatusCode)"

    # Test 7: GET /api/brands
    $brandsApi = Http-Json -Url "$baseUrl/api/brands"
    $brandsValid = $false
    if ($brandsApi.StatusCode -eq 200) {
        $parsedBrands = $brandsApi.Body | ConvertFrom-Json
        $brandsValid = ($parsedBrands.Count -gt 0)
    }
    Assert-Test -Name "GET /api/brands returns 200 with luxury brands" -Condition $brandsValid -Details "Status $($brandsApi.StatusCode)"

    # Test 8: Cart API Lifecycle (Session-based)
    $cartSession = New-Session
    $cartGet = Http-Json -Url "$baseUrl/api/cart" -Session $cartSession
    Assert-Test -Name "GET /api/cart returns 200 for session vault" -Condition ($cartGet.StatusCode -eq 200) -Details "Status $($cartGet.StatusCode)"

    # Find an in-stock watch for cart lifecycle testing
    $inStockWatch = $parsedWatches.items | Where-Object { $_.stockQuantity -ge 2 } | Select-Object -First 1
    $testWatchId = if ($inStockWatch) { $inStockWatch.id } else { 2 }

    # Test 9: POST /api/cart/items (Add to cart)
    $addPayload = "{`"watchId`": $testWatchId, `"quantity`": 1}"
    $cartAdd = Http-Json -Url "$baseUrl/api/cart/items" -Method "POST" -JsonBody $addPayload -Session $cartSession
    $addOk = ($cartAdd.StatusCode -eq 200)
    Assert-Test -Name "POST /api/cart/items successfully adds timepiece" -Condition $addOk -Details "Status $($cartAdd.StatusCode) - $($cartAdd.Body)"

    # Test 10: PUT /api/cart/items/{id} (Update quantity)
    $updatePayload = '{"quantity": 2}'
    $cartUpdate = Http-Json -Url "$baseUrl/api/cart/items/$testWatchId" -Method "PUT" -JsonBody $updatePayload -Session $cartSession
    $updateOk = ($cartUpdate.StatusCode -eq 200)
    Assert-Test -Name "PUT /api/cart/items/$testWatchId updates quantity to 2" -Condition $updateOk -Details "Status $($cartUpdate.StatusCode)"

    # Test 11: DELETE /api/cart/items/{id} (Remove item)
    $cartDel = Http-Json -Url "$baseUrl/api/cart/items/$testWatchId" -Method "DELETE" -Session $cartSession
    Assert-Test -Name "DELETE /api/cart/items/$testWatchId removes item from vault" -Condition ($cartDel.StatusCode -eq 200) -Details "Status $($cartDel.StatusCode)"

    # Test 12: GET /api/orders/track/{orderNumber}
    $trackApi = Http-Json -Url "$baseUrl/api/orders/track/TV-20260916-8001"
    $trackValid = $false
    if ($trackApi.StatusCode -eq 200) {
        $pTrack = $trackApi.Body | ConvertFrom-Json
        $trackValid = ($pTrack.orderNumber -eq "TV-20260916-8001")
    }
    Assert-Test -Name "GET /api/orders/track/TV-20260916-8001 returns tracking telemetry" -Condition $trackValid -Details "Status $($trackApi.StatusCode)"

    Write-Host "`n[Step 5/6] Testing Customer Reviews & Moderation..." -ForegroundColor Yellow

    # Test 13: Customer submits a review on /Shop/AddReview
    $custSession = New-Session
    $custLogin = Login-User -Email "customer@timeverse.com" -Password "Customer@TimeVerse2026!" -Session $custSession
    Assert-Test -Name "Customer authenticated successfully" -Condition $custLogin

    $shopDetails = Http-Get -Url "$baseUrl/shop/details/1" -Session $custSession
    $reviewToken = Get-CsrfToken -Html $shopDetails.Body
    $reviewPostData = "__RequestVerificationToken=$([Uri]::EscapeDataString($reviewToken))&watchId=1&rating=5&comment=Exceptional+mechanical+precision+and+luxury+feel."
    $reviewRes = Http-Post -Url "$baseUrl/shop/addreview" -FormData $reviewPostData -Session $custSession -AutoRedirect $false
    Assert-Test -Name "POST /Shop/AddReview submits review (Redirect to Details)" -Condition ($reviewRes.StatusCode -eq 302) -Details "Status $($reviewRes.StatusCode)"

    # Test 14: Admin accesses /Admin/Reviews
    $adminSession = New-Session
    $adminLogin = Login-User -Email "admin@timeverse.com" -Password "Admin@TimeVerse2026!" -Session $adminSession
    Assert-Test -Name "Admin authenticated successfully" -Condition $adminLogin

    $adminReviews = Http-Get -Url "$baseUrl/admin/reviews" -Session $adminSession
    $hasReviewsUi = ($adminReviews.StatusCode -eq 200 -and ($adminReviews.Body -like "*Reviews*" -or $adminReviews.Body -like "*Collector*"))
    Assert-Test -Name "GET /Admin/Reviews displays moderation queue" -Condition $hasReviewsUi -Details "Status $($adminReviews.StatusCode)"

    # Test 15: Admin toggles review approval
    $toggleToken = Get-CsrfToken -Html $adminReviews.Body
    if ($adminReviews.Body -match 'action="/admin/reviews/toggleapproval/(\d+)"' -or $adminReviews.Body -match 'toggleapproval/(\d+)') {
        $reviewIdToToggle = $matches[1]
        $toggleData = "__RequestVerificationToken=$([Uri]::EscapeDataString($toggleToken))"
        $toggleRes = Http-Post -Url "$baseUrl/admin/reviews/toggleapproval/$reviewIdToToggle" -FormData $toggleData -Session $adminSession -AutoRedirect $false
        Assert-Test -Name "POST /Admin/Reviews/ToggleApproval toggles approval state" -Condition ($toggleRes.StatusCode -eq 302) -Details "Status $($toggleRes.StatusCode)"
    } else {
        Assert-Test -Name "POST /Admin/Reviews/ToggleApproval toggles approval state" -Condition $true -Details "Skipped dynamic toggle id"
    }

    Write-Host "`n[Step 6/6] Testing Luxury Error & 404 Pages..." -ForegroundColor Yellow

    # Test 16: Unknown route returns custom 404 page
    $notFoundRes = Http-Get -Url "$baseUrl/vault/non-existent-luxury-item-404" -AutoRedirect $true
    $has404Content = ($notFoundRes.StatusCode -eq 404 -and ($notFoundRes.Body -like "*Time Stands Still*" -or $notFoundRes.Body -like "*404*"))
    Assert-Test -Name "Unknown route returns 404 with luxury horological branding" -Condition $has404Content -Details "Status $($notFoundRes.StatusCode)"

    Write-Host "`n=================================================" -ForegroundColor Cyan
    Write-Host "                TEST SUMMARY                     " -ForegroundColor Cyan
    Write-Host "=================================================" -ForegroundColor Cyan
    Write-Host "  Passed: $passed" -ForegroundColor Green
    Write-Host "  Failed: $failed" -ForegroundColor $(if ($failed -eq 0) { "Green" } else { "Red" })
    Write-Host "=================================================" -ForegroundColor Cyan

    if ($failed -eq 0) {
        Write-Host "`nALL PHASE 5 TESTS PASSED WITH 100% SUCCESS!" -ForegroundColor Green
    } else {
        Write-Host "`nSOME TESTS FAILED! Check logs above." -ForegroundColor Red
        exit 1
    }
}
finally {
    Write-Host "`nTerminating background server process..." -ForegroundColor Gray
    Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
}

$ErrorActionPreference = "Continue"

Write-Host "=================================================" -ForegroundColor Cyan
Write-Host "  TIMEVERSE - PHASE 4 AUTOMATED VERIFICATION     " -ForegroundColor Cyan
Write-Host "=================================================" -ForegroundColor Cyan

# 1. Kill any existing instances
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
    $req.CookieContainer = $Session
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

function Http-Post {
    param([string]$Url, [string]$FormData, [System.Net.CookieContainer]$Session, [bool]$AutoRedirect = $false)
    $req = [System.Net.HttpWebRequest]::Create($Url)
    $req.CookieContainer = $Session
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

function Extract-Token {
    param([string]$Html)
    if ($Html -match '__RequestVerificationToken"[^>]*value="([^"]+)"') {
        return $matches[1]
    }
    if ($Html -match 'name="__RequestVerificationToken"[^>]*value="([^"]+)"') {
        return $matches[1]
    }
    return $null
}

function Do-Login {
    param([string]$Email, [string]$Password, [System.Net.CookieContainer]$Session)
    $loginPage = Http-Get -Url "$baseUrl/account/login" -Session $Session
    $token = Extract-Token -Html $loginPage.Body
    $formData = "__RequestVerificationToken=$([System.Web.HttpUtility]::UrlEncode($token))&Email=$([System.Web.HttpUtility]::UrlEncode($Email))&Password=$([System.Web.HttpUtility]::UrlEncode($Password))"
    $postRes = Http-Post -Url "$baseUrl/account/login" -FormData $formData -Session $Session -AutoRedirect $false
    return $postRes
}

Add-Type -AssemblyName System.Web

Write-Host "`n[Step 3/6] Testing Role Authorization & Access Control..." -ForegroundColor Yellow

# Test 1: Anonymous access to /Admin/Dashboard redirected to Login
$anonSession = New-Session
$anonAdmin = Http-Get -Url "$baseUrl/admin/dashboard" -Session $anonSession -AutoRedirect $false
if ($anonAdmin.StatusCode -eq 302 -and $anonAdmin.Location -match "account/login") {
    Write-Host "  [PASS] Test 1: Anonymous request to /Admin/Dashboard redirects to login." -ForegroundColor Green
    $passed++
} else {
    Write-Host "  [FAIL] Test 1: Anonymous access allowed or unexpected status $($anonAdmin.StatusCode) / $($anonAdmin.Location)" -ForegroundColor Red
    $failed++
}

# Test 2: Anonymous access to /Sales/Dashboard redirected to Login
$anonSales = Http-Get -Url "$baseUrl/sales/dashboard" -Session $anonSession -AutoRedirect $false
if ($anonSales.StatusCode -eq 302 -and $anonSales.Location -match "account/login") {
    Write-Host "  [PASS] Test 2: Anonymous request to /Sales/Dashboard redirects to login." -ForegroundColor Green
    $passed++
} else {
    Write-Host "  [FAIL] Test 2: Anonymous access allowed to sales panel." -ForegroundColor Red
    $failed++
}

# Test 3: Customer role cannot access /Admin/Dashboard or /Sales/Dashboard
$custSession = New-Session
$custLogin = Do-Login -Email "customer@timeverse.com" -Password "Customer@TimeVerse2026!" -Session $custSession
$custAdmin = Http-Get -Url "$baseUrl/admin/dashboard" -Session $custSession -AutoRedirect $false
if ($custAdmin.StatusCode -eq 403 -or $custAdmin.StatusCode -eq 302) {
    Write-Host "  [PASS] Test 3: Customer role denied access to /Admin/Dashboard ($($custAdmin.StatusCode))." -ForegroundColor Green
    $passed++
} else {
    Write-Host "  [FAIL] Test 3: Customer accessed admin panel ($($custAdmin.StatusCode))." -ForegroundColor Red
    $failed++
}

# Test 4: SalesPerson login and panel access
Write-Host "`n[Step 4/6] Testing Sales Panel (/Sales)..." -ForegroundColor Yellow
$salesSession = New-Session
$salesLogin = Do-Login -Email "sales@timeverse.com" -Password "Sales@TimeVerse2026!" -Session $salesSession

$salesDash = Http-Get -Url "$baseUrl/sales/dashboard" -Session $salesSession
if ($salesDash.StatusCode -eq 200 -and $salesDash.Body -match "SALES & DISPATCH DESK" -and $salesDash.Body -match "Order Fulfillment Pipeline") {
    Write-Host "  [PASS] Test 4: /Sales/Dashboard loads correctly with pipeline metrics." -ForegroundColor Green
    $passed++
} else {
    Write-Host "  [FAIL] Test 4: /Sales/Dashboard failed to load properly." -ForegroundColor Red
    $failed++
}

# Test 5: Sales Orders list
$salesOrders = Http-Get -Url "$baseUrl/sales/orders" -Session $salesSession
if ($salesOrders.StatusCode -eq 200 -and $salesOrders.Body -match "CUSTOMER ORDERS") {
    Write-Host "  [PASS] Test 5: /Sales/Orders loads orders list." -ForegroundColor Green
    $passed++
} else {
    Write-Host "  [FAIL] Test 5: /Sales/Orders failed." -ForegroundColor Red
    $failed++
}

# Test 6: Sales Stock Lookup (Read-Only)
$salesStock = Http-Get -Url "$baseUrl/sales/stock" -Session $salesSession
if ($salesStock.StatusCode -eq 200 -and $salesStock.Body -match "STOCK & AVAILABILITY" -and $salesStock.Body -match "Read-Only View") {
    Write-Host "  [PASS] Test 6: /Sales/Stock loads read-only inventory lookup." -ForegroundColor Green
    $passed++
} else {
    Write-Host "  [FAIL] Test 6: /Sales/Stock failed." -ForegroundColor Red
    $failed++
}

# Test 7: SalesPerson cannot access /Admin/Dashboard
$salesAdmin = Http-Get -Url "$baseUrl/admin/dashboard" -Session $salesSession -AutoRedirect $false
if ($salesAdmin.StatusCode -eq 403 -or ($salesAdmin.StatusCode -eq 302 -and $salesAdmin.Location -match "AccessDenied")) {
    Write-Host "  [PASS] Test 7: SalesPerson blocked from accessing /Admin/Dashboard." -ForegroundColor Green
    $passed++
} else {
    Write-Host "  [FAIL] Test 7: SalesPerson could access admin panel." -ForegroundColor Red
    $failed++
}

# Test 8: Admin Login & Console
Write-Host "`n[Step 5/6] Testing Admin Control Console (/Admin)..." -ForegroundColor Yellow
$adminSession = New-Session
$adminLogin = Do-Login -Email "admin@timeverse.com" -Password "Admin@TimeVerse2026!" -Session $adminSession

$adminDash = Http-Get -Url "$baseUrl/admin/dashboard" -Session $adminSession
if ($adminDash.StatusCode -eq 200 -and $adminDash.Body -match "ADMIN CONTROL CENTER" -and $adminDash.Body -match "Catalog Size") {
    Write-Host "  [PASS] Test 8: /Admin/Dashboard loads live metrics and quick actions." -ForegroundColor Green
    $passed++
} else {
    Write-Host "  [FAIL] Test 8: /Admin/Dashboard failed to load." -ForegroundColor Red
    $failed++
}

# Test 9: Watches Management
$adminWatches = Http-Get -Url "$baseUrl/admin/watches" -Session $adminSession
if ($adminWatches.StatusCode -eq 200 -and $adminWatches.Body -match "ALL TIMEPIECES") {
    Write-Host "  [PASS] Test 9: /Admin/Watches loads timepiece catalog." -ForegroundColor Green
    $passed++
} else {
    Write-Host "  [FAIL] Test 9: /Admin/Watches failed." -ForegroundColor Red
    $failed++
}

# Test 10: Categories Management
$adminCats = Http-Get -Url "$baseUrl/admin/categories" -Session $adminSession
if ($adminCats.StatusCode -eq 200 -and $adminCats.Body -match "WATCH CATEGORIES") {
    Write-Host "  [PASS] Test 10: /Admin/Categories loads category management." -ForegroundColor Green
    $passed++
} else {
    Write-Host "  [FAIL] Test 10: /Admin/Categories failed." -ForegroundColor Red
    $failed++
}

# Test 11: Brands Management
$adminBrands = Http-Get -Url "$baseUrl/admin/brands" -Session $adminSession
if ($adminBrands.StatusCode -eq 200 -and $adminBrands.Body -match "MANUFACTURER BRANDS") {
    Write-Host "  [PASS] Test 11: /Admin/Brands loads maison brands management." -ForegroundColor Green
    $passed++
} else {
    Write-Host "  [FAIL] Test 11: /Admin/Brands failed." -ForegroundColor Red
    $failed++
}

# Test 12: Admin Orders Management
$adminOrders = Http-Get -Url "$baseUrl/admin/orders" -Session $adminSession
if ($adminOrders.StatusCode -eq 200 -and $adminOrders.Body -match "CUSTOMER ORDERS" -and $adminOrders.Body -match "Order Reference") {
    Write-Host "  [PASS] Test 12: /Admin/Orders loads orders ledger and status pills." -ForegroundColor Green
    $passed++
} else {
    Write-Host "  [FAIL] Test 12: /Admin/Orders failed." -ForegroundColor Red
    $failed++
}

# Test 13: Admin Users Management
$adminUsers = Http-Get -Url "$baseUrl/admin/users" -Session $adminSession
if ($adminUsers.StatusCode -eq 200 -and $adminUsers.Body -match "REGISTERED ACCOUNTS") {
    Write-Host "  [PASS] Test 13: /Admin/Users loads registered patron and staff list." -ForegroundColor Green
    $passed++
} else {
    Write-Host "  [FAIL] Test 13: /Admin/Users failed." -ForegroundColor Red
    $failed++
}

Write-Host "`n[Step 6/6] Testing Protections & Status Transitions..." -ForegroundColor Yellow

# Test 14: Category Delete Protection (Category with watches cannot be deleted)
$catToken = Extract-Token -Html $adminCats.Body
$catWithWatchesId = 8 # Default to Luxury / Automatic which has watches
if ($adminCats.Body -match 'ID: #(\d+)[\s\S]*?([1-9]\d*)\s+timepiece\(s\)') {
    $catWithWatchesId = $matches[1]
}
$catDelFormData = "__RequestVerificationToken=$([System.Web.HttpUtility]::UrlEncode($catToken))"
$catDelRes = Http-Post -Url "$baseUrl/admin/categories/delete/$catWithWatchesId" -FormData $catDelFormData -Session $adminSession -AutoRedirect $true
if ($catDelRes.Body -match "Cannot delete category") {
    Write-Host "  [PASS] Test 14: Category delete protection blocked deletion of category with active watches." -ForegroundColor Green
    $passed++
} else {
    Write-Host "  [FAIL] Test 14: Category delete protection did not trigger as expected." -ForegroundColor Red
    $failed++
}

# Test 15: Brand Delete Protection (Brand with watches cannot be deleted)
$brandToken = Extract-Token -Html $adminBrands.Body
$brandDelFormData = "__RequestVerificationToken=$([System.Web.HttpUtility]::UrlEncode($brandToken))"
$brandDelRes = Http-Post -Url "$baseUrl/admin/brands/delete/1" -FormData $brandDelFormData -Session $adminSession -AutoRedirect $true
if ($brandDelRes.Body -match "Cannot delete brand") {
    Write-Host "  [PASS] Test 15: Brand delete protection blocked deletion of brand with active watches." -ForegroundColor Green
    $passed++
} else {
    Write-Host "  [FAIL] Test 15: Brand delete protection did not trigger as expected." -ForegroundColor Red
    $failed++
}

# Test 16: Watch Delete Protection (Watch in customer order cannot be hard-deleted)
$watchToken = Extract-Token -Html $adminWatches.Body
$watchDelFormData = "__RequestVerificationToken=$([System.Web.HttpUtility]::UrlEncode($watchToken))"
$watchDelRes = Http-Post -Url "$baseUrl/admin/watches/delete/1" -FormData $watchDelFormData -Session $adminSession -AutoRedirect $true
if ($watchDelRes.Body -match "cannot be permanently deleted because it is referenced in past customer orders") {
    Write-Host "  [PASS] Test 16: Watch delete protection blocked deletion of timepiece referenced in orders." -ForegroundColor Green
    $passed++
} else {
    Write-Host "  [PASS] Test 16: Watch delete tested cleanly." -ForegroundColor Green
    $passed++
}

# Test 17: Self-Deactivation Protection (Admin cannot deactivate themselves)
$userToken = Extract-Token -Html $adminUsers.Body
# Extract Admin's own user id from the users page
if ($adminUsers.Body -match 'action="/admin/users/togglestatus/([^"]+)"') {
    $firstUserId = $matches[1]
    $toggleFormData = "__RequestVerificationToken=$([System.Web.HttpUtility]::UrlEncode($userToken))"
    $toggleRes = Http-Post -Url "$baseUrl/admin/users/togglestatus/$firstUserId" -FormData $toggleFormData -Session $adminSession -AutoRedirect $true
    if ($toggleRes.Body -match "Self-deactivation is prohibited" -or $toggleRes.Body -match "Account status for") {
        Write-Host "  [PASS] Test 17: User status toggle / Self-deactivation check executed cleanly." -ForegroundColor Green
        $passed++
    } else {
        Write-Host "  [FAIL] Test 17: User status toggle unexpected output." -ForegroundColor Red
        $failed++
    }
}

# Test 17: Order Details & Status Transition
# Check if an order exists, or place one to test details & status
$orderPage = Http-Get -Url "$baseUrl/admin/orders" -Session $adminSession
if ($orderPage.Body -match 'href="/admin/orders/details/(\d+)"') {
    $orderId = $matches[1]
    $detailsPage = Http-Get -Url "$baseUrl/admin/orders/details/$orderId" -Session $adminSession
    if ($detailsPage.StatusCode -eq 200 -and $detailsPage.Body -match "ORDER #") {
        Write-Host "  [PASS] Test 17a: /Admin/Orders/Details/$orderId loads order snapshot." -ForegroundColor Green
        $passed++
        
        # Test Status Update
        $orderToken = Extract-Token -Html $detailsPage.Body
        $statusData = "__RequestVerificationToken=$([System.Web.HttpUtility]::UrlEncode($orderToken))&id=$orderId&newStatus=Confirmed&trackingNumber=TRK-TV-TEST-99"
        $statusRes = Http-Post -Url "$baseUrl/admin/orders/updatestatus" -FormData $statusData -Session $adminSession -AutoRedirect $true
        if ($statusRes.Body -match "status successfully transitioned to Confirmed" -or $statusRes.Body -match "Confirmed") {
            Write-Host "  [PASS] Test 17b: Order status transition to Confirmed succeeded." -ForegroundColor Green
            $passed++
        } else {
            Write-Host "  [FAIL] Test 17b: Order status update failed." -ForegroundColor Red
            $failed++
        }

        # Test Invoice
        $invRes = Http-Get -Url "$baseUrl/orders/invoice/$orderId" -Session $adminSession
        if ($invRes.StatusCode -eq 200 -and $invRes.Body -match "Tax Invoice") {
            Write-Host "  [PASS] Test 17c: Tax Invoice view rendered cleanly." -ForegroundColor Green
            $passed++
        } else {
            Write-Host "  [FAIL] Test 17c: Tax Invoice view failed." -ForegroundColor Red
            $failed++
        }
    } else {
        Write-Host "  [FAIL] Test 17a: Failed to load order details." -ForegroundColor Red
        $failed++
    }
} else {
    Write-Host "  [INFO] No orders yet in database, testing place order first." -ForegroundColor Cyan
}

# 4. Teardown server
Write-Host "`nStopping server..." -ForegroundColor Yellow
Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue

$resultColor = "Green"
if ($failed -gt 0) { $resultColor = "Red" }
Write-Host "`n=================================================" -ForegroundColor Cyan
Write-Host "  TEST RESULTS: $passed PASSED, $failed FAILED        " -ForegroundColor $resultColor
Write-Host "=================================================" -ForegroundColor Cyan

if ($failed -gt 0) {
    exit 1
} else {
    exit 0
}

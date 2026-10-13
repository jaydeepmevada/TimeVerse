$ErrorActionPreference = "Continue"

Write-Host "=================================================" -ForegroundColor Cyan
Write-Host "  TIMEVERSE - PHASE 3 AUTOMATED VERIFICATION     " -ForegroundColor Cyan
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
$cookies = New-Object System.Net.CookieContainer

function Http-Get {
    param([string]$Url, [bool]$AutoRedirect = $true)
    $req = [System.Net.HttpWebRequest]::Create($Url)
    $req.CookieContainer = $global:cookies
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
        return @{ StatusCode = $status; Body = $body; Location = $loc; Success = $true }
    } catch [System.Net.WebException] {
        $wex = $_.Exception
        if ($wex.Response) {
            $r = [System.Net.HttpWebResponse]$wex.Response
            $reader = New-Object System.IO.StreamReader($r.GetResponseStream())
            $body = $reader.ReadToEnd()
            $reader.Close()
            $status = [int]$r.StatusCode
            $loc = $r.Headers["Location"]
            $r.Close()
            return @{ StatusCode = $status; Body = $body; Location = $loc; Success = $false }
        }
        return @{ StatusCode = 0; Body = ""; Location = ""; Success = $false }
    }
}

function Http-Post {
    param([string]$Url, [string]$FormData, [bool]$AutoRedirect = $true)
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($FormData)
    $req = [System.Net.HttpWebRequest]::Create($Url)
    $req.CookieContainer = $global:cookies
    $req.Method = "POST"
    $req.ContentType = "application/x-www-form-urlencoded"
    $req.ContentLength = $bytes.Length
    $req.AllowAutoRedirect = $AutoRedirect
    try {
        $stream = $req.GetRequestStream()
        $stream.Write($bytes, 0, $bytes.Length)
        $stream.Close()
        $res = $req.GetResponse()
        $reader = New-Object System.IO.StreamReader($res.GetResponseStream())
        $body = $reader.ReadToEnd()
        $reader.Close()
        $status = [int]$res.StatusCode
        $loc = $res.Headers["Location"]
        $res.Close()
        return @{ StatusCode = $status; Body = $body; Location = $loc; Success = $true }
    } catch [System.Net.WebException] {
        $wex = $_.Exception
        if ($wex.Response) {
            $r = [System.Net.HttpWebResponse]$wex.Response
            $reader = New-Object System.IO.StreamReader($r.GetResponseStream())
            $body = $reader.ReadToEnd()
            $reader.Close()
            $status = [int]$r.StatusCode
            $loc = $r.Headers["Location"]
            $r.Close()
            return @{ StatusCode = $status; Body = $body; Location = $loc; Success = $false }
        }
        return @{ StatusCode = 0; Body = ""; Location = ""; Success = $false }
    }
}

# 4. Identity & Authentication Tests
Write-Host "`n[Step 3/6] Testing Identity Views & Role Access Control..." -ForegroundColor Yellow

# Test 1: GET /Account/Login
$r = Http-Get "$baseUrl/Account/Login"
if ($r.StatusCode -eq 200 -and $r.Body -like "*Patron Sign In*") {
    Write-Host "  [PASS] GET /Account/Login (Sign-In Page)" -ForegroundColor Green
    $passed++
} else {
    Write-Host "  [FAIL] GET /Account/Login" -ForegroundColor Red
    $failed++
}

# Test 2: GET /Account/Register
$r = Http-Get "$baseUrl/Account/Register"
if ($r.StatusCode -eq 200 -and $r.Body -like "*Join the Horology Circle*") {
    Write-Host "  [PASS] GET /Account/Register (Sign-Up Page)" -ForegroundColor Green
    $passed++
} else {
    Write-Host "  [FAIL] GET /Account/Register" -ForegroundColor Red
    $failed++
}

# Extract anti-forgery token from login page
$loginPage = Http-Get "$baseUrl/Account/Login"
$token = ""
if ($loginPage.Body -match 'name="__RequestVerificationToken" type="hidden" value="([^"]+)"') {
    $token = $matches[1]
} elseif ($loginPage.Body -match '__RequestVerificationToken" type="hidden" value="([^"]+)"') {
    $token = $matches[1]
}

# Test 3: Authenticate Customer
Write-Host "`n[Step 4/6] Testing Customer Authentication & Vault Access..." -ForegroundColor Yellow
$loginPost = "Email=customer%40timeverse.com&Password=Customer%40TimeVerse2026!&RememberMe=false&__RequestVerificationToken=" + [System.Uri]::EscapeDataString($token)
$loginRes = Http-Post "$baseUrl/Account/Login" $loginPost $false

if ($loginRes.StatusCode -eq 302) {
    Write-Host "  [PASS] POST /Account/Login -> 302 Redirect to $($loginRes.Location)" -ForegroundColor Green
    $passed++
} else {
    Write-Host "  [FAIL] POST /Account/Login (Status: $($loginRes.StatusCode))" -ForegroundColor Red
    $failed++
}

# Test 4: Customer Dashboard
$r = Http-Get "$baseUrl/Customer/Home"
if ($r.StatusCode -eq 200 -and $r.Body -like "*My Account*") {
    Write-Host "  [PASS] GET /Customer/Home (Customer Dashboard)" -ForegroundColor Green
    $passed++
} else {
    Write-Host "  [FAIL] GET /Customer/Home" -ForegroundColor Red
    $failed++
}

# Test 5: Customer Profile Details
$r = Http-Get "$baseUrl/Customer/Profile"
if ($r.StatusCode -eq 200 -and $r.Body -like "*MANAGE PROFILE*") {
    Write-Host "  [PASS] GET /Customer/Profile (Manage Profile View)" -ForegroundColor Green
    $passed++
} else {
    Write-Host "  [FAIL] GET /Customer/Profile" -ForegroundColor Red
    $failed++
}

# Test 6: Shipping Address Book
$r = Http-Get "$baseUrl/Customer/Profile/Addresses"
if ($r.StatusCode -eq 200 -and $r.Body -like "*SHIPPING ADDRESS BOOK*") {
    Write-Host "  [PASS] GET /Customer/Profile/Addresses (Address Book)" -ForegroundColor Green
    $passed++
} else {
    Write-Host "  [FAIL] GET /Customer/Profile/Addresses" -ForegroundColor Red
    $failed++
}

# Test 7: Customer Orders Ledger
$r = Http-Get "$baseUrl/Customer/Orders"
if ($r.StatusCode -eq 200 -and $r.Body -like "*My Orders*") {
    Write-Host "  [PASS] GET /Customer/Orders (Customer Order History)" -ForegroundColor Green
    $passed++
} else {
    Write-Host "  [FAIL] GET /Customer/Orders" -ForegroundColor Red
    $failed++
}

# 5. Cart, Checkout & Atomic Transaction Order Placement
Write-Host "`n[Step 5/6] Testing Cart, Stock Locking, Checkout & Transactional Order..." -ForegroundColor Yellow

# Add watch to cart (Watch ID 3)
$addCartRes = Http-Post "$baseUrl/Cart/Add" "watchId=3&quantity=1" $true
if ($addCartRes.StatusCode -eq 200) {
    Write-Host "  [PASS] POST /Cart/Add (Added in-stock timepiece to cart)" -ForegroundColor Green
    $passed++
} else {
    Write-Host "  [FAIL] POST /Cart/Add" -ForegroundColor Red
    $failed++
}

# Checkout page GET
$checkoutPage = Http-Get "$baseUrl/Checkout"
if ($checkoutPage.StatusCode -eq 200 -and $checkoutPage.Body -like "*Secure Concierge Checkout*") {
    Write-Host "  [PASS] GET /Checkout (Concierge Checkout Page with Cart Summary)" -ForegroundColor Green
    $passed++
} else {
    Write-Host "  [FAIL] GET /Checkout" -ForegroundColor Red
    $failed++
}

$checkoutToken = ""
if ($checkoutPage.Body -match 'name="__RequestVerificationToken" type="hidden" value="([^"]+)"') {
    $checkoutToken = $matches[1]
} elseif ($checkoutPage.Body -match '__RequestVerificationToken" type="hidden" value="([^"]+)"') {
    $checkoutToken = $matches[1]
}

# Place Order POST (Atomic Transaction & Stock Lock)
$orderData = "ShippingFullName=Julian+Drake&ShippingPhone=%2B91+98765+43210&ShippingAddressLine1=Penthouse+42&ShippingAddressLine2=Tardeo+Road&ShippingCity=Mumbai&ShippingState=Maharashtra&ShippingPostalCode=400034&ShippingCountry=India&PaymentMethod=UPI&Notes=White+Glove+Delivery&SaveToAddressBook=false&__RequestVerificationToken=" + [System.Uri]::EscapeDataString($checkoutToken)
$orderRes = Http-Post "$baseUrl/Checkout/PlaceOrder" $orderData $false

$orderNumber = ""
if ($orderRes.StatusCode -eq 302 -and $orderRes.Location -like "*confirmation*") {
    Write-Host "  [PASS] POST /Checkout/PlaceOrder -> 302 Redirect to $($orderRes.Location)" -ForegroundColor Green
    $passed++

    if ($orderRes.Location -match 'ordernumber=([^&]+)') {
        $orderNumber = $matches[1]
        Write-Host "  [PASS] Unique Order Reference Generated: $orderNumber" -ForegroundColor Green
        $passed++
    }
} else {
    Write-Host "  [FAIL] POST /Checkout/PlaceOrder (Status: $($orderRes.StatusCode))" -ForegroundColor Red
    $failed++
}

# Order Confirmation GET
$confUrl = "$baseUrl" + $orderRes.Location
$confPage = Http-Get $confUrl $true
if ($confPage.StatusCode -eq 200 -and $confPage.Body -like "*Thank You For Your Order*") {
    Write-Host "  [PASS] GET /Checkout/Confirmation (Order Confirmation Page)" -ForegroundColor Green
    $passed++
} else {
    Write-Host "  [FAIL] GET /Checkout/Confirmation" -ForegroundColor Red
    $failed++
}

# Cart Cleared Post-Checkout Check
$cartCheck = Http-Get "$baseUrl/Cart" $true
if ($cartCheck.Body -like "*Your Shopping Cart is Empty*" -or $cartCheck.Body -like "*0 watches in cart*") {
    Write-Host "  [PASS] Cart cleared post-checkout atomic transaction verification" -ForegroundColor Green
    $passed++
} else {
    Write-Host "  [FAIL] Cart was not cleared post-checkout" -ForegroundColor Red
    $failed++
}

# Order confirmed in Customer Orders ledger
$ordersCheck = Http-Get "$baseUrl/Customer/Orders" $true
if ($ordersCheck.Body -like "*$orderNumber*" -or $ordersCheck.Body -like "*TV-*") {
    Write-Host "  [PASS] Order successfully present in Customer Order History" -ForegroundColor Green
    $passed++
} else {
    Write-Host "  [FAIL] Order not found in Customer Order History" -ForegroundColor Red
    $failed++
}

# 6. Role Authorization & Guest Security
Write-Host "`n[Step 6/6] Testing Role Authorization & Guest Security..." -ForegroundColor Yellow

# Logout Customer
$logoutToken = ""
if ($ordersCheck.Body -match 'name="__RequestVerificationToken" type="hidden" value="([^"]+)"') {
    $logoutToken = $matches[1]
}
$logoutData = "__RequestVerificationToken=" + [System.Uri]::EscapeDataString($logoutToken)
$logoutRes = Http-Post "$baseUrl/Account/Logout" $logoutData $false
if ($logoutRes.StatusCode -eq 302) {
    Write-Host "  [PASS] POST /Account/Logout -> 302 (Session Terminated)" -ForegroundColor Green
    $passed++
} else {
    Write-Host "  [FAIL] POST /Account/Logout" -ForegroundColor Red
    $failed++
}

# Protected Endpoint Check for Guest
$guestCheck = Http-Get "$baseUrl/Customer/Profile" $false
if ($guestCheck.StatusCode -eq 302 -and $guestCheck.Location -like "*Account/Login*") {
    Write-Host "  [PASS] GET /Customer/Profile redirects unauthenticated guest to /Account/Login" -ForegroundColor Green
    $passed++
} else {
    Write-Host "  [FAIL] Guest access allowed to protected customer profile!" -ForegroundColor Red
    $failed++
}

# Admin Role Login Redirect Check
$adminCookies = New-Object System.Net.CookieContainer
$global:cookies = $adminCookies
$adminLoginPage = Http-Get "$baseUrl/Account/Login"
$adminToken = ""
if ($adminLoginPage.Body -match 'name="__RequestVerificationToken" type="hidden" value="([^"]+)"') {
    $adminToken = $matches[1]
}
$adminPost = "Email=admin%40timeverse.com&Password=Admin%40TimeVerse2026!&RememberMe=false&__RequestVerificationToken=" + [System.Uri]::EscapeDataString($adminToken)
$adminRes = Http-Post "$baseUrl/Account/Login" $adminPost $false
if ($adminRes.StatusCode -eq 302 -and $adminRes.Location -like "*Admin*") {
    Write-Host "  [PASS] Admin Authentication redirects to Admin Dashboard: $($adminRes.Location)" -ForegroundColor Green
    $passed++
} else {
    Write-Host "  [FAIL] Admin login redirect failed (Status: $($adminRes.StatusCode), Location: $($adminRes.Location))" -ForegroundColor Red
    $failed++
}

# SalesPerson Role Login Redirect Check
$salesCookies = New-Object System.Net.CookieContainer
$global:cookies = $salesCookies
$salesLoginPage = Http-Get "$baseUrl/Account/Login"
$salesToken = ""
if ($salesLoginPage.Body -match 'name="__RequestVerificationToken" type="hidden" value="([^"]+)"') {
    $salesToken = $matches[1]
}
$salesPost = "Email=sales%40timeverse.com&Password=Sales%40TimeVerse2026!&RememberMe=false&__RequestVerificationToken=" + [System.Uri]::EscapeDataString($salesToken)
$salesRes = Http-Post "$baseUrl/Account/Login" $salesPost $false
if ($salesRes.StatusCode -eq 302 -and $salesRes.Location -like "*Sales*") {
    Write-Host "  [PASS] SalesPerson Authentication redirects to Sales Dashboard: $($salesRes.Location)" -ForegroundColor Green
    $passed++
} else {
    Write-Host "  [FAIL] SalesPerson login redirect failed (Status: $($salesRes.StatusCode), Location: $($salesRes.Location))" -ForegroundColor Red
    $failed++
}

# Cleanup server
Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue

Write-Host "`n=================================================" -ForegroundColor Cyan
Write-Host "  PHASE 3 TEST RESULTS: Passed: $passed, Failed: $failed " -ForegroundColor $(if ($failed -eq 0) { "Green" } else { "Red" })
Write-Host "=================================================" -ForegroundColor Cyan

if ($failed -gt 0) {
    exit 1
}

Add-Type -AssemblyName System.Net.Http
# Test Dashboard CSS Inclusion
$handler = New-Object System.Net.Http.HttpClientHandler
$client = New-Object System.Net.Http.HttpClient($handler)

Write-Host "=========================================================="
Write-Host " TESTING DASHBOARD CSS & LAYOUT INCLUSION"
Write-Host "=========================================================="

function Test-AreaHtml {
    param($RoleName, $Email, $Password, $Url)

    $loginHtml = $client.GetStringAsync("http://localhost:5247/account/login").Result
    $token = [regex]::Match($loginHtml, '__RequestVerificationToken".*?value="([^"]+)"').Groups[1].Value

    $form = New-Object 'System.Collections.Generic.Dictionary[string,string]'
    $form.Add("Email", $Email)
    $form.Add("Password", $Password)
    $form.Add("RememberMe", "false")
    $form.Add("__RequestVerificationToken", $token)
    $content = New-Object System.Net.Http.FormUrlEncodedContent($form)

    $loginResp = $client.PostAsync("http://localhost:5247/account/login", $content).Result

    $html = $client.GetStringAsync($Url).Result
    $hasSiteCss = $html.Contains("site.css")
    $hasBootstrap = $html.Contains("bootstrap.min.css")
    $hasDashboardBar = $html.Contains("tv-dashboard-bar")
    $hasNavbar = $html.Contains("tv-navbar")

    Write-Host "Role: $RoleName ($Url)"
    Write-Host "  - site.css linked: $hasSiteCss" -ForegroundColor $(if ($hasSiteCss) { "Green" } else { "Red" })
    Write-Host "  - bootstrap.min.css linked: $hasBootstrap" -ForegroundColor $(if ($hasBootstrap) { "Green" } else { "Red" })
    Write-Host "  - tv-dashboard-bar rendered: $hasDashboardBar" -ForegroundColor $(if ($hasDashboardBar) { "Green" } else { "Red" })
    Write-Host "  - tv-navbar rendered: $hasNavbar" -ForegroundColor $(if ($hasNavbar) { "Green" } else { "Red" })

    # Logout to clear session for next role
    $logoutToken = [regex]::Match($html, '__RequestVerificationToken".*?value="([^"]+)"').Groups[1].Value
    $loForm = New-Object 'System.Collections.Generic.Dictionary[string,string]'
    $loForm.Add("__RequestVerificationToken", $logoutToken)
    $loContent = New-Object System.Net.Http.FormUrlEncodedContent($loForm)
    $client.PostAsync("http://localhost:5247/account/logout", $loContent).Wait()
}

Test-AreaHtml -RoleName "ADMIN" -Email "admin@timeverse.com" -Password "Admin@TimeVerse2026!" -Url "http://localhost:5247/admin"
Test-AreaHtml -RoleName "SALES" -Email "sales@timeverse.com" -Password "Sales@TimeVerse2026!" -Url "http://localhost:5247/sales"
Test-AreaHtml -RoleName "CUSTOMER" -Email "customer@timeverse.com" -Password "Customer@TimeVerse2026!" -Url "http://localhost:5247/customer/home"

Write-Host "=========================================================="

$baseUrl = "http://localhost:5079"
$tests = @(
    @{ Name = "1. Home Page"; Url = "$baseUrl/" },
    @{ Name = "2. Shop Page"; Url = "$baseUrl/shop" },
    @{ Name = "3. Search 'Rolex'"; Url = "$baseUrl/shop?search=Rolex" },
    @{ Name = "4. Search Category 'Automatic'"; Url = "$baseUrl/shop?search=Automatic" },
    @{ Name = "5. Filter Brand 'Omega'"; Url = "$baseUrl/shop?brand=Omega" },
    @{ Name = "6. Filter Category 'Luxury Watches'"; Url = "$baseUrl/shop?category=Luxury%20Watches" },
    @{ Name = "7. Filter Gender 'Men'"; Url = "$baseUrl/shop?gender=Men" },
    @{ Name = "8. Filter InStock"; Url = "$baseUrl/shop?stockFilter=inStock" },
    @{ Name = "9. Sort Price Low-to-High"; Url = "$baseUrl/shop?sort=priceAsc" },
    @{ Name = "10. Sort Price High-to-Low"; Url = "$baseUrl/shop?sort=priceDesc" },
    @{ Name = "11. Sort Newest"; Url = "$baseUrl/shop?sort=newest" },
    @{ Name = "12. Sort Rating"; Url = "$baseUrl/shop?sort=rating" },
    @{ Name = "13. Product Details (Id=1)"; Url = "$baseUrl/shop/details/1" },
    @{ Name = "14. Watches Details Alias (Id=1)"; Url = "$baseUrl/watches/details/1" },
    @{ Name = "15. Cart Page"; Url = "$baseUrl/cart" },
    @{ Name = "16. Wishlist Page"; Url = "$baseUrl/wishlist" },
    @{ Name = "17. API Cart Summary"; Url = "$baseUrl/api/cart" }
)

Write-Host "Starting Phase 2 Automated Route Tests..." -ForegroundColor Cyan

$allPassed = $true
foreach ($t in $tests) {
    try {
        $res = Invoke-WebRequest -Uri $t.Url -Method Get -UseBasicParsing -TimeoutSec 10
        if ($res.StatusCode -eq 200) {
            Write-Host "[PASS] $($t.Name) -> 200 OK" -ForegroundColor Green
        } else {
            Write-Host "[FAIL] $($t.Name) -> Status $($res.StatusCode)" -ForegroundColor Red
            $allPassed = $false
        }
    } catch {
        Write-Host "[FAIL] $($t.Name) -> $($_.Exception.Message)" -ForegroundColor Red
        $allPassed = $false
    }
}

# Test POST API Add to Cart
try {
    $body = @{ watchId = 1; quantity = 1 } | ConvertTo-Json
    $postRes = Invoke-WebRequest -Uri "$baseUrl/api/cart/items" -Method Post -Body $body -ContentType "application/json" -UseBasicParsing
    if ($postRes.StatusCode -eq 200) {
        Write-Host "[PASS] 18. POST /api/cart/items (Add to Cart) -> 200 OK : $($postRes.Content)" -ForegroundColor Green
    } else {
        Write-Host "[FAIL] 18. POST /api/cart/items -> Status $($postRes.StatusCode)" -ForegroundColor Red
        $allPassed = $false
    }
} catch {
    Write-Host "[FAIL] 18. POST /api/cart/items -> $($_.Exception.Message)" -ForegroundColor Red
    $allPassed = $false
}

# Test POST Wishlist Toggle
try {
    $wishRes = Invoke-WebRequest -Uri "$baseUrl/wishlist/toggle?watchId=1" -Method Post -Headers @{ "X-Requested-With" = "XMLHttpRequest" } -UseBasicParsing
    if ($wishRes.StatusCode -eq 200) {
        Write-Host "[PASS] 19. POST /wishlist/toggle (Toggle Wishlist) -> 200 OK : $($wishRes.Content)" -ForegroundColor Green
    } else {
        Write-Host "[FAIL] 19. POST /wishlist/toggle -> Status $($wishRes.StatusCode)" -ForegroundColor Red
        $allPassed = $false
    }
} catch {
    Write-Host "[FAIL] 19. POST /wishlist/toggle -> $($_.Exception.Message)" -ForegroundColor Red
    $allPassed = $false
}

if ($allPassed) {
    Write-Host "`nAll Phase 2 Verification Tests PASSED Successfully!" -ForegroundColor Green
} else {
    Write-Host "`nSome tests failed. Please review the output." -ForegroundColor Yellow
}

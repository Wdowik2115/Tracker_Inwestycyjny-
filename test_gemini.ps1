$apiKey = "AQ.Ab8RN6IF-3YKDfextOiHSUTjNBiKb6Q3AA6EfFqZXXUfBsrY4Q"
$url = "https://generativelanguage.googleapis.com/v1/models?key=$apiKey"

try {
    Write-Host "Pobieranie listy modeli..." -ForegroundColor Cyan
    $response = Invoke-RestMethod -Uri $url -Method Get
    Write-Host "Sukces! Dostępne modele:" -ForegroundColor Green
    $response.models | Select-Object -First 5 | ForEach-Object { $_.name }
} catch {
    Write-Host "Błąd!" -ForegroundColor Red
    $_.Exception.Message
}

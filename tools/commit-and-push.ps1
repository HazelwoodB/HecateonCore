param(
    [string]$Message = "chore: sync local changes"
)

$ErrorActionPreference = "Stop"

Write-Host "[1/6] Checking git repo..."
git rev-parse --is-inside-work-tree | Out-Null

Write-Host "[2/6] Restore..."
dotnet restore .\Lullaby.slnx

Write-Host "[3/6] Build..."
dotnet build .\Lullaby.slnx -c Release -v minimal

Write-Host "[4/6] Test..."
dotnet test .\Lullaby\Lullaby.Tests\Lullaby.Tests.csproj -c Release -v minimal

Write-Host "[5/6] Commit..."
git add -A
if ((git status --porcelain).Length -eq 0) {
    Write-Host "No changes to commit."
    exit 0
}
git commit -m $Message

Write-Host "[6/6] Push..."
git push

Write-Host "Done."
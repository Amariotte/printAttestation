# Script de désinstallation du service Windows pour PrintAttestation API
# À exécuter en tant qu'administrateur

param(
	[string]$ServiceName = "PrintAttestationAPI"
)

# Vérifier si on est administrateur
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")

if (-not $isAdmin) {
	Write-Host "❌ Ce script doit être exécuté en tant qu'administrateur" -ForegroundColor Red
	Write-Host "Veuillez relancer PowerShell en mode administrateur." -ForegroundColor Yellow
	exit 1
}

# Vérifier si le service existe
$service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue

if (-not $service) {
	Write-Host "❌ Le service '$ServiceName' n'existe pas." -ForegroundColor Red
	exit 1
}

Write-Host "`n=== Désinstallation du Service Windows ===" -ForegroundColor Cyan
Write-Host "Nom du service: $ServiceName`n" -ForegroundColor White

# Confirmation
$response = Read-Host "Êtes-vous sûr de vouloir désinstaller ce service? (O/N)"

if ($response -ne "O" -and $response -ne "o") {
	Write-Host "Désinstallation annulée." -ForegroundColor Yellow
	exit 0
}

try {
	# Vérifier si le service est en cours d'exécution
	if ($service.Status -eq "Running") {
		Write-Host "Arrêt du service..." -ForegroundColor Yellow
		Stop-Service -Name $ServiceName -Force -ErrorAction Stop
		Start-Sleep -Seconds 2
	}

	# Supprimer le service
	Write-Host "Suppression du service..." -ForegroundColor Yellow
	sc.exe delete $ServiceName | Out-Null
	Start-Sleep -Seconds 2

	# Vérifier si le service est bien supprimé
	$serviceAfter = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue

	if ($null -eq $serviceAfter) {
		Write-Host "✅ Le service a été supprimé avec succès!" -ForegroundColor Green
	} else {
		Write-Host "⚠️  Le service existe toujours. Une deuxième tentative peut être nécessaire." -ForegroundColor Yellow
	}
}
catch {
	Write-Host "❌ Erreur lors de la suppression du service: $_" -ForegroundColor Red
	exit 1
}

Write-Host "`n💡 Pour vérifier que le service est bien supprimé:" -ForegroundColor Cyan
Write-Host "   - Services Windows (services.msc)" -ForegroundColor Gray
Write-Host "   - Commande: Get-Service -Name '$ServiceName'" -ForegroundColor Gray

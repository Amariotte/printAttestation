# Script d'installation du service Windows pour PrintAttestation API
# À exécuter en tant qu'administrateur

param(
	[string]$ServiceName = "PrintAttestationAPI",
	[string]$DisplayName = "Print Attestation API Service",
	[string]$BinaryPath = "",
	[string]$StartupType = "Automatic",
	[string]$ServiceAccount = "LocalSystem",
	[string]$ServicePassword = ""
)

# Vérifier si on est administrateur
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")

if (-not $isAdmin) {
	Write-Host "❌ Ce script doit être exécuté en tant qu'administrateur" -ForegroundColor Red
	Write-Host "Veuillez relancer PowerShell en mode administrateur." -ForegroundColor Yellow
	exit 1
}

# Si BinaryPath n'est pas fourni, demander le chemin
if ([string]::IsNullOrEmpty($BinaryPath)) {
	Write-Host "`n=== Installation du Service Windows - PrintAttestation API ===" -ForegroundColor Cyan
	Write-Host "`nVeruillez entrer le chemin complet vers le fichier exécutable (.exe):"
	Write-Host "Exemple: C:\Applications\PrintAttestation\print_attestation.exe`n"
	$BinaryPath = Read-Host "Chemin vers l'exécutable"
}

# Valider que le fichier existe
if (-not (Test-Path $BinaryPath)) {
	Write-Host "❌ Le fichier n'existe pas: $BinaryPath" -ForegroundColor Red
	exit 1
}

# Vérifier si le service existe déjà
$existingService = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue

if ($existingService) {
	Write-Host "⚠️  Le service '$ServiceName' existe déjà." -ForegroundColor Yellow
	$response = Read-Host "Voulez-vous le remplacer? (O/N)"

	if ($response -ne "O" -and $response -ne "o") {
		Write-Host "Installation annulée." -ForegroundColor Yellow
		exit 0
	}

	# Arrêter et supprimer le service existant
	Write-Host "Arrêt du service..." -ForegroundColor Yellow
	Stop-Service -Name $ServiceName -Force -ErrorAction SilentlyContinue
	Start-Sleep -Seconds 2

	Write-Host "Suppression du service existant..." -ForegroundColor Yellow
	sc.exe delete $ServiceName | Out-Null
	Start-Sleep -Seconds 2
}

try {
	# Créer le service
	Write-Host "Création du service Windows..." -ForegroundColor Green

	if ($ServiceAccount -eq "LocalSystem") {
		# Créer le service avec le compte LocalSystem
		New-Service -Name $ServiceName `
				   -DisplayName $DisplayName `
				   -BinaryPathName $BinaryPath `
				   -StartupType $StartupType `
				   -ErrorAction Stop | Out-Null
	} else {
		# Créer le service avec un compte spécifique
		$secPassword = ConvertTo-SecureString $ServicePassword -AsPlainText -Force
		$credential = New-Object System.Management.Automation.PSCredential ($ServiceAccount, $secPassword)

		New-Service -Name $ServiceName `
				   -DisplayName $DisplayName `
				   -BinaryPathName $BinaryPath `
				   -StartupType $StartupType `
				   -Credential $credential `
				   -ErrorAction Stop | Out-Null
	}

	Write-Host "✅ Service créé avec succès!" -ForegroundColor Green

	# Démarrer le service
	Write-Host "Démarrage du service..." -ForegroundColor Green
	Start-Service -Name $ServiceName -ErrorAction Stop
	Start-Sleep -Seconds 2

	# Vérifier le statut du service
	$service = Get-Service -Name $ServiceName
	if ($service.Status -eq "Running") {
		Write-Host "✅ Le service est en cours d'exécution!" -ForegroundColor Green
	} else {
		Write-Host "⚠️  Le service a été créé mais n'est pas en cours d'exécution." -ForegroundColor Yellow
		Write-Host "   Statut: $($service.Status)" -ForegroundColor Yellow
	}

	# Afficher les informations du service
	Write-Host "`n=== Informations du Service ===" -ForegroundColor Cyan
	Write-Host "Nom du service: $ServiceName"
	Write-Host "Nom d'affichage: $DisplayName"
	Write-Host "Type de démarrage: $StartupType"
	Write-Host "Chemin: $BinaryPath"
	Write-Host "Statut: $($service.Status)"

	Write-Host "`n💡 Pour gérer le service, vous pouvez utiliser:" -ForegroundColor Cyan
	Write-Host "   - Services Windows (services.msc)"
	Write-Host "   - Commande: Get-Service -Name '$ServiceName'"
	Write-Host "   - Commande: Stop-Service -Name '$ServiceName'"
	Write-Host "   - Commande: Start-Service -Name '$ServiceName'"

}
catch {
	Write-Host "❌ Erreur lors de la création du service: $_" -ForegroundColor Red
	exit 1
}

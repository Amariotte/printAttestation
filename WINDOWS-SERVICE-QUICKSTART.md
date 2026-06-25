# Guide Rapide - Service Windows

## 🚀 Installation rapide (2 minutes)

```powershell
# 1. Compiler l'application
dotnet publish -c Release -o "C:\Applications\PrintAttestation"

# 2. Exécuter le script en tant qu'administrateur
.\scripts\Install-WindowsService.ps1

# 3. Entrer le chemin: C:\Applications\PrintAttestation\print_attestation.exe
# ✅ Service créé et démarré!
```

## 📊 Vérifier le statut

```powershell
Get-Service -Name PrintAttestationAPI
```

## ⏹️ Arrêter / Redémarrer

```powershell
Stop-Service -Name PrintAttestationAPI    # Arrêter
Start-Service -Name PrintAttestationAPI   # Démarrer
Restart-Service -Name PrintAttestationAPI # Redémarrer
```

## 🗑️ Désinstaller

```powershell
# Exécuter le script en tant qu'administrateur
.\scripts\Uninstall-WindowsService.ps1

# Ou la commande directe:
Stop-Service -Name PrintAttestationAPI -Force
sc.exe delete PrintAttestationAPI
```

## 📝 Logs

Fichier: `C:\log\print-attestation\log-.txt` (à configurer dans appsettings.json)

```powershell
# Afficher les 50 dernières lignes
Get-Content "C:\log\print-attestation\log-.txt" -Tail 50

# Ou en temps réel
Get-Content "C:\log\print-attestation\log-.txt" -Wait
```

## ⚙️ Configuration

Fichier requis: `C:\Applications\PrintAttestation\appsettings.json`

Exemple de structure:
```json
{
  "Logging": {
	"LogLevel": {
	  "Default": "Information"
	}
  },
  "Serilog": {
	"MinimumLevel": "Information"
  }
}
```

## ✔️ Checklist avant production

- [ ] Application testée en mode console
- [ ] Répertoire `C:\log\print-attestation\` existe
- [ ] Permissions d'écriture pour le compte de service
- [ ] `appsettings.json` est dans le même répertoire que l'exécutable
- [ ] Base de données accessible depuis le serveur
- [ ] Service démarre sans erreur: `Start-Service -Name PrintAttestationAPI`
- [ ] Logs générés correctement
- [ ] Type de démarrage configuré: `Automatic` ou `Manual`

## 🔧 Gestionnaire de services Windows

Appuyer sur `Win + R` et taper `services.msc` pour une interface graphique.

---

**Pour une documentation complète**, voir `README-WINDOWS-SERVICE.md`

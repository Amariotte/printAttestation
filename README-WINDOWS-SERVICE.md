# Guide d'Installation - Service Windows PrintAttestation API

## 📋 Table des matières
1. [Prérequis](#prérequis)
2. [Installation](#installation)
3. [Désinstallation](#désinstallation)
4. [Gestion du service](#gestion-du-service)
5. [Dépannage](#dépannage)
6. [Configuration](#configuration)

---

## Prérequis

- **Windows Server 2019+** ou **Windows 10/11 Pro/Enterprise**
- **Compte administrateur** pour installer/gérer le service
- Application **PrintAttestation API** compilée en Release
- **PowerShell 5.0+** (intégré à Windows 10+)

---

## Installation

### Option 1 : Utiliser le script PowerShell (Recommandé)

1. **Compiler l'application en mode Release:**
   ```powershell
   dotnet publish -c Release -o "C:\Applications\PrintAttestation"
   ```

2. **Ouvrir PowerShell en tant qu'administrateur:**
   - Clic droit sur PowerShell → "Exécuter en tant qu'administrateur"

3. **Exécuter le script d'installation:**
   ```powershell
   .\scripts\Install-WindowsService.ps1
   ```

4. **Suivre les instructions:**
   - Entrer le chemin complet vers le fichier `print_attestation.exe`
   - Exemple: `C:\Applications\PrintAttestation\print_attestation.exe`

### Option 2 : Utiliser la commande sc.exe

```powershell
sc.exe create PrintAttestationAPI ^
  binPath= "C:\Applications\PrintAttestation\print_attestation.exe" ^
  DisplayName= "Print Attestation API Service" ^
  start= auto
```

### Option 3 : Installation avancée avec compte de service

```powershell
# Créer un compte de service (optionnel)
New-LocalUser -Name "PrintAttestation" -Password (ConvertTo-SecureString "MonPassword123!" -AsPlainText -Force) -FullName "Print Attestation Service" -Description "Compte de service pour PrintAttestation API"

# Installer le service avec ce compte
sc.exe create PrintAttestationAPI ^
  binPath= "C:\Applications\PrintAttestation\print_attestation.exe" ^
  DisplayName= "Print Attestation API Service" ^
  start= auto ^
  obj= ".\PrintAttestation"
```

---

## Désinstallation

### Utiliser le script PowerShell

1. **Ouvrir PowerShell en tant qu'administrateur**

2. **Exécuter le script:**
   ```powershell
   .\scripts\Uninstall-WindowsService.ps1
   ```

### Utiliser la commande sc.exe

```powershell
# D'abord arrêter le service
Stop-Service -Name PrintAttestationAPI -Force

# Ensuite le supprimer
sc.exe delete PrintAttestationAPI
```

---

## Gestion du Service

### Démarrer le service

```powershell
Start-Service -Name PrintAttestationAPI
```

Ou via Services Windows:
- Appuyer sur `Win + R` → taper `services.msc` → Trouver "Print Attestation API Service" → Clic droit → "Démarrer"

### Arrêter le service

```powershell
Stop-Service -Name PrintAttestationAPI
```

### Redémarrer le service

```powershell
Restart-Service -Name PrintAttestationAPI
```

### Vérifier le statut

```powershell
Get-Service -Name PrintAttestationAPI
```

Ou pour plus de détails:
```powershell
Get-Service -Name PrintAttestationAPI | Select-Object -Property *
```

### Modifier le type de démarrage

**Démarrage automatique:**
```powershell
Set-Service -Name PrintAttestationAPI -StartupType Automatic
```

**Démarrage manuel:**
```powershell
Set-Service -Name PrintAttestationAPI -StartupType Manual
```

**Désactiver le service:**
```powershell
Set-Service -Name PrintAttestationAPI -StartupType Disabled
```

---

## Configuration

### Configuration du démarrage

Le fichier `appsettings.json` doit être dans le même répertoire que l'exécutable pour que le service puisse lire la configuration:

```
C:\Applications\PrintAttestation\
├── print_attestation.exe
├── appsettings.json
├── appsettings.Development.json
└── ... (autres fichiers de l'application)
```

### Configuration des logs

Les logs sont écrits dans: `/log/print-attestation/log-.txt`

**⚠️ Important:** Assurez-vous que le répertoire existe et que le compte de service a les permissions d'écriture:

```powershell
# Créer le répertoire s'il n'existe pas
New-Item -ItemType Directory -Force -Path "C:\log\print-attestation"

# Accorder les permissions au compte de service (LocalSystem ou autre)
$acl = Get-Acl "C:\log\print-attestation"
$rule = New-Object System.Security.AccessControl.FileSystemAccessRule("SYSTEM", "Modify", "ContainerInherit,ObjectInherit", "None", "Allow")
$acl.AddAccessRule($rule)
Set-Acl "C:\log\print-attestation" $acl
```

### Variables d'environnement

Le service hérite les variables d'environnement du système. Pour définir des variables spécifiques:

```powershell
# Voir les variables existantes du service
[Environment]::GetEnvironmentVariables("Machine") | Sort-Object

# Ajouter une variable d'environnement
[Environment]::SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Production", "Machine")
```

---

## Dépannage

### Le service ne démarre pas

1. **Vérifier les logs:**
   ```powershell
   Get-Content "C:\log\print-attestation\log-.txt" -Tail 50
   ```

2. **Vérifier le chemin de l'exécutable:**
   ```powershell
   Get-Service PrintAttestationAPI | Select-Object -Property *
   # Vérifier la propriété "PathName"
   ```

3. **Tester l'application en mode console:**
   ```powershell
   C:\Applications\PrintAttestation\print_attestation.exe
   ```

4. **Vérifier les permissions:**
   - Le compte de service doit avoir accès au répertoire de l'application
   - Le compte de service doit avoir accès au répertoire des logs

### Le service s'arrête immédiatement après démarrage

- **Vérifier les erreurs dans les logs** (voir ci-dessus)
- **Vérifier la configuration appsettings.json:**
  ```powershell
  Test-Path "C:\Applications\PrintAttestation\appsettings.json"
  ```
- **Vérifier la base de données:**
  - La connexion à la base de données est-elle accessible depuis le serveur?
  - Les identifiants dans appsettings.json sont-ils corrects?

### Impossible de démarrer le service (Accès refusé)

```powershell
# Exécuter PowerShell en tant qu'administrateur
# Puis réessayer les commandes

# Ou utiliser le script avec mode administrateur explicite
Start-Process powershell -ArgumentList "-File .\scripts\Install-WindowsService.ps1" -Verb RunAs
```

### Le service n'a pas accès au réseau

- Si le service utilise un compte personnalisé, l'authentification réseau peut être différente
- Tester les connexions depuis le compte de service:
  ```powershell
  # Lancer une tâche avec le compte de service
  Invoke-Command -ScriptBlock { Test-Connection example.com } -AsJob
  ```

### Voir l'historique des événements

```powershell
# Afficher les événements du service
Get-EventLog -LogName System -Source Service* | Where-Object { $_.Message -like "*PrintAttestation*" }

# Ou dans l'Observateur d'événements Windows
# -> Annuaire: Windows Journaux → Système
# -> Rechercher les événements avec "PrintAttestation"
```

---

## Commandes rapides

```powershell
# Résumé complet
$service = Get-Service -Name PrintAttestationAPI
Write-Host "Nom: $($service.Name)"
Write-Host "Statut: $($service.Status)"
Write-Host "Type: $($service.StartType)"

# Redémarrer après mise à jour
Restart-Service -Name PrintAttestationAPI -Force

# Désactiver temporairement
Set-Service -Name PrintAttestationAPI -StartupType Disabled

# Réactiver
Set-Service -Name PrintAttestationAPI -StartupType Automatic
Start-Service -Name PrintAttestationAPI
```

---

## Notes de sécurité

1. **Compte de service:**
   - Préférer un compte de service dédié plutôt que SYSTEM si possible
   - Limiter les permissions au minimum nécessaire

2. **Fichiers sensibles:**
   - Protéger le fichier `appsettings.json` (contient les connexions DB, etc.)
   - Vérifier les permissions d'accès

3. **Logs:**
   - Implémenter une rotation des logs pour économiser l'espace disque
   - Configurer avec Serilog dans `appsettings.json`

4. **Mise à jour:**
   - Arrêter le service avant de mettre à jour l'application
   - Tester la nouvelle version en mode console avant de redémarrer le service

---

## Références

- [Microsoft - Windows Services](https://docs.microsoft.com/en-us/dotnet/framework/windows-services/introduction-to-windows-service-applications)
- [Microsoft - Extensions.Hosting.WindowsServices](https://github.com/dotnet/extensions/tree/main/src/Hosting/Hosting.WindowsServices)
- [PowerShell - Get-Service](https://docs.microsoft.com/en-us/powershell/module/microsoft.powershell.management/get-service)
- [Serilog - Configuration](https://github.com/serilog/serilog-settings-configuration)

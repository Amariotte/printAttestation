# Migration vers MySQL - Guide complet

## ✅ Modifications effectuées

### 1. Package NuGet
- ✅ **Ajouté** : `Pomelo.EntityFrameworkCore.MySql` version 8.0.2
- ✅ **Remplacé** : `Npgsql.EntityFrameworkCore.PostgreSQL` (PostgreSQL)

### 2. Configuration Program.cs
```csharp
// Avant (PostgreSQL)
options.UseNpgsql(builder.Configuration.GetConnectionString("localConnectionPostgre"))

// Après (MySQL)
options.UseMySql(
	builder.Configuration.GetConnectionString("MySqlConnection"),
	ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("MySqlConnection"))
)
```

### 3. Chaînes de connexion

**appsettings.json (Production)**
```json
{
  "ConnectionStrings": {
	"MySqlConnection": "Server=localhost;Port=3306;Database=bd_print;User=root;Password=root;"
  }
}
```

**appsettings.Development.json (Développement)**
```json
{
  "ConnectionStrings": {
	"MySqlConnection": "Server=localhost;Port=3306;Database=bd_print_dev;User=root;Password=root;"
  }
}
```

---

## 📋 Étapes suivantes à effectuer MANUELLEMENT

### Étape 1 : Supprimer les anciennes migrations PostgreSQL

Dans PowerShell ou Terminal :
```powershell
Remove-Item -Path "Migrations" -Recurse -Force
```

Ou manuellement : Supprimez le dossier `Migrations` dans l'explorateur de fichiers.

---

### Étape 2 : Créer une nouvelle migration initiale pour MySQL

```powershell
dotnet ef migrations add Initial_MySQL_Migration
```

---

### Étape 3 : Mettre à jour/Créer la base de données MySQL

```powershell
dotnet ef database update
```

Cette commande va :
- Créer la base de données `bd_print_dev` (en mode Development)
- Créer toutes les tables (t_user, t_histo_email, t_histo_sms, etc.)
- Appliquer tous les index et contraintes

---

## 🔧 Configuration MySQL

### Prérequis
Assurez-vous que MySQL est installé et démarré :

```powershell
# Vérifier que MySQL est actif
mysql --version

# Se connecter à MySQL
mysql -u root -p
```

### Créer les bases de données (si nécessaire)

```sql
-- Base de développement
CREATE DATABASE bd_print_dev CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

-- Base de production
CREATE DATABASE bd_print CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

-- Créer un utilisateur dédié (recommandé)
CREATE USER 'ask_user'@'localhost' IDENTIFIED BY 'VotreMotDePasseSecurise';
GRANT ALL PRIVILEGES ON bd_print.* TO 'ask_user'@'localhost';
GRANT ALL PRIVILEGES ON bd_print_dev.* TO 'ask_user'@'localhost';
FLUSH PRIVILEGES;
```

### Mettre à jour les chaînes de connexion (si utilisateur dédié)

```json
{
  "ConnectionStrings": {
	"MySqlConnection": "Server=localhost;Port=3306;Database=bd_print_dev;User=ask_user;Password=VotreMotDePasseSecurise;"
  }
}
```

---

## 🧪 Tester la connexion

### 1. Démarrer l'application

```powershell
dotnet run --project print_attestation.csproj
```

### 2. Vérifier les logs

Recherchez dans la sortie console :
```
✅ Application started successfully
✅ Database connection established
```

### 3. Tester l'endpoint d'inscription

```bash
POST http://localhost:5002/api/auth/register
Content-Type: application/json

{
  "nom": "Test",
  "prenom": "User",
  "email": "test@example.com",
  "telephone": "22312345678"
}
```

### 4. Vérifier en base de données

```sql
USE bd_print_dev;

-- Vérifier les tables créées
SHOW TABLES;

-- Vérifier les utilisateurs créés
SELECT * FROM t_user;

-- Vérifier les emails envoyés
SELECT * FROM t_histo_email;
```

---

## 🔍 Différences MySQL vs PostgreSQL

### Types de données
| PostgreSQL | MySQL |
|------------|-------|
| `timestamp with time zone` | `DATETIME(6)` |
| `boolean` | `TINYINT(1)` |
| `text` | `LONGTEXT` |
| `serial` | `INT AUTO_INCREMENT` |

### Timestamps UTC
✅ **Bonne nouvelle** : La conversion automatique DateTime UTC du `askContext` fonctionne aussi avec MySQL !

MySQL stocke les `DATETIME` sans timezone, mais notre code garantit que toutes les dates sont en UTC avant insertion.

---

## ⚠️ Points d'attention

### 1. Sensibilité à la casse
MySQL sur Windows est **insensible à la casse** par défaut pour les noms de tables.
MySQL sur Linux est **sensible à la casse**.

### 2. Longueur des index
MySQL a une limite de 767 octets pour les clés d'index sur les colonnes VARCHAR.
Si vous avez des colonnes VARCHAR(500) avec index, cela peut poser problème avec utf8mb4 (4 octets par caractère).

### 3. Performance
MySQL est généralement plus rapide pour les lectures simples, PostgreSQL pour les requêtes complexes.

---

## 🚀 Commandes utiles

### Migrations
```powershell
# Créer une nouvelle migration
dotnet ef migrations add NomDeLaMigration

# Appliquer les migrations
dotnet ef database update

# Revenir à une migration spécifique
dotnet ef database update NomDeLaMigration

# Supprimer la dernière migration (si non appliquée)
dotnet ef migrations remove

# Lister les migrations
dotnet ef migrations list
```

### Base de données
```powershell
# Supprimer et recréer la base
dotnet ef database drop --force
dotnet ef database update
```

---

## 📝 Checklist finale

- [ ] Dossier `Migrations` supprimé
- [ ] MySQL installé et démarré
- [ ] Base de données créée
- [ ] Migration initiale créée (`dotnet ef migrations add Initial_MySQL_Migration`)
- [ ] Migration appliquée (`dotnet ef database update`)
- [ ] Application démarre sans erreur
- [ ] Test d'inscription réussi
- [ ] Données visibles en base MySQL

---

## 🆘 En cas de problème

### Erreur : "Cannot connect to MySQL server"
```
✅ Solution : Vérifier que MySQL est démarré
services.msc (Windows) → Chercher MySQL → Démarrer
```

### Erreur : "Access denied for user"
```
✅ Solution : Vérifier les identifiants dans la chaîne de connexion
mysql -u root -p  # Tester la connexion manuellement
```

### Erreur : "Unknown database"
```
✅ Solution : Créer la base de données
CREATE DATABASE bd_print_dev;
```

### Erreur lors de la migration
```
✅ Solution : Supprimer le dossier Migrations et recréer
Remove-Item -Path "Migrations" -Recurse -Force
dotnet ef migrations add Initial_MySQL_Migration
```

---

## 📚 Documentation
- [Pomelo.EntityFrameworkCore.MySql](https://github.com/PomeloFoundation/Pomelo.EntityFrameworkCore.MySql)
- [Entity Framework Core](https://docs.microsoft.com/ef/core/)
- [MySQL Documentation](https://dev.mysql.com/doc/)

---

**Statut** : ✅ Configuration terminée, prêt pour les migrations !

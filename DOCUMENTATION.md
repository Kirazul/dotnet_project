# InvestPortfolio — Documentation Complète du Projet

## Module : Programmation .NET C#

**Année universitaire** : 2025-2026
**Spécialité** : 4ème Génie Informatique — Data Science
**Sujet** : Gestion simple d'un portefeuille d'investissement (achat/vente d'actions et crypto-monnaies)

---

## Table des Matières

1. [Vue d'Ensemble du Projet](#1-vue-densemble-du-projet)
2. [Architecture Générale](#2-architecture-générale)
3. [Stack Technique](#3-stack-technique)
4. [Structure du Projet](#4-structure-du-projet)
5. [Modèle de Données (TP5 - Chapitre 5)](#5-modèle-de-données-tp5---chapitre-5)
6. [Couche Données : DbContext (TP5)](#6-couche-données--dbcontext-tp5)
7. [Couche Services (TP4, TP6, TP10)](#7-couche-services-tp4-tp6-tp10)
8. [Composants UI Réutilisables (TP8)](#8-composants-ui-réutilisables-tp8)
9. [Pages (TP6, TP7, TP9, TP10, TP11)](#9-pages-tp6-tp7-tp9-tp10-tp11)
10. [Authentification & Autorisation (TP11)](#10-authentification--autorisation-tp11)
11. [Simulation de Prix (Innovation/Créativité)](#11-simulation-de-prix-innovationcréativité)
12. [Configuration (Program.cs)](#12-configuration-programcs)
13. [Formules & Calculs Métier](#13-formules--calculs-métier)
14. [Correspondance TP → Fonctionnalité](#14-correspondance-tp--fonctionnalité)
15. [Guide d'Utilisation](#15-guide-dutilisation)

---

## 1. Vue d'Ensemble du Projet

### Objectif
Développer une application web complète basée sur .NET 10 (Blazor + EF Core) permettant la gestion d'un portefeuille d'investissement :
- Gestion des actifs (Actions, Crypto, ETF)
- Saisie des transactions (achat / vente)
- Budget d'investissement
- Historique des opérations et des prix
- Tableau de bord analytique avec indicateurs et graphiques

### Exigences Techniques respectées
- ✅ Blazor Web Application (.NET 10)
- ✅ Architecture propre en couches (UI / Services / Data)
- ✅ Entity Framework Core (Code-First + Migrations)
- ✅ CRUD complet sur les entités principales
- ✅ Recherche et filtrage dynamique (LINQ)
- ✅ Tableau de bord avec indicateurs et graphiques (Radzen)
- ✅ Validation via Data Annotations
- ✅ Utilisation de l'asynchronisme (async / await)
- ✅ Authentification & Autorisation (Identity + Rôles)
- ✅ **Extension créative** : Simulation automatique de variation de prix (marche aléatoire) via un service d'arrière-plan

---

## 2. Architecture Générale

### Schéma en couches

```
┌─────────────────────────────────────────────────────────┐
│  UI (Blazor Components .razor)                          │
│  - Pages/       : Dashboard, Assets, Transactions, etc. │
│  - UI/          : KpiCard, AssetTable, TransactionTable │
│  - Layout/      : MainLayout, NavMenu, EmptyLayout      │
└────────────────────────┬────────────────────────────────┘
                         │ @inject IPortfolioService
                         ▼
┌─────────────────────────────────────────────────────────┐
│  Services (Logique Métier)                              │
│  - IPortfolioService  : Contrat (Interface)             │
│  - PortfolioService   : Implémentation                  │
│  - PriceSimulationHostedService : BackgroundService     │
└────────────────────────┬────────────────────────────────┘
                         │ Injection du AppDbContext
                         ▼
┌─────────────────────────────────────────────────────────┐
│  Data (EF Core)                                         │
│  - AppDbContext : IdentityDbContext<IdentityUser>       │
│  - Migrations   : InitialCreate                         │
└────────────────────────┬────────────────────────────────┘
                         │ UseSqlite()
                         ▼
┌─────────────────────────────────────────────────────────┐
│  SQLite (portfolio.db)                                  │
└─────────────────────────────────────────────────────────┘
```

### Principe d'Inversion de Contrôle (IoC) — Chapitre 4
Les pages Blazor ne créent jamais leurs dépendances avec `new`. Elles les demandent via `@inject`, et le conteneur IoC d'ASP.NET Core les fournit.

```csharp
@inject IPortfolioService PortfolioService   // Interface, pas la classe concrète
```

### Cycles de Vie des Services — Chapitre 4
- **Scoped** : `AppDbContext`, `IPortfolioService` → une instance par session utilisateur Blazor
- **Singleton** : `PriceSimulationHostedService` → une instance unique pour toute l'application


---

## 3. Stack Technique

| Couche | Technologie | Version | TP source |
|--------|-------------|---------|-----------|
| Runtime | .NET | 10.0.202 | Chapitre 1 |
| Framework UI | Blazor Web App (Interactive Server) | .NET 10 | TP3, Chapitre 3 |
| ORM | Entity Framework Core | 10.0.3 | TP5, Chapitre 5 |
| Base de données | SQLite | — | TP5 |
| Authentification | ASP.NET Core Identity | 10.0.7 | TP11 |
| Bibliothèque graphique | Radzen.Blazor | 10.4.1 | TP9 |
| Icônes | Bootstrap Icons | 1.13.1 | TP8 |
| CSS Framework | Bootstrap (interne à Blazor) | 5.x | TP3+ |

### Fichier `.csproj`
```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>disable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="10.0.3" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="10.0.3" />
    <PackageReference Include="Microsoft.AspNetCore.Identity.EntityFrameworkCore" Version="10.0.7" />
    <PackageReference Include="Radzen.Blazor" Version="10.4.1" />
  </ItemGroup>
</Project>
```

---

## 4. Structure du Projet

```
InvestPortfolio/
├── Components/
│   ├── _Imports.razor              # Usings globaux (TP3, TP8)
│   ├── App.razor                   # Document HTML racine (TP3)
│   ├── Routes.razor                # Routeur + CascadingAuthenticationState (TP11)
│   ├── Layout/
│   │   ├── MainLayout.razor        # Layout authentifié (sidebar)
│   │   ├── MainLayout.razor.css
│   │   ├── NavMenu.razor           # Menu latéral
│   │   ├── NavMenu.razor.css
│   │   └── EmptyLayout.razor       # Layout pour Login/Register
│   ├── Pages/
│   │   ├── Dashboard.razor         # Tableau de bord (TP6, TP9)
│   │   ├── Assets.razor            # Liste des actifs (TP10)
│   │   ├── AssetDetails.razor      # Détails + historique prix (TP9)
│   │   ├── EditAsset.razor         # CRUD Actif (TP7) — Admin only
│   │   ├── Transactions.razor      # Liste transactions (TP10)
│   │   ├── NewTransaction.razor    # Formulaire achat/vente (TP7)
│   │   ├── BudgetSetup.razor       # Gestion du budget
│   │   ├── Analytics.razor         # Analyse avancée (TP9)
│   │   ├── Login.razor             # Connexion (TP11)
│   │   ├── Register.razor          # Inscription (TP11)
│   │   ├── Error.razor
│   │   └── NotFound.razor
│   └── UI/                         # Composants "dumb" réutilisables (TP8)
│       ├── KpiCard.razor
│       ├── AssetTable.razor
│       ├── AssetCard.razor
│       └── TransactionTable.razor
├── Data/
│   └── AppDbContext.cs             # IdentityDbContext (TP5 + TP11)
├── Models/                         # Entités Code-First (TP5)
│   ├── Asset.cs
│   ├── Transaction.cs
│   ├── PriceHistory.cs
│   ├── Tag.cs
│   ├── Budget.cs
│   └── PortfolioStats.cs           # DTOs pour agrégations (TP9)
├── Services/
│   ├── IPortfolioService.cs        # Contrat (TP4, Chapitre 4)
│   ├── PortfolioService.cs         # Implémentation (TP6)
│   └── PriceSimulationHostedService.cs  # BackgroundService (créativité)
├── Migrations/                     # Générées par `dotnet ef` (TP5)
│   └── 20260512164605_InitialCreate.cs
├── wwwroot/
│   ├── app.css                     # Thème Binance
│   └── lib/bootstrap/              # Bootstrap intégré
├── appsettings.json                # ConnectionString SQLite (TP5)
├── Program.cs                      # Point d'entrée + DI + Identity
└── portfolio.db                    # Base SQLite (générée par migration)
```

---

## 5. Modèle de Données (TP5 - Chapitre 5)

Tout le modèle suit l'approche **Code-First** : les classes C# sont la source de vérité, et EF Core génère le schéma SQL.

### 5.1 Entité `Asset` (Actif)

**Fichier** : `Models/Asset.cs`

```csharp
public class Asset
{
    [Key]                                // Clé Primaire
    public int Id { get; set; }

    [Required(ErrorMessage = "Le nom de l'actif est requis.")]
    [StringLength(100, MinimumLength = 2)]
    public string Name { get; set; }      // Ex: "Bitcoin"

    [Required]
    [StringLength(10, MinimumLength = 1)]
    public string Symbol { get; set; }    // Ex: "BTC"

    [Required]
    public string AssetType { get; set; } = "Action"; // Action / Crypto / ETF

    public double CurrentPrice { get; set; }
    public DateTime LastUpdate { get; set; } = DateTime.Now;

    // ===== Relations =====
    // 1-to-N : un actif → plusieurs transactions
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();

    // 1-to-N : un actif → plusieurs snapshots de prix
    public ICollection<PriceHistory> PriceHistories { get; set; } = new List<PriceHistory>();

    // N-to-N : un actif ↔ plusieurs tags (table pivot AssetTag auto-générée)
    public ICollection<Tag> Tags { get; set; } = new List<Tag>();
}
```

**Techniques utilisées :**
- `[Key]` → Clé primaire explicite (TP5, Chapitre 5)
- `[Required]`, `[StringLength]` → Validation automatique (TP7, Chapitre 6)
- `ICollection<T>` → Propriété de navigation pour les relations (TP5)

### 5.2 Entité `Transaction`

**Fichier** : `Models/Transaction.cs`

```csharp
public class Transaction
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string Type { get; set; } = "Achat"; // Achat ou Vente

    [Range(0.0001, double.MaxValue, ErrorMessage = "La quantité doit être supérieure à 0.")]
    public double Quantity { get; set; }

    [Range(0.01, double.MaxValue)]
    public double UnitPrice { get; set; }

    // Propriété calculée (Expression-Bodied, Chapitre 1)
    [NotMapped]                               // N'est pas une colonne SQL
    public double TotalAmount => Quantity * UnitPrice;

    public DateTime Date { get; set; } = DateTime.Now;
    public string Notes { get; set; } = "";

    // FK 1-to-N vers Asset
    [Range(1, int.MaxValue, ErrorMessage = "Veuillez sélectionner un actif.")]
    public int AssetId { get; set; }
    public Asset Asset { get; set; }          // Propriété de navigation
}
```

**Techniques utilisées :**
- `[Range]` pour forcer des valeurs positives (TP7, Exercice 1)
- `[NotMapped]` → EF Core ignore cette propriété (Chapitre 5)
- Expression-bodied property `=>` → Propriété calculée à la volée (Chapitre 1)
- Convention EF Core : `AssetId` + `Asset` → EF détecte automatiquement la FK (Chapitre 5)

### 5.3 Entité `PriceHistory`

**Fichier** : `Models/PriceHistory.cs`

```csharp
public class PriceHistory
{
    [Key]
    public int Id { get; set; }

    [Required]
    public double Price { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.Now;

    // FK 1-to-N vers Asset
    public int AssetId { get; set; }
    public Asset Asset { get; set; }
}
```

**Rôle** : Snapshot du prix à un instant donné. Permet de reconstituer l'évolution du prix (graphique LineChart sur `/asset-details/{id}`).

Inspiré directement de `SensorValueHistory` du TP5 (Exercice 2).

### 5.4 Entité `Tag`

```csharp
public class Tag
{
    [Key] public int Id { get; set; }
    [Required] [StringLength(30)] public string Label { get; set; }

    // N-to-N : Un tag ↔ plusieurs actifs
    public ICollection<Asset> Assets { get; set; } = new List<Asset>();
}
```

**EF Core** détecte `ICollection<Asset>` dans Tag ET `ICollection<Tag>` dans Asset → il génère automatiquement la table pivot `AssetTag` (technique du TP5).

### 5.5 Entité `Budget`

```csharp
public class Budget
{
    [Key] public int Id { get; set; }
    [Range(0.01, double.MaxValue)] public double InitialAmount { get; set; }
    public double CurrentBalance { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime LastUpdate { get; set; } = DateTime.Now;
}
```

Stocke le capital initial et le solde disponible de l'utilisateur.

### 5.6 DTOs pour les agrégations (TP9)

**Fichier** : `Models/PortfolioStats.cs`

```csharp
// Classes "transporteuses" utilisées par les requêtes GroupBy
public class AssetAllocationStat
{
    public string AssetType { get; set; }
    public double TotalValue { get; set; }
    public int Count { get; set; }
}

public class AssetPerformanceStat
{
    public string AssetName { get; set; }
    public string Symbol { get; set; }
    public double GainLoss { get; set; }
    public double GainLossPercent { get; set; }
}

public class MonthlyTransactionStat
{
    public string Month { get; set; }
    public double TotalBuy { get; set; }
    public double TotalSell { get; set; }
}
```

**Analogie au TP9** : comme `LocationStat` dans le projet DashboardData, ces classes servent à transporter les résultats des agrégations LINQ `GroupBy` + `Select`.

### 5.7 Schéma relationnel généré par EF Core

```
┌──────────────┐         ┌──────────────────┐
│   Budgets    │         │    AspNetUsers   │  (Identity - TP11)
│              │         │                  │
│ Id (PK)      │         │ Id (PK, string)  │
│ InitialAmount│         │ UserName         │
│ CurrentBalance         │ Email            │
│ CreatedAt    │         │ PasswordHash     │
│ LastUpdate   │         │ ...              │
└──────────────┘         └──────────────────┘

┌─────────────┐   1    N  ┌──────────────┐   N    1  ┌─────────┐
│   Assets    │──────────►│ Transactions │◄──────────│         │
│             │           │              │           │         │
│ Id (PK)     │           │ Id (PK)      │           │         │
│ Name        │           │ Type         │           │         │
│ Symbol      │           │ Quantity     │           │         │
│ AssetType   │           │ UnitPrice    │           │         │
│ CurrentPrice│           │ Date         │           │         │
│ LastUpdate  │           │ Notes        │           │         │
└──────┬──────┘           │ AssetId (FK) │           │         │
       │                  └──────────────┘           │         │
       │ 1                                           │         │
       │                                             │         │
       │ N                                           │         │
┌──────▼──────────┐                                  │         │
│ PriceHistories  │                                  │         │
│                 │                                  │         │
│ Id (PK)         │                                  │         │
│ Price           │                                  │         │
│ Timestamp       │                                  │         │
│ AssetId (FK)    │                                  │         │
└─────────────────┘                                  │         │
                                                     │         │
┌──────────────┐   N    N   ┌─────────────┐          │         │
│    Assets    │◄──────────►│    Tags     │          │         │
│              │            │             │          │         │
│              │            │ Id (PK)     │          │         │
│              │            │ Label       │          │         │
└──────┬───────┘            └──────┬──────┘          │         │
       │                           │                 │         │
       │   ┌──────────────┐        │                 │         │
       └──►│   AssetTag   │◄───────┘                 │         │
           │              │                          │         │
           │ AssetsId (FK)│  (Table pivot            │         │
           │ TagsId   (FK)│   auto-générée)          │         │
           └──────────────┘                          │         │
```

### 5.8 Migrations (Chapitre 5)

```bash
# Génère le code C# qui décrit le schéma
dotnet ef migrations add InitialCreate

# Applique les migrations → crée le fichier portfolio.db
dotnet ef database update
```

Le fichier `Migrations/20260512164605_InitialCreate.cs` décrit la création de toutes les tables. EF Core a automatiquement créé :
- Les 5 tables métier : Assets, Transactions, PriceHistories, Tags, Budgets
- La table pivot : AssetTag
- Les 7 tables Identity : AspNetUsers, AspNetRoles, AspNetUserRoles, etc. (TP11)


---

## 3. Stack Technique

| Couche | Technologie | Version | Rôle |
|--------|-------------|---------|------|
| Runtime | .NET | 10.0.202 | Environnement d'exécution |
| Framework | ASP.NET Core | 10.0 | Serveur web |
| UI | Blazor Web App (Interactive Server) | 10.0 | Framework SPA |
| ORM | Entity Framework Core | 10.0.3 | Mapping objet-relationnel |
| Base de données | SQLite | - | Stockage (fichier `portfolio.db`) |
| Auth | ASP.NET Core Identity | 10.0.7 | Comptes + rôles + hashage |
| Graphiques | Radzen.Blazor | 10.4.1 | Charts, Gauges, Donut |
| CSS Framework | Bootstrap | 5 | Grille et composants |
| Icônes | Bootstrap Icons | 1.13.1 | Iconographie |

### Fichier `.csproj`
```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>disable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.Identity.EntityFrameworkCore" Version="10.0.7" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="10.0.3" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="10.0.3" />
    <PackageReference Include="Radzen.Blazor" Version="10.4.1" />
  </ItemGroup>
</Project>
```

---

## 4. Structure du Projet

```
InvestPortfolio/
│
├── Components/
│   ├── _Imports.razor              ← usings globaux Razor
│   ├── App.razor                   ← racine HTML (<html>, <body>)
│   ├── Routes.razor                ← routeur + CascadingAuthenticationState
│   │
│   ├── Layout/
│   │   ├── MainLayout.razor        ← layout principal (sidebar + topbar)
│   │   ├── MainLayout.razor.css    ← CSS isolé
│   │   ├── NavMenu.razor           ← navigation latérale
│   │   ├── NavMenu.razor.css
│   │   └── EmptyLayout.razor       ← layout vide pour Login/Register
│   │
│   ├── Pages/
│   │   ├── Login.razor             ← TP11 : formulaire de connexion
│   │   ├── Register.razor          ← TP11 : inscription
│   │   ├── Dashboard.razor         ← TP6,8,9 : tableau de bord
│   │   ├── Assets.razor            ← TP10 : liste + filtre des actifs
│   │   ├── AssetDetails.razor      ← historique de prix (chart)
│   │   ├── EditAsset.razor         ← TP7 : form CRUD (Admin only)
│   │   ├── Transactions.razor      ← TP10 : liste des transactions
│   │   ├── NewTransaction.razor    ← TP7 : form d'achat/vente
│   │   ├── BudgetSetup.razor       ← gestion du budget
│   │   ├── Analytics.razor         ← TP9 : analyses avancées
│   │   ├── Error.razor
│   │   └── NotFound.razor
│   │
│   └── UI/                          ← TP8 : composants réutilisables
│       ├── KpiCard.razor            ← carte KPI générique
│       ├── AssetTable.razor         ← tableau des actifs
│       ├── AssetCard.razor          ← carte d'actif
│       └── TransactionTable.razor   ← tableau des transactions
│
├── Data/
│   └── AppDbContext.cs             ← TP5 : IdentityDbContext + DbSets
│
├── Models/                          ← TP5 : entités EF Core
│   ├── Asset.cs
│   ├── Transaction.cs
│   ├── PriceHistory.cs
│   ├── Tag.cs
│   ├── Budget.cs
│   └── PortfolioStats.cs           ← DTOs pour les stats (LocationStat-like)
│
├── Services/
│   ├── IPortfolioService.cs        ← TP4 : contrat d'interface
│   ├── PortfolioService.cs         ← TP4,6,10 : implémentation métier
│   └── PriceSimulationHostedService.cs ← créativité : BackgroundService
│
├── Migrations/                      ← TP5 : Code-First migrations
│   └── InitialCreate.cs
│
├── wwwroot/
│   └── app.css                     ← thème Binance (dark + gradient gold)
│
├── Properties/
│   └── launchSettings.json
│
├── appsettings.json                ← connection string SQLite
├── InvestPortfolio.csproj
├── portfolio.db                     ← base SQLite générée
└── Program.cs                       ← point d'entrée + DI + Identity + endpoints
```

---

## 5. Modèle de Données (TP5 - Chapitre 5)

### Schéma relationnel

```
┌─────────────┐  1    N  ┌─────────────────┐
│   Asset     │◄────────►│  Transaction    │
│             │          │                 │
│ Id (PK)     │          │ Id (PK)         │
│ Name        │          │ Type            │
│ Symbol      │          │ Quantity        │
│ AssetType   │          │ UnitPrice       │
│ CurrentPrice│          │ Date            │
│ LastUpdate  │          │ Notes           │
└──────┬──────┘          │ AssetId (FK)    │
       │                 └─────────────────┘
       │ 1
       │
       │ N       1 ┌──────────────┐
       └──────────►│ PriceHistory │
                   │              │
                   │ Id (PK)      │
                   │ Price        │
                   │ Timestamp    │
                   │ AssetId (FK) │
                   └──────────────┘
                   
┌─────────┐  N  M  ┌──────────┐       ┌──────────────┐
│  Asset  │◄─────►│   Tag    │       │    Budget    │
│         │       │          │       │              │
│ Id      │       │ Id       │       │ Id           │
│ ...     │       │ Label    │       │ InitialAmount│
└─────────┘       └──────────┘       │ CurrentBalance│
     │                               │ CreatedAt    │
  table pivot                        │ LastUpdate   │
  AssetTag (auto)                    └──────────────┘
```

### 5.1 `Asset.cs` — Entité principale

```csharp
public class Asset
{
    [Key]
    public int Id { get; set; }

    [Required(ErrorMessage = "Le nom de l'actif est requis.")]
    [StringLength(100, MinimumLength = 2)]
    public string Name { get; set; }

    [Required(ErrorMessage = "Le symbole est requis.")]
    [StringLength(10, MinimumLength = 1)]
    public string Symbol { get; set; }

    [Required]
    public string AssetType { get; set; } = "Action"; // Action, Crypto, ETF

    public double CurrentPrice { get; set; }
    public DateTime LastUpdate { get; set; } = DateTime.Now;

    // ===== Relations EF Core =====
    // 1-to-N : 1 actif peut avoir plusieurs transactions
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    // 1-to-N : 1 actif peut avoir plusieurs historiques de prix
    public ICollection<PriceHistory> PriceHistories { get; set; } = new List<PriceHistory>();
    // N-to-N : un actif peut avoir plusieurs tags
    public ICollection<Tag> Tags { get; set; } = new List<Tag>();
}
```

**Data Annotations utilisées (Chapitre 6)** :
- `[Key]` : Clé primaire
- `[Required]` : NOT NULL
- `[StringLength]` : longueur min/max
- `[Range]` : bornes numériques

**Conventions EF Core respectées** :
- `LocationId` + objet `Location` → EF détecte automatiquement la FK (1-to-N)
- `ICollection<Tag>` dans Asset + `ICollection<Asset>` dans Tag → table pivot `AssetTag` générée automatiquement

### 5.2 `Transaction.cs` — Opération d'achat/vente

```csharp
public class Transaction
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string Type { get; set; } = "Achat"; // Achat ou Vente

    [Range(0.0001, double.MaxValue)]
    public double Quantity { get; set; }

    [Range(0.01, double.MaxValue)]
    public double UnitPrice { get; set; }

    // Propriété calculée (Chapitre 1 : Expression-Bodied Property)
    [NotMapped]  // pas de colonne en BDD
    public double TotalAmount => Quantity * UnitPrice;

    public DateTime Date { get; set; } = DateTime.Now;
    public string Notes { get; set; } = "";

    // FK vers Asset (1-to-N)
    [Range(1, int.MaxValue, ErrorMessage = "Veuillez sélectionner un actif.")]
    public int AssetId { get; set; }
    public Asset Asset { get; set; }
}
```

**Points remarquables** :
- `[NotMapped]` empêche EF de créer une colonne pour `TotalAmount` (propriété calculée)
- `Type = "Achat"` : valeur par défaut via initialisation

### 5.3 `PriceHistory.cs` — Historique des prix (TP5 Exercice 2)

```csharp
public class PriceHistory
{
    [Key]
    public int Id { get; set; }

    [Required]
    public double Price { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.Now;

    // FK vers Asset (1-to-N)
    public int AssetId { get; set; }
    public Asset Asset { get; set; }
}
```

**Rôle** : chaque changement de prix (manuel Admin ou simulation automatique) crée une ligne ici → alimente le graphique d'évolution.

### 5.4 `Tag.cs` — Catégorisation (N-to-N)

```csharp
public class Tag
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(30)]
    public string Label { get; set; }

    // N-to-N : un tag peut être associé à plusieurs actifs
    public ICollection<Asset> Assets { get; set; } = new List<Asset>();
}
```

**EF Core détecte la N-to-N automatiquement** grâce aux `ICollection` croisées et génère la table pivot `AssetTag`.

### 5.5 `Budget.cs` — Capital d'investissement

```csharp
public class Budget
{
    [Key]
    public int Id { get; set; }

    [Range(0.01, double.MaxValue)]
    public double InitialAmount { get; set; }

    public double CurrentBalance { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime LastUpdate { get; set; } = DateTime.Now;
}
```

### 5.6 `PortfolioStats.cs` — DTOs pour les graphiques (TP9)

Similaire à `LocationStat` du TP9, classes pour transporter les données agrégées :

```csharp
public class AssetAllocationStat
{
    public string AssetType { get; set; }
    public double TotalValue { get; set; }
    public int Count { get; set; }
}

public class AssetPerformanceStat
{
    public string AssetName { get; set; }
    public string Symbol { get; set; }
    public double GainLoss { get; set; }
    public double GainLossPercent { get; set; }
}

public class MonthlyTransactionStat
{
    public string Month { get; set; }
    public double TotalBuy { get; set; }
    public double TotalSell { get; set; }
}
```

---

## 6. Couche Données : DbContext (TP5)

### `Data/AppDbContext.cs`

```csharp
// On hérite de IdentityDbContext (comme TP11)
public class AppDbContext : IdentityDbContext<IdentityUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // Chaque DbSet = une Table SQL
    public DbSet<Asset> Assets { get; set; }
    public DbSet<Transaction> Transactions { get; set; }
    public DbSet<PriceHistory> PriceHistories { get; set; }
    public DbSet<Tag> Tags { get; set; }
    public DbSet<Budget> Budgets { get; set; }
}
```

**Pourquoi `IdentityDbContext<IdentityUser>`** (TP11) ?
- Ajoute automatiquement les tables `AspNetUsers`, `AspNetRoles`, `AspNetUserRoles`, etc.
- Permet de stocker utilisateurs + rôles dans la même base

### Connection String (`appsettings.json`)
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=portfolio.db"
  }
}
```

### Migrations
```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

**Tables SQL générées** :
- **Métier** : `Assets`, `Transactions`, `PriceHistories`, `Tags`, `Budgets`, `AssetTag` (pivot auto)
- **Identity** : `AspNetUsers`, `AspNetRoles`, `AspNetUserRoles`, `AspNetUserClaims`, `AspNetUserLogins`, `AspNetUserTokens`, `AspNetRoleClaims`

---

## 7. Couche Services (TP4, TP6, TP10)

### 7.1 Interface `IPortfolioService`

Le contrat que l'UI connaît. Principe du TP4 : la page ne connaît que l'interface, pas la classe concrète.

```csharp
public interface IPortfolioService
{
    // Assets CRUD
    Task<List<Asset>> GetAssetsAsync();
    Task<Asset> GetAssetByIdAsync(int id);
    Task AddAssetAsync(Asset asset);
    Task UpdateAssetAsync(Asset asset);
    Task DeleteAssetAsync(int id);

    // Transactions CRUD
    Task<List<Transaction>> GetTransactionsAsync();
    Task<List<Transaction>> GetTransactionsByAssetAsync(int assetId);
    Task AddTransactionAsync(Transaction transaction);
    Task DeleteTransactionAsync(int id);

    // Budget
    Task<Budget> GetBudgetAsync();
    Task UpdateBudgetAsync(Budget budget);

    // KPIs (agrégations)
    Task<int> GetTotalAssetsCountAsync();
    Task<double> GetPortfolioValueAsync();
    Task<double> GetTotalGainLossAsync();
    Task<double> GetBudgetBalanceAsync();

    // Statistiques pour graphiques (GroupBy - TP9)
    Task<List<AssetAllocationStat>> GetAllocationByTypeAsync();
    Task<List<AssetPerformanceStat>> GetPerformanceByAssetAsync();
    Task<List<MonthlyTransactionStat>> GetMonthlyTransactionsAsync();

    // Recherche (IQueryable - TP10)
    Task<List<Asset>> SearchAssetsAsync(string searchText, string assetType);
    Task<List<Transaction>> SearchTransactionsAsync(string assetName, string transactionType);

    // Historique des prix
    Task<List<PriceHistory>> GetPriceHistoryAsync(int assetId);

    // Simulation (extension créative)
    Task SimulatePriceChangeAsync(int assetId);
    Task SimulateAllPricesAsync();
}
```

### 7.2 Implémentation `PortfolioService`

#### Injection du DbContext (TP6)
```csharp
public class PortfolioService : IPortfolioService
{
    private readonly AppDbContext _context;

    // DI : le conteneur IoC fournit automatiquement le AppDbContext
    public PortfolioService(AppDbContext context)
    {
        _context = context;
    }
    // ...
}
```

#### CRUD Create (TP7)
```csharp
public async Task AddAssetAsync(Asset asset)
{
    asset.LastUpdate = DateTime.Now;

    // Historisation du prix initial (TP5 Exercice 2)
    asset.PriceHistories.Add(new PriceHistory
    {
        Price = asset.CurrentPrice,
        Timestamp = DateTime.Now
    });

    _context.Assets.Add(asset);
    await _context.SaveChangesAsync();
}
```

#### CRUD Read avec Include (TP6)
```csharp
public async Task<List<Asset>> GetAssetsAsync()
{
    return await _context.Assets
        .Include(a => a.Tags)      // JOIN sur la table pivot AssetTag
        .ToListAsync();            // async + EF Core = pas de freeze UI
}
```

#### Agrégations SQL (TP6 Exercice 1)
```csharp
public async Task<int> GetTotalAssetsCountAsync()
{
    // EF traduit en : SELECT COUNT(*) FROM Assets
    return await _context.Assets.CountAsync();
}

public async Task<double> GetPortfolioValueAsync()
{
    var assets = await _context.Assets
        .Include(a => a.Transactions)
        .ToListAsync();

    double totalValue = 0;
    foreach (var asset in assets)
    {
        // LINQ in-memory après rapatriement : calcul de la quantité détenue
        double qty = asset.Transactions.Where(t => t.Type == "Achat").Sum(t => t.Quantity)
                   - asset.Transactions.Where(t => t.Type == "Vente").Sum(t => t.Quantity);
        if (qty > 0) totalValue += qty * asset.CurrentPrice;
    }
    return totalValue;
}
```

#### GroupBy pour graphiques (TP9)
```csharp
public async Task<List<AssetAllocationStat>> GetAllocationByTypeAsync()
{
    var assets = await _context.Assets
        .Include(a => a.Transactions)
        .ToListAsync();

    // LINQ GroupBy : agrège les actifs par type
    return assets
        .GroupBy(a => a.AssetType)
        .Select(g => new AssetAllocationStat
        {
            AssetType = g.Key,
            Count = g.Count(),
            TotalValue = g.Sum(a => {
                double qty = a.Transactions.Where(t => t.Type == "Achat").Sum(t => t.Quantity)
                           - a.Transactions.Where(t => t.Type == "Vente").Sum(t => t.Quantity);
                return qty > 0 ? qty * a.CurrentPrice : 0;
            })
        })
        .ToList();
}
```

#### Recherche dynamique avec IQueryable (TP10)
```csharp
public async Task<List<Asset>> SearchAssetsAsync(string searchText, string assetType)
{
    // IQueryable : construit la requête SQL étape par étape
    IQueryable<Asset> query = _context.Assets.Include(a => a.Tags).AsQueryable();

    if (!string.IsNullOrEmpty(assetType))
    {
        // Ajoute un WHERE au SQL dynamiquement
        query = query.Where(a => a.AssetType == assetType);
    }

    if (!string.IsNullOrEmpty(searchText))
    {
        // Traduit en : WHERE Name LIKE '%text%' OR Symbol LIKE '%text%'
        query = query.Where(a => a.Name.Contains(searchText) || a.Symbol.Contains(searchText));
    }

    // Exécution SQL unique ici avec tous les filtres combinés
    return await query.ToListAsync();
}
```


---

## 6. Couche Données : DbContext (TP5)

**Fichier** : `Data/AppDbContext.cs`

```csharp
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using InvestPortfolio.Models;

namespace InvestPortfolio.Data
{
    // Héritage de IdentityDbContext (TP11) au lieu de DbContext simple
    public class AppDbContext : IdentityDbContext<IdentityUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // Chaque DbSet = une table SQL
        public DbSet<Asset> Assets { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<PriceHistory> PriceHistories { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<Budget> Budgets { get; set; }
    }
}
```

**Analogie du "Panier d'achat"** (Chapitre 5) :
Le `DbContext` fonctionne comme un panier e-commerce :
1. On modifie des objets C# en mémoire (tracking)
2. Rien n'est envoyé à la BDD avant l'appel à `SaveChangesAsync()`
3. C'est alors que toutes les requêtes SQL sont exécutées d'un coup

**Configuration** (dans `appsettings.json`) :
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=portfolio.db"
  }
}
```

---

## 7. Couche Services (TP4, TP6, TP10)

### 7.1 Interface `IPortfolioService` — Le Contrat

**Fichier** : `Services/IPortfolioService.cs`

Principe du **Chapitre 4** : l'interface définit **ce que** le service doit faire, pas **comment**. Les composants dépendent de l'abstraction, pas de l'implémentation.

```csharp
public interface IPortfolioService
{
    // ===== Assets CRUD =====
    Task<List<Asset>> GetAssetsAsync();
    Task<Asset> GetAssetByIdAsync(int id);
    Task AddAssetAsync(Asset asset);
    Task UpdateAssetAsync(Asset asset);
    Task DeleteAssetAsync(int id);
    Task ReloadAssetAsync(Asset asset);

    // ===== Transactions CRUD =====
    Task<List<Transaction>> GetTransactionsAsync();
    Task<List<Transaction>> GetTransactionsByAssetAsync(int assetId);
    Task<Transaction> GetTransactionByIdAsync(int id);
    Task AddTransactionAsync(Transaction transaction);
    Task DeleteTransactionAsync(int id);

    // ===== Budget =====
    Task<Budget> GetBudgetAsync();
    Task UpdateBudgetAsync(Budget budget);

    // ===== KPIs (Agrégations côté serveur) =====
    Task<int> GetTotalAssetsCountAsync();
    Task<double> GetPortfolioValueAsync();
    Task<double> GetTotalGainLossAsync();
    Task<double> GetBudgetBalanceAsync();

    // ===== Statistiques pour graphiques =====
    Task<List<AssetAllocationStat>> GetAllocationByTypeAsync();
    Task<List<AssetPerformanceStat>> GetPerformanceByAssetAsync();
    Task<List<MonthlyTransactionStat>> GetMonthlyTransactionsAsync();

    // ===== Recherche & Filtrage (IQueryable - TP10) =====
    Task<List<Asset>> SearchAssetsAsync(string searchText, string assetType);
    Task<List<Transaction>> SearchTransactionsAsync(string assetName, string transactionType);

    // ===== Historique & Simulation =====
    Task<List<PriceHistory>> GetPriceHistoryAsync(int assetId);
    Task SimulatePriceChangeAsync(int assetId);
    Task SimulateAllPricesAsync();
}
```

Toutes les méthodes sont **async** (TP6, Chapitre 4) pour ne pas bloquer le thread UI pendant les requêtes BDD.

### 7.2 Implémentation `PortfolioService`

**Fichier** : `Services/PortfolioService.cs`

#### Constructeur avec Injection de Dépendance (TP6)

```csharp
public class PortfolioService : IPortfolioService
{
    private readonly AppDbContext _context;

    // Quand Blazor crée PortfolioService, il lui passe automatiquement le DbContext
    public PortfolioService(AppDbContext context)
    {
        _context = context;
    }
    // ...
}
```

#### 7.2.1 Lecture (Read) — Include pour les JOIN (TP6)

```csharp
public async Task<List<Asset>> GetAssetsAsync()
{
    // .Include() force EF Core à faire un JOIN SQL vers la table Tags
    return await _context.Assets
        .Include(a => a.Tags)
        .ToListAsync();
}

public async Task<Asset> GetAssetByIdAsync(int id)
{
    // FindAsync = recherche ultra-optimisée par clé primaire
    return await _context.Assets.FindAsync(id);
}
```

#### 7.2.2 Création (Create) avec historisation (TP7)

```csharp
public async Task AddAssetAsync(Asset asset)
{
    asset.LastUpdate = DateTime.Now;

    // Historisation du prix initial (technique TP5, Exercice 2)
    asset.PriceHistories.Add(new PriceHistory
    {
        Price = asset.CurrentPrice,
        Timestamp = DateTime.Now
    });

    _context.Assets.Add(asset);
    await _context.SaveChangesAsync();
}
```

#### 7.2.3 Mise à jour (Update) avec historique (TP7)

```csharp
public async Task UpdateAssetAsync(Asset asset)
{
    asset.LastUpdate = DateTime.Now;

    // À chaque mise à jour du prix, on ajoute un snapshot
    asset.PriceHistories.Add(new PriceHistory
    {
        Price = asset.CurrentPrice,
        Timestamp = DateTime.Now
    });

    _context.Assets.Update(asset);
    await _context.SaveChangesAsync();
}
```

C'est exactement le même pattern que `UpdateSensorAsync` dans le TP7 de référence.

#### 7.2.4 Suppression (Delete)

```csharp
public async Task DeleteAssetAsync(int id)
{
    var asset = await _context.Assets.FindAsync(id);
    if (asset != null)
    {
        _context.Assets.Remove(asset);
        await _context.SaveChangesAsync();
    }
}
```

#### 7.2.5 Transactions avec logique métier

```csharp
public async Task AddTransactionAsync(Transaction transaction)
{
    transaction.Date = DateTime.Now;
    _context.Transactions.Add(transaction);

    // Impact sur le budget
    var budget = await _context.Budgets.FirstOrDefaultAsync();
    if (budget != null)
    {
        if (transaction.Type == "Achat")
            budget.CurrentBalance -= transaction.TotalAmount;
        else
            budget.CurrentBalance += transaction.TotalAmount;

        budget.LastUpdate = DateTime.Now;
    }

    await _context.SaveChangesAsync();
}
```

L'opération est **atomique** : ajout de transaction + mise à jour budget → un seul `SaveChangesAsync()`.

#### 7.2.6 Agrégations SQL côté serveur (TP6, Exercice 1)

```csharp
public async Task<int> GetTotalAssetsCountAsync()
{
    return await _context.Assets.CountAsync();
}
// → SQL : SELECT COUNT(*) FROM Assets
```

Pas de chargement en mémoire, c'est SQL qui calcule.

#### 7.2.7 Agrégation par groupe avec GroupBy (TP9)

```csharp
public async Task<List<AssetAllocationStat>> GetAllocationByTypeAsync()
{
    var assets = await _context.Assets
        .Include(a => a.Transactions)
        .ToListAsync();

    // GroupBy sur AssetType puis Select pour construire le DTO
    var stats = assets
        .GroupBy(a => a.AssetType)
        .Select(g => new AssetAllocationStat
        {
            AssetType = g.Key,
            Count = g.Count(),
            TotalValue = g.Sum(a =>
            {
                double qty = a.Transactions.Where(t => t.Type == "Achat").Sum(t => t.Quantity)
                           - a.Transactions.Where(t => t.Type == "Vente").Sum(t => t.Quantity);
                return qty > 0 ? qty * a.CurrentPrice : 0;
            })
        })
        .ToList();

    return stats;
}
```

**Technique TP9** : `GroupBy(...) → Select(...)` pour préparer des données agrégées destinées à un graphique.

#### 7.2.8 Recherche dynamique IQueryable (TP10)

```csharp
public async Task<List<Asset>> SearchAssetsAsync(string searchText, string assetType)
{
    // AsQueryable() prépare la requête SQL sans l'exécuter
    IQueryable<Asset> query = _context.Assets.Include(a => a.Tags).AsQueryable();

    // Ajout conditionnel de WHERE clauses
    if (!string.IsNullOrEmpty(assetType))
    {
        query = query.Where(a => a.AssetType == assetType);
    }

    if (!string.IsNullOrEmpty(searchText))
    {
        query = query.Where(a => a.Name.Contains(searchText) || a.Symbol.Contains(searchText));
    }

    // L'exécution SQL se fait ici seulement
    return await query.ToListAsync();
}
```

**C'est exactement le pattern du TP10, Activité 3** : construction progressive d'une requête SQL via `IQueryable`, sans ramener toute la table en mémoire.

### 7.3 Enregistrement dans le conteneur IoC (TP4, Chapitre 4)

**Fichier** : `Program.cs`

```csharp
// Cycle de vie Scoped : une instance par session utilisateur Blazor
builder.Services.AddScoped<IPortfolioService, PortfolioService>();
```

Tout composant Blazor peut maintenant faire :
```csharp
@inject IPortfolioService PortfolioService
```

---

## 8. Composants UI Réutilisables (TP8)

### 8.1 `KpiCard.razor` — Composant "dumb" (TP8, Activité 1)

**Fichier** : `Components/UI/KpiCard.razor`

Un composant sans logique qui reçoit ses données par `[Parameter]` depuis le parent.

```razor
<div class="card kpi-card h-100">
    <div class="card-body">
        <div class="d-flex justify-content-between align-items-center">
            <div>
                <p class="text-muted small mb-1 fw-semibold text-uppercase">@Title</p>
                <h4 class="mb-0 fw-bold">@Value</h4>
            </div>
            <div class="kpi-icon @IconBgClass">
                <i class="bi @IconClass"></i>
            </div>
        </div>
    </div>
</div>

@code {
    [Parameter] public string Title { get; set; } = "KPI";
    [Parameter] public string Value { get; set; } = "0";
    [Parameter] public string IconClass { get; set; } = "bi-graph-up";
    [Parameter] public string IconBgClass { get; set; } = "bg-primary-subtle";
}
```

**Utilisation dans Dashboard** :
```razor
<KpiCard Title="Valeur Portefeuille"
         Value="@($"{PortfolioValue:N2} $")"
         IconClass="bi-wallet2"
         IconBgClass="bg-primary-subtle" />
```

### 8.2 `AssetTable.razor` — Passage d'objets (TP8, Activité 2)

**Fichier** : `Components/UI/AssetTable.razor`

Reçoit une liste d'objets complexes via `[Parameter]` :

```razor
@code {
    [Parameter] public new List<Asset> Assets { get; set; } = new();
    [Parameter] public EventCallback<int> OnDeleteClicked { get; set; }

    private async Task TriggerDelete(int id)
    {
        await OnDeleteClicked.InvokeAsync(id);
    }
}
```

**Communication Enfant → Parent via `EventCallback`** (TP8, Activité 3) :

Le bouton Supprimer dans `AssetTable` ne supprime pas lui-même (principe de séparation). Il notifie le parent :

```razor
<button class="btn btn-sm btn-outline-danger" @onclick="() => TriggerDelete(asset.Id)">
    <i class="bi bi-trash"></i>
</button>
```

Le parent `Assets.razor` s'abonne :
```razor
<AssetTable Assets="FilteredAssets" OnDeleteClicked="DeleteAsset" />
```

Et traite l'événement :
```csharp
private async Task DeleteAsset(int id)
{
    await PortfolioService.DeleteAssetAsync(id);
    await ExecuteSearch();
}
```

**Protection par rôle** (TP11) : les boutons Edit/Delete sont masqués pour les non-admins :

```razor
<AuthorizeView Roles="Admin">
    <Authorized>
        <a href="/edit-asset/@asset.Id" class="btn btn-sm btn-outline-primary">
            <i class="bi bi-pencil"></i>
        </a>
        <button class="btn btn-sm btn-outline-danger" @onclick="() => TriggerDelete(asset.Id)">
            <i class="bi bi-trash"></i>
        </button>
    </Authorized>
</AuthorizeView>
```

### 8.3 `AssetCard.razor` — Vue Cartes (TP8, Exercice)

Alternative visuelle au tableau. Même technique `[Parameter]` + `EventCallback` :

```csharp
[Parameter] public Asset Asset { get; set; }
[Parameter] public EventCallback<int> OnDeleteClicked { get; set; }
```

### 8.4 `TransactionTable.razor` — Tableau des transactions

Affiche les transactions avec badges colorés (vert = Achat, rouge = Vente). Même pattern que `AssetTable`.

### 8.5 Basculement Tableau / Cartes (TP8, Exercice 2)

Dans `Assets.razor` :
```csharp
private bool ShowAsCards = false;
```

```razor
<button class="btn btn-outline-secondary" @onclick="() => ShowAsCards = !ShowAsCards">
    <i class="bi @(ShowAsCards ? "bi-table" : "bi-grid-3x3-gap")"></i>
    @(ShowAsCards ? " Vue Tableau" : " Vue Cartes")
</button>

@if (ShowAsCards)
{
    <div class="row g-3">
        @foreach (var asset in FilteredAssets)
        {
            <AssetCard Asset="asset" OnDeleteClicked="DeleteAsset" />
        }
    </div>
}
else
{
    <AssetTable Assets="FilteredAssets" OnDeleteClicked="DeleteAsset" />
}
```


---

## 9. Pages (TP6, TP7, TP9, TP10, TP11)

### 9.1 `Dashboard.razor` — Page Tableau de Bord

**Route** : `/` et `/dashboard`
**Protection** : `@attribute [Authorize]` (TP11)
**Render Mode** : `InteractiveServer` (Chapitre 3)

#### Structure
1. **4 KPI Cards** (TP8, TP6 Exercice 2) :
   - Valeur Portefeuille
   - Gain / Perte (couleur dynamique selon signe)
   - Budget Disponible
   - Nombre d'Actifs

2. **Graphiques Radzen** (TP9) :
   - **DonutChart** : Répartition par type (Action/Crypto/ETF)
   - **ColumnChart** : Performance par actif (Gain/Perte)
   - **RadialGauge** : Rendement global en %
   - **Dual ColumnChart** : Flux mensuel (Achats vs Ventes)

3. **TransactionTable** : Dernières 5 transactions

#### Empty State
Si `TotalAssets == 0`, affiche un écran d'onboarding :
```razor
@if (TotalAssets == 0)
{
    <div class="card">
        <div class="card-body text-center py-5">
            <i class="bi bi-rocket-takeoff"></i>
            <h5>Commencez à investir</h5>
            <a href="/budget">Configurer mon Budget</a>
            <a href="/edit-asset">Ajouter un Actif</a>
        </div>
    </div>
}
```

#### Rafraîchissement automatique (temps réel)
```csharp
private System.Threading.Timer _refreshTimer;

protected override async Task OnInitializedAsync()
{
    await LoadDashboard();

    // Toutes les 30 secondes, on recharge pour voir les prix changer
    _refreshTimer = new System.Threading.Timer(async _ =>
    {
        await InvokeAsync(async () =>
        {
            await LoadDashboard();
            StateHasChanged();
        });
    }, null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
}

public void Dispose()
{
    _refreshTimer?.Dispose();
}
```

La classe implémente `IDisposable` pour libérer le timer quand la page est quittée.

### 9.2 `Assets.razor` — Catalogue des actifs

**Route** : `/assets`
**Fonctionnalités** :
- Barre de recherche temps réel (TP10, Activité 1)
- Filtre par type (TP10, Activité 2)
- Basculement Tableau/Cartes (TP8, Exercice 2)
- Bouton "Simuler le marché" (visible Admin uniquement — TP11)

#### Recherche réactive (TP10)
```razor
<input type="text"
       @bind="SearchText"
       @bind:event="oninput"     <!-- Déclenche à chaque frappe -->
       class="form-control"
       placeholder="Rechercher..." />
```

Dans le code-behind :
```csharp
private string SearchText
{
    get => _searchText;
    set
    {
        _searchText = value;
        _ = ExecuteSearch();      // Relance la requête SQL
    }
}

private async Task ExecuteSearch()
{
    FilteredAssets = await PortfolioService.SearchAssetsAsync(_searchText, SelectedType);
}
```

La méthode `SearchAssetsAsync` utilise `IQueryable` côté serveur (TP10).

### 9.3 `AssetDetails.razor` — Détails et historique

**Route** : `/asset-details/{Id:int}`

Affiche :
- 4 KPIs : Prix Actuel, Quantité Possédée, Valeur Position, Prix d'Achat Moyen
- **Graphique LineChart** de l'évolution du prix (données de `PriceHistory`)
- Tableau des transactions sur cet actif

```razor
<RadzenChart Style="height: 320px;">
    <RadzenLineSeries Data="@PriceHistory"
                      CategoryProperty="Label"
                      ValueProperty="Price"
                      Smooth="true"
                      Stroke="#F0B90B"
                      StrokeWidth="3">
        <RadzenMarkers MarkerType="MarkerType.Circle" />
    </RadzenLineSeries>
    <RadzenValueAxis Formatter="@FormatCurrency" />
</RadzenChart>
```

Calcul du prix d'achat moyen pondéré :
```csharp
double totalBought = AssetTransactions.Where(t => t.Type == "Achat").Sum(t => t.Quantity);
double totalCost = AssetTransactions.Where(t => t.Type == "Achat").Sum(t => t.Quantity * t.UnitPrice);
AveragePrice = totalBought > 0 ? totalCost / totalBought : 0;
```

### 9.4 `EditAsset.razor` — Formulaire CRUD (TP7, Chapitre 6)

**Routes** : `/edit-asset` (création) et `/edit-asset/{Id:int}` (modification)
**Protection** : `@attribute [Authorize(Roles = "Admin")]` — seul l'admin peut gérer les actifs (TP11, Activité 5)

#### Unification Création/Modification (TP7, Activité 2)
```csharp
[Parameter]
public int? Id { get; set; }

protected override async Task OnInitializedAsync()
{
    if (Id.HasValue)
    {
        currentAsset = await PortfolioService.GetAssetByIdAsync(Id.Value);
    }

    if (currentAsset == null)
    {
        currentAsset = new Asset();   // Mode création
    }
}

private async Task HandleValidSubmit()
{
    if (Id.HasValue)
        await PortfolioService.UpdateAssetAsync(currentAsset);
    else
        await PortfolioService.AddAssetAsync(currentAsset);

    NavigationManager.NavigateTo("/assets");
}
```

#### EditForm + DataAnnotationsValidator (TP7, Chapitre 6)
```razor
<EditForm Model="currentAsset" OnValidSubmit="HandleValidSubmit">
    <DataAnnotationsValidator />
    <ValidationSummary class="text-danger" />

    <InputText @bind-Value="currentAsset.Name" class="form-control" />
    <ValidationMessage For="@(() => currentAsset.Name)" class="text-danger" />

    <InputSelect @bind-Value="currentAsset.AssetType" class="form-select">
        <option value="">-- Sélectionnez --</option>
        <option value="Action">Action</option>
        <option value="Crypto">Crypto-monnaie</option>
        <option value="ETF">ETF</option>
    </InputSelect>

    <InputNumber @bind-Value="currentAsset.CurrentPrice" class="form-control" />
</EditForm>
```

Les règles `[Required]`, `[StringLength]`, `[Range]` définies dans `Asset.cs` sont automatiquement appliquées. L'`EditForm` bloque la soumission si les règles ne sont pas respectées, et `ValidationMessage` affiche l'erreur en rouge **sans JavaScript**.

### 9.5 `NewTransaction.razor` — Passer une transaction

**Route** : `/new-transaction`
**Protection** : `@attribute [Authorize]`

#### Flux UX
1. L'utilisateur choisit un actif → `@bind-Value:after="OnAssetChanged"` déclenche :
```csharp
private void OnAssetChanged()
{
    SelectedAsset = Assets.FirstOrDefault(a => a.Id == currentTransaction.AssetId);
    if (SelectedAsset != null)
    {
        currentTransaction.UnitPrice = SelectedAsset.CurrentPrice;
    }
}
```
2. Le prix courant s'affiche (lecture seule)
3. L'utilisateur choisit Achat ou Vente (radio buttons)
4. Il saisit une quantité (supporte `.` et `,`)
5. Le montant total se calcule automatiquement :
```csharp
private double TotalAmount => currentTransaction.Quantity * (SelectedAsset?.CurrentPrice ?? 0);
```

#### Validations métier
```csharp
// Vérif budget pour un achat
if (currentTransaction.Type == "Achat" && Budget != null)
{
    if (TotalAmount > Budget.CurrentBalance)
    {
        ErrorMessage = $"Budget insuffisant. Disponible : {Budget.CurrentBalance:N2} $.";
        return;
    }
}

// Vérif quantité possédée pour une vente
if (currentTransaction.Type == "Vente")
{
    var assetTx = await PortfolioService.GetTransactionsByAssetAsync(currentTransaction.AssetId);
    double held = assetTx.Where(t => t.Type == "Achat").Sum(t => t.Quantity)
                - assetTx.Where(t => t.Type == "Vente").Sum(t => t.Quantity);
    if (currentTransaction.Quantity > held)
    {
        ErrorMessage = $"Vous ne possédez que {held:F4} unités de cet actif.";
        return;
    }
}
```

#### Parsing flexible de la quantité
```csharp
private double ParseDouble(string value)
{
    if (string.IsNullOrWhiteSpace(value)) return 0;
    var normalized = value.Replace(',', '.');   // Accepte "0,25" ou "0.25"
    return double.TryParse(normalized, NumberStyles.Any,
                           CultureInfo.InvariantCulture, out var result) ? result : 0;
}
```

### 9.6 `Transactions.razor` — Historique

**Route** : `/transactions`

Même pattern que `Assets.razor` :
- Barre de recherche temps réel
- Filtre par type de transaction (Achat/Vente)
- Utilise `TransactionTable` composant
- IQueryable dans le service (TP10)

### 9.7 `Analytics.razor` — Analyse avancée

**Route** : `/analytics`

Affiche :
- Table de performance triée par Gain/Perte %
- PieChart (répartition par type)
- ColumnChart (nombre d'actifs par type)
- LineChart (évolution mensuelle des achats/ventes)
- 3 cartes résumé : Total Investi, Total Vendu, Nombre de Transactions

Toutes ces données viennent de méthodes GroupBy côté serveur (TP9).

### 9.8 `BudgetSetup.razor` — Gestion du budget

**Route** : `/budget`

Affiche :
- Capital Initial
- Solde Disponible actuel

Formulaire pour ajouter des fonds :
```csharp
private async Task AddFunds()
{
    if (AmountToAdd > 0 && CurrentBudget != null)
    {
        CurrentBudget.InitialAmount += AmountToAdd;
        CurrentBudget.CurrentBalance += AmountToAdd;
        await PortfolioService.UpdateBudgetAsync(CurrentBudget);
        Message = $"{AmountToAdd:N2} $ ajoutés avec succès !";
    }
}
```

### 9.9 `Login.razor` & `Register.razor` — Authentification

**Route** : `/login`, `/register`
**Layout** : `EmptyLayout` (pas de sidebar)

Formulaire HTML classique (pas `EditForm`) qui poste vers une Minimal API :

```razor
<form method="post" action="/api/auth/login" data-enhance="false">
    <input type="email" name="email" required />
    <input type="password" name="password" required />
    <button type="submit">Se connecter</button>
</form>
```

**Pourquoi HTML et pas `EditForm`** ? Expliqué dans le TP11 : Blazor Interactive (WebSocket) ne peut pas modifier les cookies HTTP. La connexion doit passer par une requête HTTP standard qui peut écrire le cookie d'authentification.

**L'attribut `data-enhance="false"`** empêche Blazor d'intercepter le formulaire.

Redirection si déjà connecté :
```csharp
protected override async Task OnInitializedAsync()
{
    var authState = await AuthStateProvider.GetAuthenticationStateAsync();
    if (authState.User.Identity?.IsAuthenticated == true)
    {
        Nav.NavigateTo("/dashboard");
    }
}
```

---

## 10. Authentification & Autorisation (TP11)

### 10.1 Configuration Identity (TP11, Activité 2)

**Fichier** : `Program.cs`

```csharp
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

// Redirige automatiquement vers /login si non authentifié
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/login";
    options.LogoutPath = "/api/auth/logout";
});

builder.Services.AddCascadingAuthenticationState();
```

### 10.2 Seeding du compte Admin (TP11, Activité 2)

```csharp
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

    if (!await roleManager.RoleExistsAsync("Admin"))
        await roleManager.CreateAsync(new IdentityRole("Admin"));

    if (await userManager.FindByEmailAsync("admin@invest.com") == null)
    {
        var adminUser = new IdentityUser { UserName = "admin@invest.com", Email = "admin@invest.com" };
        var result = await userManager.CreateAsync(adminUser, "Admin123!");
        if (result.Succeeded)
            await userManager.AddToRoleAsync(adminUser, "Admin");
    }
}
```

### 10.3 Minimal API Endpoints (TP11, Activité 3)

```csharp
// --- LOGIN ---
app.MapPost("/api/auth/login", async (
    [FromServices] SignInManager<IdentityUser> signInManager,
    [FromForm] string email,
    [FromForm] string password) =>
{
    var result = await signInManager.PasswordSignInAsync(email, password,
                                                         isPersistent: true,
                                                         lockoutOnFailure: false);
    if (result.Succeeded) return Results.Redirect("/dashboard");
    return Results.Redirect("/login?error=Identifiants incorrects");
}).DisableAntiforgery();

// --- REGISTER ---
app.MapPost("/api/auth/register", async (
    [FromServices] UserManager<IdentityUser> userManager,
    [FromServices] SignInManager<IdentityUser> signInManager,
    [FromForm] string email,
    [FromForm] string password) =>
{
    var user = new IdentityUser { UserName = email, Email = email };
    var result = await userManager.CreateAsync(user, password);
    if (result.Succeeded)
    {
        await signInManager.SignInAsync(user, isPersistent: true);
        return Results.Redirect("/dashboard");
    }
    return Results.Redirect("/register?error=Erreur lors de la creation");
}).DisableAntiforgery();

// --- LOGOUT ---
app.MapPost("/api/auth/logout", async ([FromServices] SignInManager<IdentityUser> signInManager) =>
{
    await signInManager.SignOutAsync();
    return Results.Redirect("/login");
}).DisableAntiforgery();
```

### 10.4 `CascadingAuthenticationState` dans Routes.razor (TP11, Activité 2)

```razor
<CascadingAuthenticationState>
    <Router AppAssembly="typeof(Program).Assembly">
        <Found Context="routeData">
            <RouteView RouteData="routeData" DefaultLayout="typeof(Layout.MainLayout)" />
            <FocusOnNavigate RouteData="routeData" Selector="h1" />
        </Found>
    </Router>
</CascadingAuthenticationState>
```

Cela diffuse l'état d'authentification à travers tout l'arbre de composants.

### 10.5 Protection des pages

**Backend** — Protection au niveau URL (TP11, Activité 5) :
```razor
@attribute [Authorize]                       <!-- Connecté requis -->
@attribute [Authorize(Roles = "Admin")]      <!-- Admin requis -->
```

**Frontend** — Masquage visuel des boutons :
```razor
<AuthorizeView Roles="Admin">
    <Authorized>
        <a href="/edit-asset" class="btn btn-primary">+ Nouvel Actif</a>
    </Authorized>
</AuthorizeView>
```

### 10.6 Barre Supérieure avec authentification

**Fichier** : `Components/Layout/MainLayout.razor`

```razor
<div class="top-row px-4">
    <AuthorizeView Context="authState">
        <Authorized>
            <span>@authState.User.Identity?.Name</span>
            <form method="post" action="/api/auth/logout" data-enhance="false">
                <button type="submit" class="btn btn-outline-danger">Déconnexion</button>
            </form>
        </Authorized>
    </AuthorizeView>
</div>
```

### 10.7 Récapitulatif des permissions

| Action | Visiteur | User authentifié | Admin |
|--------|----------|------------------|-------|
| Voir la page `/login`, `/register` | ✅ | ✅ | ✅ |
| Voir Dashboard, Assets, etc. | ❌ (redirigé) | ✅ | ✅ |
| Passer une transaction | ❌ | ✅ | ✅ |
| Gérer son budget | ❌ | ✅ | ✅ |
| Voir l'historique des prix | ❌ | ✅ | ✅ |
| Créer / modifier / supprimer un actif | ❌ | ❌ | ✅ |
| Lancer "Simuler le marché" (bouton) | ❌ | ❌ | ✅ |


---

## 11. Simulation de Prix (Innovation/Créativité)

Point **Créativité** du barème (2 points). Le socle initial est respecté ; cette extension apporte une valeur analytique supplémentaire en simulant automatiquement le comportement d'un marché réel.

### 11.1 Méthode `SimulatePriceChangeAsync`

**Fichier** : `Services/PortfolioService.cs`

```csharp
private static readonly Random _rng = new Random();

public async Task SimulatePriceChangeAsync(int assetId)
{
    var asset = await _context.Assets.FindAsync(assetId);
    if (asset == null) return;

    // Volatilité selon le type d'actif
    double volatility = asset.AssetType switch
    {
        "Crypto" => 0.08, // ±8%
        "Action" => 0.04, // ±4%
        "ETF"    => 0.02, // ±2%
        _        => 0.03
    };

    // Formule : marche aléatoire gaussienne
    double variation = (_rng.NextDouble() * 2 - 1) * volatility;
    double newPrice = asset.CurrentPrice * (1 + variation);

    // Plancher pour éviter des prix négatifs
    asset.CurrentPrice = Math.Max(0.01, Math.Round(newPrice, 2));
    asset.LastUpdate = DateTime.Now;

    // Snapshot dans l'historique
    _context.PriceHistories.Add(new PriceHistory
    {
        AssetId = asset.Id,
        Price = asset.CurrentPrice,
        Timestamp = DateTime.Now
    });

    await _context.SaveChangesAsync();
}
```

### 11.2 Switch Expression (Chapitre 1)
Utilisation du "switch moderne" :
```csharp
double volatility = asset.AssetType switch
{
    "Crypto" => 0.08,
    "Action" => 0.04,
    "ETF"    => 0.02,
    _        => 0.03   // cas par défaut
};
```

### 11.3 Background Service (automatisation)

**Fichier** : `Services/PriceSimulationHostedService.cs`

```csharp
using Microsoft.Extensions.Hosting;

public class PriceSimulationHostedService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<PriceSimulationHostedService> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(1);

    public PriceSimulationHostedService(IServiceProvider services,
                                         ILogger<PriceSimulationHostedService> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Un nouveau scope à chaque tick (le DbContext est Scoped)
                using var scope = _services.CreateScope();
                var portfolioService = scope.ServiceProvider.GetRequiredService<IPortfolioService>();
                await portfolioService.SimulateAllPricesAsync();
                _logger.LogInformation("Prix mis à jour à {time}", DateTime.Now);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur pendant la simulation");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }
}
```

**Enregistrement dans Program.cs** :
```csharp
builder.Services.AddHostedService<PriceSimulationHostedService>();
```

Le `BackgroundService` tourne en arrière-plan pendant toute la durée de vie de l'application et modifie les prix **automatiquement toutes les minutes**.

### 11.4 Auto-refresh côté Dashboard

Pour que l'utilisateur voie les changements sans rafraîchir la page :

```csharp
private System.Threading.Timer _refreshTimer;

protected override async Task OnInitializedAsync()
{
    await LoadDashboard();

    _refreshTimer = new System.Threading.Timer(async _ =>
    {
        await InvokeAsync(async () =>
        {
            await LoadDashboard();
            StateHasChanged();
        });
    }, null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
}

public void Dispose()
{
    _refreshTimer?.Dispose();
}
```

### 11.5 Déclenchement manuel (bouton Admin)

Dans `Assets.razor`, un bouton réservé à l'Admin permet de forcer une simulation :

```razor
<AuthorizeView Roles="Admin">
    <Authorized>
        <button class="btn btn-outline-secondary" @onclick="SimulatePrices">
            <i class="bi bi-arrow-repeat me-1"></i> Simuler le marché
        </button>
    </Authorized>
</AuthorizeView>
```

```csharp
private async Task SimulatePrices()
{
    await PortfolioService.SimulateAllPricesAsync();
    await ExecuteSearch();
    SimulationMessage = "Prix du marché mis à jour.";
    StateHasChanged();
    await Task.Delay(4000);
    SimulationMessage = null;
    StateHasChanged();
}
```

---

## 12. Configuration (Program.cs)

**Fichier complet** : `Program.cs`

```csharp
using InvestPortfolio.Components;
using InvestPortfolio.Data;
using InvestPortfolio.Models;
using InvestPortfolio.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Radzen;

var builder = WebApplication.CreateBuilder(args);

// ===== 1. Ajout des services Blazor (Chapitre 3) =====
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// ===== 2. EF Core + SQLite (TP5) =====
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(connectionString));

// ===== 3. Identity (TP11) =====
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

// Redirection auto vers login
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/login";
    options.LogoutPath = "/api/auth/logout";
});

builder.Services.AddCascadingAuthenticationState();

// ===== 4. Services métier (TP4, TP6) =====
builder.Services.AddScoped<IPortfolioService, PortfolioService>();

// ===== 5. Radzen (TP9) =====
builder.Services.AddRadzenComponents();

// ===== 6. Background service (Créativité) =====
builder.Services.AddHostedService<PriceSimulationHostedService>();

var app = builder.Build();

// ===== 7. Seeding Admin + Budget =====
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<AppDbContext>();
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = services.GetRequiredService<UserManager<IdentityUser>>();

    context.Database.EnsureCreated();

    if (!await roleManager.RoleExistsAsync("Admin"))
        await roleManager.CreateAsync(new IdentityRole("Admin"));

    if (await userManager.FindByEmailAsync("admin@invest.com") == null)
    {
        var adminUser = new IdentityUser { UserName = "admin@invest.com", Email = "admin@invest.com" };
        var result = await userManager.CreateAsync(adminUser, "Admin123!");
        if (result.Succeeded)
            await userManager.AddToRoleAsync(adminUser, "Admin");
    }

    if (!context.Budgets.Any())
    {
        context.Budgets.Add(new Budget
        {
            InitialAmount = 0,
            CurrentBalance = 0,
            CreatedAt = DateTime.Now
        });
        context.SaveChanges();
    }
}

// ===== 8. Pipeline HTTP =====
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

// ===== 9. Endpoints Auth (Minimal API - TP11) =====
app.MapPost("/api/auth/login", /* ... */).DisableAntiforgery();
app.MapPost("/api/auth/register", /* ... */).DisableAntiforgery();
app.MapPost("/api/auth/logout", /* ... */).DisableAntiforgery();

app.Run();
```

---

## 13. Formules & Calculs Métier

### 13.1 Valeur du portefeuille
```
ValeurPortefeuille = Σ (QuantitéDétenue × PrixActuel) pour chaque actif
```
où `QuantitéDétenue = Σ quantité(Achats) − Σ quantité(Ventes)`

### 13.2 Gain / Perte
```
GainPerte = (ValeurActuelle + TotalVendu) − TotalInvesti
```
où :
- `TotalInvesti = Σ (quantité × prix) pour tous les achats`
- `TotalVendu = Σ (quantité × prix) pour toutes les ventes`
- `ValeurActuelle = QuantitéDétenue × PrixActuel`

### 13.3 Pourcentage de rendement
```
Rendement% = (GainPerte / CapitalInitial) × 100
```

### 13.4 Prix d'achat moyen pondéré (PAM)
```
PAM = Σ(Quantité × PrixAchat) / Σ(Quantité des achats)
```

### 13.5 Variation de prix (simulation)
```
NouveauPrix = PrixActuel × (1 + variation)
où variation ∈ [−volatilité, +volatilité]
et volatilité = 8% pour Crypto, 4% pour Action, 2% pour ETF
```

### 13.6 Montant d'une transaction
```
MontantTotal = Quantité × PrixUnitaire
```
(propriété calculée `Expression-Bodied` dans Transaction.cs)

---

## 14. Correspondance TP → Fonctionnalité

| TP / Chapitre | Notion | Utilisation dans InvestPortfolio |
|---------------|--------|----------------------------------|
| **Chapitre 1** | Types, classes, propriétés, switch expression | Tous les modèles, propriété calculée `TotalAmount`, switch dans simulation |
| **Chapitre 2** | LINQ, Records | `Where`, `Select`, `GroupBy`, `Sum`, `Count`, `OrderBy` partout dans `PortfolioService` |
| **TP3 / Chapitre 3** | Blazor, Interactive Server, @page, @bind | Toutes les pages `.razor`, mode `InteractiveServer` |
| **TP4 / Chapitre 4** | Architecture en couches, DI, interfaces | `IPortfolioService` / `PortfolioService`, `AddScoped`, cycle de vie Scoped |
| **TP5 / Chapitre 5** | EF Core Code-First, relations 1-N et N-N, migrations | Modèles Asset/Transaction/PriceHistory/Tag, `AppDbContext`, `dotnet ef migrations` |
| **TP6 / Chapitre 5** | Async, `Include()`, filtrage LINQ, agrégations (CountAsync, AverageAsync) | Toutes les méthodes du service, KPIs du Dashboard |
| **TP7 / Chapitre 6** | `EditForm`, `DataAnnotationsValidator`, Unification CRUD | `EditAsset.razor`, `NewTransaction.razor`, `BudgetSetup.razor` |
| **TP8** | Composants `[Parameter]`, `EventCallback` | `KpiCard`, `AssetTable`, `AssetCard`, `TransactionTable` |
| **TP9** | Radzen, GroupBy pour graphiques, SeriesClick, Gauge | Dashboard, Analytics, AssetDetails — DonutChart, ColumnChart, LineChart, RadialGauge |
| **TP10** | Recherche temps réel, IQueryable, filtrage SQL dynamique | `Assets.razor`, `Transactions.razor`, `SearchAssetsAsync`, `SearchTransactionsAsync` |
| **TP11** | Identity, CascadingAuthenticationState, [Authorize], AuthorizeView, Minimal API Auth | Login/Register, protection pages, Admin-only actions |
| **Créativité** | BackgroundService, `System.Threading.Timer`, random walk | Simulation de prix auto, auto-refresh Dashboard |

---

## 15. Guide d'Utilisation

### 15.1 Démarrer l'application

```bash
cd InvestPortfolio
dotnet run
```

L'application démarre sur `http://localhost:5202`.

### 15.2 Compte par défaut

| Rôle | Email | Mot de passe |
|------|-------|--------------|
| Admin | `admin@invest.com` | `Admin123!` |

Les utilisateurs normaux peuvent s'inscrire via `/register`.

### 15.3 Scénario d'utilisation complet

**En tant qu'Admin :**
1. Se connecter sur `/login` avec les identifiants admin
2. Aller sur `/assets` → "Nouvel Actif" → créer par exemple :
   - "Bitcoin" / "BTC" / Crypto / 68000 $
   - "Apple" / "AAPL" / Action / 192 $
3. Cliquer sur "Simuler le marché" pour faire varier les prix

**En tant qu'Utilisateur :**
1. Créer un compte sur `/register`
2. Aller sur `/budget` → ajouter des fonds (ex: 10 000 $)
3. Aller sur `/new-transaction` :
   - Choisir Bitcoin → prix affiché automatiquement
   - Choisir "Achat"
   - Saisir quantité `0.05`
   - Voir le montant total calculé : 3 400 $
   - Valider
4. Le Dashboard affiche instantanément :
   - Valeur Portefeuille, Gain/Perte, Répartition, etc.
5. Attendre 1 minute → les prix changent automatiquement → le Dashboard se rafraîchit tout seul toutes les 30 secondes
6. Cliquer sur un actif dans `/assets` → voir le graphique d'évolution du prix

### 15.4 Recréer la base de données from scratch

```bash
# Supprimer la DB
rm portfolio.db*

# Recréer depuis la migration
dotnet ef database update
```

### 15.5 Ajouter une nouvelle migration (après modification d'un modèle)

```bash
dotnet ef migrations add NomDeLaMigration
dotnet ef database update
```

---

## Annexe A — Diagramme de flux d'une transaction d'achat

```
Utilisateur clique "Nouvelle Transaction"
         │
         ▼
   /new-transaction
         │
         ├── Page charge les actifs disponibles (GetAssetsAsync)
         ├── Page charge le budget (GetBudgetAsync)
         │
         ▼
Utilisateur choisit un actif
         │
         ├── OnAssetChanged() → prix auto-rempli
         │
         ▼
Utilisateur saisit quantité 0.25
         │
         ├── OnQuantityInput() → parse "0.25"
         ├── TotalAmount calculé en temps réel
         │
         ▼
Utilisateur clique "Valider"
         │
         ▼
HandleValidSubmit()
         │
         ├── Vérif budget (si Achat) → sinon ErrorMessage
         ├── Vérif quantité détenue (si Vente) → sinon ErrorMessage
         │
         ▼
PortfolioService.AddTransactionAsync(transaction)
         │
         ├── _context.Transactions.Add(transaction)
         ├── budget.CurrentBalance -= TotalAmount
         ├── await _context.SaveChangesAsync()
         │
         ▼
NavigationManager.NavigateTo("/transactions")
         │
         ▼
Dashboard et Assets se rafraîchissent (Timer de 30s)
```

---

## Annexe B — Exécution du serveur (test de santé)

Commandes testées lors du développement :
- `GET http://localhost:5202/` sans cookie → **302 Redirect** vers `/login` ✅
- `POST /api/auth/login` avec `admin@invest.com` / `Admin123!` → **302 Redirect** vers `/dashboard` + cookie auth ✅
- `GET http://localhost:5202/dashboard` sans cookie → **302 Redirect** vers `/login` ✅
- `GET http://localhost:5202/edit-asset` en tant qu'utilisateur non-admin → **302 Redirect** (Forbidden) ✅
- Build sans erreurs ni warnings ✅

---

## Annexe C — Respect strict du barème

| Critère (sur 20) | Points | Statut | Emplacement |
|------------------|--------|--------|-------------|
| Blazor Web Application (.NET 10) | — | ✅ | `InvestPortfolio.csproj` |
| Architecture propre en couches | — | ✅ | `UI / Services / Data` |
| EF Core Code-First + Migrations | — | ✅ | `Models/`, `Data/AppDbContext.cs`, `Migrations/` |
| CRUD complet | — | ✅ | `PortfolioService` + pages `EditAsset`, `NewTransaction` |
| Recherche & filtrage LINQ | — | ✅ | `SearchAssetsAsync`, `SearchTransactionsAsync` |
| Tableau de bord analytique | — | ✅ | `Dashboard.razor`, `Analytics.razor` |
| Validation Data Annotations | — | ✅ | Tous les modèles |
| Asynchronisme | — | ✅ | Tout est `async/await` |
| Authentification | — | ✅ | TP11 respecté intégralement |
| **Créativité — Simulation marché auto** | +2 | ✅ | `PriceSimulationHostedService` |

---

**Fin de la documentation.**

Réalisé pour le module Programmation .NET C# — 4ème Génie Informatique / Data Science
Année universitaire 2025-2026

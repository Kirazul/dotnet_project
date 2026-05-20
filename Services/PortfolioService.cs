using InvestPortfolio.Data;
using InvestPortfolio.Models;
using Microsoft.EntityFrameworkCore;

namespace InvestPortfolio.Services
{
    public class PortfolioService : IPortfolioService
    {
        private readonly AppDbContext _context;

        // Injection de dépendance du DbContext
        public PortfolioService(AppDbContext context)
        {
            _context = context;
        }

        // ===== Assets CRUD =====

        public async Task<List<Asset>> GetAssetsAsync()
        {
            return await _context.Assets
                .Include(a => a.Tags)
                .ToListAsync();
        }

        public async Task<Asset> GetAssetByIdAsync(int id)
        {
            return await _context.Assets.FindAsync(id);
        }

        public async Task AddAssetAsync(Asset asset)
        {
            asset.LastUpdate = DateTime.Now;

            // Historisation du prix initial
            asset.PriceHistories.Add(new PriceHistory
            {
                Price = asset.CurrentPrice,
                Timestamp = DateTime.Now
            });

            _context.Assets.Add(asset);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAssetAsync(Asset asset)
        {
            asset.LastUpdate = DateTime.Now;

            // Ajout à l'historique lors d'une modification
            asset.PriceHistories.Add(new PriceHistory
            {
                Price = asset.CurrentPrice,
                Timestamp = DateTime.Now
            });

            _context.Assets.Update(asset);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAssetAsync(int id)
        {
            var asset = await _context.Assets.FindAsync(id);
            if (asset != null)
            {
                _context.Assets.Remove(asset);
                await _context.SaveChangesAsync();
            }
        }

        public async Task ReloadAssetAsync(Asset asset)
        {
            await _context.Entry(asset).ReloadAsync();
        }

        // ===== Historique des prix =====
        public async Task<List<PriceHistory>> GetPriceHistoryAsync(int assetId)
        {
            return await _context.PriceHistories
                .Where(p => p.AssetId == assetId)
                .OrderBy(p => p.Timestamp)
                .ToListAsync();
        }

        // ===== Simulation de variation de prix =====
        // Modèle : marche aléatoire gaussienne (random walk)
        // Formule : nouveauPrix = ancienPrix × (1 + variation)
        // où variation ∈ [-5%, +5%] selon le type d'actif
        private static readonly Random _rng = new Random();

        public async Task SimulatePriceChangeAsync(int assetId)
        {
            var asset = await _context.Assets.FindAsync(assetId);
            if (asset == null) return;

            double volatility = asset.AssetType switch
            {
                "Crypto" => 0.08, // ±8% (très volatile)
                "Action" => 0.04, // ±4%
                "ETF"    => 0.02, // ±2% (stable)
                _        => 0.03
            };

            // Variation aléatoire entre -volatility et +volatility
            double variation = (_rng.NextDouble() * 2 - 1) * volatility;
            double newPrice = asset.CurrentPrice * (1 + variation);

            // Plancher à 0.01 pour éviter des prix négatifs
            asset.CurrentPrice = Math.Max(0.01, Math.Round(newPrice, 2));
            asset.LastUpdate = DateTime.Now;

            // Enregistrement dans l'historique
            _context.PriceHistories.Add(new PriceHistory
            {
                AssetId = asset.Id,
                Price = asset.CurrentPrice,
                Timestamp = DateTime.Now
            });

            await _context.SaveChangesAsync();
        }

        public async Task SimulateAllPricesAsync()
        {
            var assets = await _context.Assets.ToListAsync();
            foreach (var asset in assets)
            {
                await SimulatePriceChangeAsync(asset.Id);
            }
        }

        // ===== Transactions CRUD =====

        public async Task<List<Transaction>> GetTransactionsAsync()
        {
            return await _context.Transactions
                .Include(t => t.Asset)
                .OrderByDescending(t => t.Date)
                .ToListAsync();
        }

        public async Task<List<Transaction>> GetTransactionsByAssetAsync(int assetId)
        {
            return await _context.Transactions
                .Include(t => t.Asset)
                .Where(t => t.AssetId == assetId)
                .OrderByDescending(t => t.Date)
                .ToListAsync();
        }

        public async Task<Transaction> GetTransactionByIdAsync(int id)
        {
            return await _context.Transactions.FindAsync(id);
        }

        public async Task AddTransactionAsync(Transaction transaction)
        {
            transaction.Date = DateTime.Now;
            _context.Transactions.Add(transaction);

            // Mettre à jour le budget
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

        public async Task DeleteTransactionAsync(int id)
        {
            var transaction = await _context.Transactions.FindAsync(id);
            if (transaction != null)
            {
                // Reverser l'impact sur le budget
                var budget = await _context.Budgets.FirstOrDefaultAsync();
                if (budget != null)
                {
                    if (transaction.Type == "Achat")
                        budget.CurrentBalance += transaction.Quantity * transaction.UnitPrice;
                    else
                        budget.CurrentBalance -= transaction.Quantity * transaction.UnitPrice;

                    budget.LastUpdate = DateTime.Now;
                }

                _context.Transactions.Remove(transaction);
                await _context.SaveChangesAsync();
            }
        }

        // ===== Budget =====

        public async Task<Budget> GetBudgetAsync()
        {
            return await _context.Budgets.FirstOrDefaultAsync();
        }

        public async Task UpdateBudgetAsync(Budget budget)
        {
            budget.LastUpdate = DateTime.Now;
            _context.Budgets.Update(budget);
            await _context.SaveChangesAsync();
        }

        // ===== KPIs & Agrégations =====

        public async Task<int> GetTotalAssetsCountAsync()
        {
            return await _context.Assets.CountAsync();
        }

        public async Task<double> GetPortfolioValueAsync()
        {
            // Valeur du portefeuille = somme (quantité achetée - quantité vendue) * prix actuel par actif
            var assets = await _context.Assets
                .Include(a => a.Transactions)
                .ToListAsync();

            double totalValue = 0;
            foreach (var asset in assets)
            {
                double quantityHeld = asset.Transactions
                    .Where(t => t.Type == "Achat").Sum(t => t.Quantity)
                    - asset.Transactions
                    .Where(t => t.Type == "Vente").Sum(t => t.Quantity);

                if (quantityHeld > 0)
                    totalValue += quantityHeld * asset.CurrentPrice;
            }

            return totalValue;
        }

        public async Task<double> GetTotalGainLossAsync()
        {
            var assets = await _context.Assets
                .Include(a => a.Transactions)
                .ToListAsync();

            double totalGainLoss = 0;
            foreach (var asset in assets)
            {
                double totalInvested = asset.Transactions
                    .Where(t => t.Type == "Achat").Sum(t => t.Quantity * t.UnitPrice);
                double totalSold = asset.Transactions
                    .Where(t => t.Type == "Vente").Sum(t => t.Quantity * t.UnitPrice);
                double quantityHeld = asset.Transactions
                    .Where(t => t.Type == "Achat").Sum(t => t.Quantity)
                    - asset.Transactions
                    .Where(t => t.Type == "Vente").Sum(t => t.Quantity);

                double currentValue = quantityHeld * asset.CurrentPrice;
                totalGainLoss += (currentValue + totalSold) - totalInvested;
            }

            return totalGainLoss;
        }

        public async Task<double> GetBudgetBalanceAsync()
        {
            var budget = await _context.Budgets.FirstOrDefaultAsync();
            return budget?.CurrentBalance ?? 0;
        }

        // ===== Statistiques pour graphiques (LINQ GroupBy) =====

        public async Task<List<AssetAllocationStat>> GetAllocationByTypeAsync()
        {
            var assets = await _context.Assets
                .Include(a => a.Transactions)
                .ToListAsync();

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

        public async Task<List<AssetPerformanceStat>> GetPerformanceByAssetAsync()
        {
            var assets = await _context.Assets
                .Include(a => a.Transactions)
                .ToListAsync();

            var stats = assets.Select(a =>
            {
                double totalInvested = a.Transactions
                    .Where(t => t.Type == "Achat").Sum(t => t.Quantity * t.UnitPrice);
                double totalSold = a.Transactions
                    .Where(t => t.Type == "Vente").Sum(t => t.Quantity * t.UnitPrice);
                double quantityHeld = a.Transactions
                    .Where(t => t.Type == "Achat").Sum(t => t.Quantity)
                    - a.Transactions.Where(t => t.Type == "Vente").Sum(t => t.Quantity);

                double currentValue = quantityHeld > 0 ? quantityHeld * a.CurrentPrice : 0;
                double gainLoss = (currentValue + totalSold) - totalInvested;
                double gainLossPercent = totalInvested > 0 ? (gainLoss / totalInvested) * 100 : 0;

                return new AssetPerformanceStat
                {
                    AssetName = a.Name,
                    Symbol = a.Symbol,
                    GainLoss = gainLoss,
                    GainLossPercent = gainLossPercent
                };
            }).ToList();

            return stats;
        }

        public async Task<List<MonthlyTransactionStat>> GetMonthlyTransactionsAsync()
        {
            var transactions = await _context.Transactions.ToListAsync();

            var stats = transactions
                .GroupBy(t => t.Date.ToString("yyyy-MM"))
                .OrderBy(g => g.Key)
                .Select(g => new MonthlyTransactionStat
                {
                    Month = g.Key,
                    TotalBuy = g.Where(t => t.Type == "Achat").Sum(t => t.Quantity * t.UnitPrice),
                    TotalSell = g.Where(t => t.Type == "Vente").Sum(t => t.Quantity * t.UnitPrice)
                })
                .ToList();

            return stats;
        }

        // ===== Recherche & Filtrage (IQueryable) =====

        public async Task<List<Asset>> SearchAssetsAsync(string searchText, string assetType)
        {
            // IQueryable : construction de la requête SQL étape par étape
            IQueryable<Asset> query = _context.Assets.Include(a => a.Tags).AsQueryable();

            if (!string.IsNullOrEmpty(assetType))
            {
                query = query.Where(a => a.AssetType == assetType);
            }

            if (!string.IsNullOrEmpty(searchText))
            {
                query = query.Where(a => a.Name.Contains(searchText) || a.Symbol.Contains(searchText));
            }

            return await query.ToListAsync();
        }

        public async Task<List<Transaction>> SearchTransactionsAsync(string assetName, string transactionType)
        {
            IQueryable<Transaction> query = _context.Transactions.Include(t => t.Asset).AsQueryable();

            if (!string.IsNullOrEmpty(assetName))
            {
                query = query.Where(t => t.Asset.Name.Contains(assetName) || t.Asset.Symbol.Contains(assetName));
            }

            if (!string.IsNullOrEmpty(transactionType))
            {
                query = query.Where(t => t.Type == transactionType);
            }

            return await query.OrderByDescending(t => t.Date).ToListAsync();
        }
    }
}

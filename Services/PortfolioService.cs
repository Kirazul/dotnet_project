using InvestPortfolio.Data;
using InvestPortfolio.Models;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace InvestPortfolio.Services
{
    public class PortfolioService : IPortfolioService
    {
        private readonly AppDbContext _context;
        private readonly AuthenticationStateProvider _authStateProvider;

        public PortfolioService(AppDbContext context, AuthenticationStateProvider authStateProvider)
        {
            _context = context;
            _authStateProvider = authStateProvider;
        }

        private async Task<string> GetCurrentUserIdAsync()
        {
            var authState = await _authStateProvider.GetAuthenticationStateAsync();
            var userId = authState.User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new UnauthorizedAccessException("Utilisateur non authentifié.");
            }

            return userId;
        }

        private async Task<Budget> GetOrCreateBudgetAsync(string userId)
        {
            var budget = await _context.Budgets.FirstOrDefaultAsync(b => b.UserId == userId);
            if (budget != null)
            {
                return budget;
            }

            budget = new Budget
            {
                UserId = userId,
                InitialAmount = 0,
                CurrentBalance = 0,
                CreatedAt = DateTime.Now,
                LastUpdate = DateTime.Now
            };

            _context.Budgets.Add(budget);
            await _context.SaveChangesAsync();
            return budget;
        }

        // ===== Assets CRUD =====

        public async Task<List<Asset>> GetAssetsAsync()
        {
            var userId = await GetCurrentUserIdAsync();
            return await _context.Assets
                .Include(a => a.Tags)
                .Where(a => a.UserId == userId)
                .ToListAsync();
        }

        public async Task<Asset> GetAssetByIdAsync(int id)
        {
            var userId = await GetCurrentUserIdAsync();
            return await _context.Assets
                .Include(a => a.Tags)
                .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);
        }

        public async Task AddAssetAsync(Asset asset)
        {
            var userId = await GetCurrentUserIdAsync();

            asset.Id = 0;
            asset.UserId = userId;
            asset.LastUpdate = DateTime.Now;

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
            var userId = await GetCurrentUserIdAsync();
            var existing = await _context.Assets
                .Include(a => a.PriceHistories)
                .FirstOrDefaultAsync(a => a.Id == asset.Id && a.UserId == userId);

            if (existing == null)
            {
                return;
            }

            existing.Name = asset.Name;
            existing.Symbol = asset.Symbol;
            existing.AssetType = asset.AssetType;
            existing.CurrentPrice = asset.CurrentPrice;
            existing.LastUpdate = DateTime.Now;
            existing.PriceHistories.Add(new PriceHistory
            {
                Price = existing.CurrentPrice,
                Timestamp = DateTime.Now
            });

            await _context.SaveChangesAsync();
        }

        public async Task DeleteAssetAsync(int id)
        {
            var userId = await GetCurrentUserIdAsync();
            var asset = await _context.Assets.FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);
            if (asset != null)
            {
                _context.Assets.Remove(asset);
                await _context.SaveChangesAsync();
            }
        }

        public async Task ReloadAssetAsync(Asset asset)
        {
            var userId = await GetCurrentUserIdAsync();
            var isOwned = await _context.Assets.AnyAsync(a => a.Id == asset.Id && a.UserId == userId);
            if (isOwned)
            {
                await _context.Entry(asset).ReloadAsync();
            }
        }

        // ===== Historique des prix =====

        public async Task<List<PriceHistory>> GetPriceHistoryAsync(int assetId)
        {
            var userId = await GetCurrentUserIdAsync();
            return await _context.PriceHistories
                .Where(p => p.AssetId == assetId && p.Asset.UserId == userId)
                .OrderBy(p => p.Timestamp)
                .ToListAsync();
        }

        // ===== Simulation de variation de prix =====

        private static readonly Random _rng = new Random();

        private void ApplyPriceChange(Asset asset)
        {
            double volatility = asset.AssetType switch
            {
                "Crypto" => 0.08,
                "Action" => 0.04,
                "ETF" => 0.02,
                _ => 0.03
            };

            double variation = (_rng.NextDouble() * 2 - 1) * volatility;
            double newPrice = asset.CurrentPrice * (1 + variation);

            asset.CurrentPrice = Math.Max(0.01, Math.Round(newPrice, 2));
            asset.LastUpdate = DateTime.Now;

            _context.PriceHistories.Add(new PriceHistory
            {
                AssetId = asset.Id,
                Price = asset.CurrentPrice,
                Timestamp = DateTime.Now
            });
        }

        public async Task SimulatePriceChangeAsync(int assetId)
        {
            var userId = await GetCurrentUserIdAsync();
            var asset = await _context.Assets.FirstOrDefaultAsync(a => a.Id == assetId && a.UserId == userId);
            if (asset == null) return;

            ApplyPriceChange(asset);
            await _context.SaveChangesAsync();
        }

        public async Task SimulateOwnedPricesAsync()
        {
            var userId = await GetCurrentUserIdAsync();
            var assets = await _context.Assets.Where(a => a.UserId == userId).ToListAsync();

            foreach (var asset in assets)
            {
                ApplyPriceChange(asset);
            }

            await _context.SaveChangesAsync();
        }

        public async Task SimulateAllPricesAsync()
        {
            var assets = await _context.Assets.ToListAsync();

            foreach (var asset in assets)
            {
                ApplyPriceChange(asset);
            }

            await _context.SaveChangesAsync();
        }

        // ===== Transactions CRUD =====

        public async Task<List<Transaction>> GetTransactionsAsync()
        {
            var userId = await GetCurrentUserIdAsync();
            return await _context.Transactions
                .Include(t => t.Asset)
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.Date)
                .ToListAsync();
        }

        public async Task<List<Transaction>> GetTransactionsByAssetAsync(int assetId)
        {
            var userId = await GetCurrentUserIdAsync();
            return await _context.Transactions
                .Include(t => t.Asset)
                .Where(t => t.UserId == userId && t.AssetId == assetId)
                .OrderByDescending(t => t.Date)
                .ToListAsync();
        }

        public async Task<Transaction> GetTransactionByIdAsync(int id)
        {
            var userId = await GetCurrentUserIdAsync();
            return await _context.Transactions.FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);
        }

        public async Task AddTransactionAsync(Transaction transaction)
        {
            var userId = await GetCurrentUserIdAsync();
            var asset = await _context.Assets.FirstOrDefaultAsync(a => a.Id == transaction.AssetId && a.UserId == userId);
            if (asset == null)
            {
                throw new InvalidOperationException("Actif introuvable pour cet utilisateur.");
            }

            transaction.Id = 0;
            transaction.UserId = userId;
            transaction.Asset = asset;
            transaction.UnitPrice = asset.CurrentPrice;
            transaction.Date = DateTime.Now;

            if (transaction.Type == "Vente")
            {
                double held = await _context.Transactions
                    .Where(t => t.UserId == userId && t.AssetId == asset.Id && t.Type == "Achat")
                    .SumAsync(t => t.Quantity)
                    - await _context.Transactions
                    .Where(t => t.UserId == userId && t.AssetId == asset.Id && t.Type == "Vente")
                    .SumAsync(t => t.Quantity);

                if (transaction.Quantity > held)
                {
                    throw new InvalidOperationException("Quantité insuffisante pour cette vente.");
                }
            }

            var budget = await GetOrCreateBudgetAsync(userId);
            if (transaction.Type == "Achat")
            {
                if (transaction.TotalAmount > budget.CurrentBalance)
                {
                    throw new InvalidOperationException("Budget insuffisant.");
                }

                budget.CurrentBalance -= transaction.TotalAmount;
            }
            else
            {
                budget.CurrentBalance += transaction.TotalAmount;
            }

            budget.LastUpdate = DateTime.Now;
            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteTransactionAsync(int id)
        {
            var userId = await GetCurrentUserIdAsync();
            var transaction = await _context.Transactions.FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);
            if (transaction != null)
            {
                var budget = await GetOrCreateBudgetAsync(userId);
                if (transaction.Type == "Achat")
                    budget.CurrentBalance += transaction.TotalAmount;
                else
                    budget.CurrentBalance -= transaction.TotalAmount;

                budget.LastUpdate = DateTime.Now;
                _context.Transactions.Remove(transaction);
                await _context.SaveChangesAsync();
            }
        }

        // ===== Budget =====

        public async Task<Budget> GetBudgetAsync()
        {
            var userId = await GetCurrentUserIdAsync();
            return await GetOrCreateBudgetAsync(userId);
        }

        public async Task UpdateBudgetAsync(Budget budget)
        {
            var userId = await GetCurrentUserIdAsync();
            var existing = await _context.Budgets.FirstOrDefaultAsync(b => b.Id == budget.Id && b.UserId == userId)
                ?? await _context.Budgets.FirstOrDefaultAsync(b => b.UserId == userId);

            if (existing == null)
            {
                budget.Id = 0;
                budget.UserId = userId;
                budget.LastUpdate = DateTime.Now;
                _context.Budgets.Add(budget);
            }
            else
            {
                existing.InitialAmount = budget.InitialAmount;
                existing.CurrentBalance = budget.CurrentBalance;
                existing.LastUpdate = DateTime.Now;
            }

            await _context.SaveChangesAsync();
        }

        // ===== KPIs & Agrégations =====

        public async Task<int> GetTotalAssetsCountAsync()
        {
            var userId = await GetCurrentUserIdAsync();
            return await _context.Assets.CountAsync(a => a.UserId == userId);
        }

        public async Task<double> GetPortfolioValueAsync()
        {
            var userId = await GetCurrentUserIdAsync();
            var assets = await _context.Assets
                .Include(a => a.Transactions)
                .Where(a => a.UserId == userId)
                .ToListAsync();

            double totalValue = 0;
            foreach (var asset in assets)
            {
                double quantityHeld = asset.Transactions
                    .Where(t => t.UserId == userId && t.Type == "Achat").Sum(t => t.Quantity)
                    - asset.Transactions
                    .Where(t => t.UserId == userId && t.Type == "Vente").Sum(t => t.Quantity);

                if (quantityHeld > 0)
                    totalValue += quantityHeld * asset.CurrentPrice;
            }

            return totalValue;
        }

        public async Task<double> GetTotalGainLossAsync()
        {
            var userId = await GetCurrentUserIdAsync();
            var assets = await _context.Assets
                .Include(a => a.Transactions)
                .Where(a => a.UserId == userId)
                .ToListAsync();

            double totalGainLoss = 0;
            foreach (var asset in assets)
            {
                double totalInvested = asset.Transactions
                    .Where(t => t.UserId == userId && t.Type == "Achat").Sum(t => t.Quantity * t.UnitPrice);
                double totalSold = asset.Transactions
                    .Where(t => t.UserId == userId && t.Type == "Vente").Sum(t => t.Quantity * t.UnitPrice);
                double quantityHeld = asset.Transactions
                    .Where(t => t.UserId == userId && t.Type == "Achat").Sum(t => t.Quantity)
                    - asset.Transactions
                    .Where(t => t.UserId == userId && t.Type == "Vente").Sum(t => t.Quantity);

                double currentValue = quantityHeld * asset.CurrentPrice;
                totalGainLoss += (currentValue + totalSold) - totalInvested;
            }

            return totalGainLoss;
        }

        public async Task<double> GetBudgetBalanceAsync()
        {
            var budget = await GetBudgetAsync();
            return budget.CurrentBalance;
        }

        // ===== Statistiques pour graphiques (LINQ GroupBy) =====

        public async Task<List<AssetAllocationStat>> GetAllocationByTypeAsync()
        {
            var userId = await GetCurrentUserIdAsync();
            var assets = await _context.Assets
                .Include(a => a.Transactions)
                .Where(a => a.UserId == userId)
                .ToListAsync();

            return assets
                .GroupBy(a => a.AssetType)
                .Select(g => new AssetAllocationStat
                {
                    AssetType = g.Key,
                    Count = g.Count(),
                    TotalValue = g.Sum(a =>
                    {
                        double qty = a.Transactions.Where(t => t.UserId == userId && t.Type == "Achat").Sum(t => t.Quantity)
                                   - a.Transactions.Where(t => t.UserId == userId && t.Type == "Vente").Sum(t => t.Quantity);
                        return qty > 0 ? qty * a.CurrentPrice : 0;
                    })
                })
                .ToList();
        }

        public async Task<List<AssetPerformanceStat>> GetPerformanceByAssetAsync()
        {
            var userId = await GetCurrentUserIdAsync();
            var assets = await _context.Assets
                .Include(a => a.Transactions)
                .Where(a => a.UserId == userId)
                .ToListAsync();

            return assets.Select(a =>
            {
                double totalInvested = a.Transactions
                    .Where(t => t.UserId == userId && t.Type == "Achat").Sum(t => t.Quantity * t.UnitPrice);
                double totalSold = a.Transactions
                    .Where(t => t.UserId == userId && t.Type == "Vente").Sum(t => t.Quantity * t.UnitPrice);
                double quantityHeld = a.Transactions
                    .Where(t => t.UserId == userId && t.Type == "Achat").Sum(t => t.Quantity)
                    - a.Transactions.Where(t => t.UserId == userId && t.Type == "Vente").Sum(t => t.Quantity);

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
        }

        public async Task<List<MonthlyTransactionStat>> GetMonthlyTransactionsAsync()
        {
            var userId = await GetCurrentUserIdAsync();
            var transactions = await _context.Transactions
                .Where(t => t.UserId == userId)
                .ToListAsync();

            return transactions
                .GroupBy(t => t.Date.ToString("yyyy-MM"))
                .OrderBy(g => g.Key)
                .Select(g => new MonthlyTransactionStat
                {
                    Month = g.Key,
                    TotalBuy = g.Where(t => t.Type == "Achat").Sum(t => t.Quantity * t.UnitPrice),
                    TotalSell = g.Where(t => t.Type == "Vente").Sum(t => t.Quantity * t.UnitPrice)
                })
                .ToList();
        }

        // ===== Recherche & Filtrage (IQueryable) =====

        public async Task<List<Asset>> SearchAssetsAsync(string searchText, string assetType)
        {
            var userId = await GetCurrentUserIdAsync();
            IQueryable<Asset> query = _context.Assets
                .Include(a => a.Tags)
                .Where(a => a.UserId == userId)
                .AsQueryable();

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
            var userId = await GetCurrentUserIdAsync();
            IQueryable<Transaction> query = _context.Transactions
                .Include(t => t.Asset)
                .Where(t => t.UserId == userId)
                .AsQueryable();

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

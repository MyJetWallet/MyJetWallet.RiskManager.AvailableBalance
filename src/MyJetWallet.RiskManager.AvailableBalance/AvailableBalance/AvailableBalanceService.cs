using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MyJetWallet.Domain.Assets;
using Service.AssetsDictionary.Client;
using Service.Balances.Domain.Models;
using Service.Balances.Grpc;
using Service.ClientRiskManager.Grpc;
using Service.ClientRiskManager.Grpc.Models;
using Service.HighYieldEngine.Domain.Models.Dtos;
using Service.HighYieldEngine.Grpc;
using Service.HighYieldEngine.Grpc.Models.ClientOffer;
using Service.IndexPrices.Client;

namespace MyJetWallet.RiskManager.AvailableBalance.AvailableBalance
{
    public class AvailableBalanceService : IAvailableBalanceService
    {
        private readonly IWalletBalanceService _balanceService;
        private readonly IAssetsDictionaryClient _assetsDictionaryClient;
        private readonly IClientLimitsRiskService _clientLimitsRiskService;
        private readonly IIndexPricesClient _indexPricesClient;
        private readonly IHighYieldEngineClientOfferService _highYieldEngineClientOfferService;

        private readonly ILogger<AvailableBalanceService> _logger;

        public AvailableBalanceService(
            ILogger<AvailableBalanceService> logger, 
            IWalletBalanceService balanceService, 
            IAssetsDictionaryClient assetsDictionaryClient, 
            IClientLimitsRiskService clientLimitsRiskService, 
            IIndexPricesClient indexPricesClient, 
            IHighYieldEngineClientOfferService highYieldEngineClientOfferService)
        {
            _logger = logger;
            _balanceService = balanceService;
            _assetsDictionaryClient = assetsDictionaryClient;
            _clientLimitsRiskService = clientLimitsRiskService;
            _indexPricesClient = indexPricesClient;
            _highYieldEngineClientOfferService = highYieldEngineClientOfferService;
        }

        public async ValueTask<List<Models.AvailableBalance>> CalculateAvailableWalletBalances(string walletId, string clientId,
            string brokerId, string brandId, string baseAsset, string language, List<string> assetShowOnlyWithBalance)
        {
            var balances = await CalculateBalancesForWalletBalances(
                walletId, clientId, brokerId, assetShowOnlyWithBalance);

            await CalculateCardReserveForWalletBalances(
                walletId, clientId, brokerId, brandId, baseAsset, language, balances);

            return balances;
        }

        private async Task<List<Models.AvailableBalance>> 
            CalculateBalancesForWalletBalances(string walletId, string clientId, string brokerId, List<string> assetShowOnlyWithBalance)
        {
            var data = (await _balanceService.GetBalancesByWallet(walletId)) ?? new List<WalletBalance>();

            List<Models.AvailableBalance> balances = new List<Models.AvailableBalance>();

            foreach (var walletBalance in data)
            {
                var asset = _assetsDictionaryClient.GetAssetById(new AssetIdentity()
                {
                    Symbol = walletBalance.AssetId,
                    BrokerId = brokerId
                });

                if (asset == null)
                {
                    if (walletBalance.Balance > 0)
                    {
                        _logger.LogError($"Client {clientId} has balance {walletBalance.Balance} {walletBalance.AssetId}, but asset not exist");
                    }
                    continue;
                }

                var balance = (decimal) Math.Round(walletBalance.Balance, asset.Accuracy, MidpointRounding.ToZero);
                if (balance < 0)
                    balance = 0;

                if (assetShowOnlyWithBalance.Contains(walletBalance.AssetId) && balance == 0)
                {
                    continue;
                }

                var item = new Models.AvailableBalance(walletBalance.AssetId, balance, walletBalance.LastUpdate, 0);

                balances.Add(item);
            }
            return balances;
        }

        public async Task CalculateCardReserveForWalletBalances(string walletId, string clientId, 
            string brokerId, string brandId, string baseAsset, string language, List<Models.AvailableBalance> balances)
        {
            try
            {
                var clientWithdrawalLimits = await _clientLimitsRiskService.GetClientWithdrawalLimitsAsync(
                    new GetClientWithdrawalLimitsRequest
                    {
                        BrokerId = brokerId,
                        ClientId = clientId
                    });
                
                if (clientWithdrawalLimits?.CardDepositsSummary == null)
                    return;
                
                var clientLimitsSummary = clientWithdrawalLimits.CardDepositsSummary;
                
                var clientEarn = _highYieldEngineClientOfferService
                    .GetClientOfferDtoListAsync(new GetClientOfferDtoListGrpcRequest
                {
                    ClientId = clientId,
                    BaseAsset = baseAsset,
                    Lang = language, //"en",
                    Brand = brandId,
                    BrokerId = brokerId
                });

                var earns = clientEarn.Result?.ClientOffers ?? new List<EarnOfferDto>();
                var earnBalances = earns
                    .GroupBy(e => e.Asset)
                    .ToDictionary(e => e.Key, e => e.Sum(i => i.Amount));

                var usdAccountEquity = 0m;

                foreach (var assetId in earnBalances.Keys.Where(e => balances.All(b => b.AssetId != e)))
                {
                    balances.Add(new Models.AvailableBalance(assetId, 0, DateTime.UtcNow, 0));
                }
                
                foreach (var balance in balances)
                {
                    var amount = balance.Balance;
                    if (earnBalances.TryGetValue(balance.AssetId, out var earnAmount))
                    {
                        amount += earnAmount;
                    }
                    
                    var (_, usdAmount) = _indexPricesClient.GetIndexPriceByAssetVolumeAsync(balance.AssetId, amount);
                    usdAccountEquity += usdAmount;
                }
                
                var usdReserved = clientLimitsSummary.DepositLast14DaysInUsd; //todo sub aveilable reserve to withdrawal

                var availableAmountUsd = (usdAccountEquity > usdReserved)
                    ? (usdAccountEquity - usdReserved) 
                    : 0m;
                
                foreach (var balance in balances)
                {
                    var balanceAmount = balance.Balance;
                    var balanceAsset = balance.AssetId;
                    
                    if (earnBalances.TryGetValue(balanceAsset, out var earnAmount))
                    {
                        balanceAmount += earnAmount;
                    }
                    else
                    {
                        earnAmount = 0;
                    }
                    
                    var asset = _assetsDictionaryClient.GetAssetById(new AssetIdentity()
                    {
                        Symbol = balanceAsset,
                        BrokerId = brokerId
                    });
                    
                    if (asset == null)
                    {
                        _logger.LogError(
                            "Cannot find {asset} for wallet '{walletId}' [{brokerId}|{brandId}|{clientId}]",
                            balanceAsset, walletId, brokerId, brandId,
                            clientId);
                        continue;
                    }
                    
                    if (balanceAmount > 0)
                    {
                        var (_, balanceUsd) = _indexPricesClient.GetIndexPriceByAssetVolumeAsync(balanceAsset, balanceAmount);

                        var reserve = 0m;
                        if (availableAmountUsd < balanceUsd)
                        {
                            var coef = (balanceUsd - availableAmountUsd) / balanceUsd;
                            reserve = Math.Round(balanceAmount * coef, asset.Accuracy, MidpointRounding.ToPositiveInfinity);
                        }

                        reserve -= earnAmount;
                        if (reserve < 0)
                            reserve = 0;
                        
                        if (reserve > balanceAmount)
                            reserve = balanceAmount;
                        
                        balance.CardReserve = reserve;
                    }
                    else
                    {
                        balance.CardReserve = 0m;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    "Error {message}",
                    ex.Message);
            }
        }
    }
}
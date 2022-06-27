using System;

namespace MyJetWallet.RiskManager.AvailableBalance.Models;

public class AvailableBalance
{
    public AvailableBalance()
    {
    }

    public AvailableBalance(string assetId, decimal balance, DateTime lastUpdate, decimal cardReserve)
    {
        AssetId = assetId;
        Balance = balance;
        LastUpdate = lastUpdate;
        CardReserve = cardReserve;
    }

    public string AssetId { get; set; }
    public decimal Balance { get; set; }
    public DateTime LastUpdate { get; set; }
    public decimal CardReserve { get; set; }
}
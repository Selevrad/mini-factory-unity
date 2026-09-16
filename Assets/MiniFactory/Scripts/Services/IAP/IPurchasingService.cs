using System;

namespace MiniFactory.Services.IAP
{
    public enum PurchaseFailureKind
    {
        NotInitialized,
        ProductUnavailable,
        UserCancelled,
        PaymentDeclined,
        Unknown
    }

    // Gameplay talks only to this interface - the Unity IAP API/types are
    // confined to the UnityPurchasingService implementation.
    public interface IPurchasingService
    {
        bool IsInitialized { get; }
        void Initialize();
        void BuyProduct(string productId);

        event Action Initialized;
        event Action<string> InitializeFailed;
        event Action<string> PurchaseSucceeded;
        event Action<string, PurchaseFailureKind> PurchaseFailed;
    }
}

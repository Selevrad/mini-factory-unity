using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Purchasing;

namespace MiniFactory.Services.IAP
{
    // The only file in the project allowed to reference UnityEngine.Purchasing -
    // everything else talks to IPurchasingService. Uses the non-deprecated
    // StoreController API introduced in IAP v5 (UnityIAPServices.StoreController()),
    // not the legacy IStoreListener/ConfigurationBuilder API.
    public class UnityPurchasingService : IPurchasingService
    {
        private readonly string[] _consumableProductIds;
        private readonly StoreController _storeController;
        private bool _productsReady;

        public bool IsInitialized => _productsReady && _storeController.GetConnectionState() == ConnectionState.Connected;

        public event Action Initialized;
        public event Action<string> InitializeFailed;
        public event Action<string> PurchaseSucceeded;
        public event Action<string, PurchaseFailureKind> PurchaseFailed;

        public UnityPurchasingService(string[] consumableProductIds)
        {
            _consumableProductIds = consumableProductIds;
            _storeController = UnityIAPServices.StoreController();
        }

        public async void Initialize()
        {
            _storeController.OnPurchasePending += OnPurchasePending;
            _storeController.OnPurchaseFailed += OnStorePurchaseFailed;
            _storeController.OnProductsFetched += OnProductsFetched;
            _storeController.OnProductsFetchFailed += OnProductsFetchFailed;

            try
            {
                await _storeController.Connect();
            }
            catch (Exception e)
            {
                InitializeFailed?.Invoke($"Connect failed: {e.Message}");
                return;
            }

            var definitions = _consumableProductIds
                .Select(id => new ProductDefinition(id, ProductType.Consumable))
                .ToList();
            _storeController.FetchProducts(definitions);
        }

        public void BuyProduct(string productId)
        {
            if (!IsInitialized)
            {
                PurchaseFailed?.Invoke(productId, PurchaseFailureKind.NotInitialized);
                return;
            }

            var product = _storeController.GetProductById(productId);
            if (product == null || !product.availableToPurchase)
            {
                PurchaseFailed?.Invoke(productId, PurchaseFailureKind.ProductUnavailable);
                return;
            }

            _storeController.PurchaseProduct(product);
        }

        private void OnProductsFetched(List<Product> products)
        {
            _productsReady = true;
            Initialized?.Invoke();
        }

        private void OnProductsFetchFailed(ProductFetchFailed failure)
        {
            InitializeFailed?.Invoke($"Fetch products failed: {failure.FailureReason}");
        }

        private void OnPurchasePending(PendingOrder order)
        {
            // These handlers run from inside the store's own callback chain (e.g. the
            // Fake Store dialog's button click) - an unhandled exception here would
            // propagate back into it and can leave its UI stuck (seen with Cancel
            // never closing the dialog), so every path out must be exception-safe.
            try
            {
                _storeController.ConfirmPurchase(order);
                PurchaseSucceeded?.Invoke(GetProductId(order));
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogWarning($"[MiniFactory] OnPurchasePending handler failed: {e}");
            }
        }

        private void OnStorePurchaseFailed(FailedOrder order)
        {
            try
            {
                PurchaseFailed?.Invoke(GetProductId(order), MapFailureReason(order.FailureReason));
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogWarning($"[MiniFactory] OnStorePurchaseFailed handler failed: {e}");
            }
        }

        private static string GetProductId(Order order)
        {
            var items = order?.CartOrdered?.Items();
            if (items == null || items.Count == 0) return "";
            return items[0]?.Product?.uSku ?? "";
        }

        private static PurchaseFailureKind MapFailureReason(PurchaseFailureReason reason)
        {
            switch (reason)
            {
                case PurchaseFailureReason.UserCancelled:
                case PurchaseFailureReason.OrderCancelled:
                    return PurchaseFailureKind.UserCancelled;
                case PurchaseFailureReason.PaymentDeclined:
                    return PurchaseFailureKind.PaymentDeclined;
                case PurchaseFailureReason.ProductUnavailable:
                    return PurchaseFailureKind.ProductUnavailable;
                case PurchaseFailureReason.StoreNotConnected:
                    return PurchaseFailureKind.NotInitialized;
                default:
                    return PurchaseFailureKind.Unknown;
            }
        }
    }
}

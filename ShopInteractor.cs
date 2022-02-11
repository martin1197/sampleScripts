using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Entity;
using Gateway;
using UniRx;
using UnityEngine;
using Zenject;

namespace Interactor
{
    public class ShopInteractor
    {
        private ISubject<CatalogEntity> onCatalogUpdate = new Subject<CatalogEntity>();
        public IObservable<CatalogEntity> OnCatalogUpdate => onCatalogUpdate;
        private ISubject<ProductEntity[]> onGotProducts = new Subject<ProductEntity[]>();
        public IObservable<ProductEntity[]> OnGotProducts => onGotProducts;
        private ISubject<ProductEntity[]> onSearchSuccess = new Subject<ProductEntity[]>();
        public IObservable<ProductEntity[]> OnSearchSuccess => onSearchSuccess;
        private ISubject<PromoEntity[]> onGotPromo = new Subject<PromoEntity[]>();
        public IObservable<PromoEntity[]> OnGotPromo => onGotPromo;

        [Inject] private ShopGateway shopGateway;
        [Inject] private ShopLocationInteractor shopLocationInteractor;
        [Inject] private FavouritesInteractor favouritesInteractor;
        [Inject] private LoadingInteractor loadingInteractor;

        private CategoryEntity ChosenCategory;
        private CategoryEntity SubCategory;
        private bool isSearching, isFavorite;
        private IComparer<ProductEntity> comparer = new DefaultComparer();
        private IComparer<PromoEntity> comparerPromo = new PromoComparer();

        public void GetLatestCatalog() => shopGateway.Catalog(OnGotCatalog, OnError);
        public CategoryEntity GetCurrentCategory() => ChosenCategory;
        public CategoryEntity GetSubCategory() => SubCategory;
        public void SetCurrentCategory(CategoryEntity category) => ChosenCategory = category;
        public void SetCurrentSubCategory(CategoryEntity category) => SubCategory = category;

        public void GetGoods()
        {
            isFavorite = false;
            if(SubCategory != null && !isSearching)
                shopGateway.Goods(shopLocationInteractor.ShopId(), int.Parse(SubCategory.Id), OnGotGoods, OnError);
        }

        public void GetPromo()
        {
            isFavorite = false;
            if (SubCategory != null && !isSearching)
                shopGateway.Promo(OnGotPromoData, OnError);
        }

        public void SearchForGoods(string searchString) => SearchForGoods( searchString, OnGotSearch);

        public void SearchForGoods(string searchString, Action<ProductEntity[]> action)
        {
            isFavorite = false;
            shopGateway.Search(shopLocationInteractor.ShopId(), searchString, action, OnError);
        }

        public void ActivateCustomSearch(ProductEntity[] prods)
        {
            OnGotSearch(prods);
        }

        public void SearchForFavouriteGoods(string searchString)
        {
            isFavorite = true;
            shopGateway.Search(shopLocationInteractor.ShopId(), searchString, OnGotFavouriteGoods, OnError);
        }
        public void GetFavouriteGoods()
        {
            isFavorite = true;
            if(SubCategory != null && !isSearching)
                shopGateway.Goods(shopLocationInteractor.ShopId(), 
                    int.Parse(SubCategory.Id), OnGotFavouriteGoods, OnError);
        }
        
        public void SetIsSearching(bool active) => isSearching = active;
        public bool IsSearching() => isSearching;
        public bool IsFavorite() => isFavorite;

        public void OrderItems(AccountEntity account, ProductEntity[] products) 
            => shopGateway.Order(account, products, OnOrderSuccess, OnError);

        private void OnGotCatalog(CatalogEntity catalog)
        {
            Debug.LogWarning("Got catalog! Total categories: " + catalog.Categories.Length);
            onCatalogUpdate.OnNext(catalog);
        }

        private void OnGotFavouriteGoods(ProductEntity[] products) =>
            OnGotGoods(
                (from product in products 
                    from favourite in favouritesInteractor.GetBasketProducts() 
                    where favourite.Value.Id == product.Id 
                    select product).ToArray());

        private void OnGotGoods(ProductEntity[] products)
        {
            Debug.LogWarning("Got goods! Total products: " + products.Length);
            Array.Sort(products, comparer);
            onGotProducts.OnNext(products);
        }

        private void OnGotPromoData(PromoEntity[] promos)
        {
            Debug.LogWarning("Got goods! Total products: " + promos.Length);
            Array.Sort(promos, comparerPromo);
            onGotPromo.OnNext(promos);
        }

        private void OnGotSearch(ProductEntity[] products)
        {
            Debug.LogWarning("Got search! Total products: " + products.Length);
            Array.Sort(products, comparer);
            onSearchSuccess.OnNext(products);
        }

        private void OnOrderSuccess(string result)
        {
            Debug.LogWarning("Order success!" + result);
        }

        private void OnError(string error)
        {
            Debug.LogError("Shop error. " + error);
            loadingInteractor.EndLoad();
        }

        public void SetDefaultComparer() => comparer = new DefaultComparer();
        public void SetFromAtoZComparer() => comparer = new FromAtoZComparer();
        public void SetFromZtoAComparer() => comparer = new FromZtoAComparer();
        public void SetAscendingPricesComparer() => comparer = new AscendingPricesComparer();
        public void SetDescendingPricesComparer() => comparer = new DescendingPricesComparer();
        public void SetDiscountComparer() => comparer = new DiscountComparer();
        
        class DefaultComparer : IComparer<ProductEntity>
        {
            public int Compare(ProductEntity p1, ProductEntity p2) => 0;
        }

        class PromoComparer : IComparer<PromoEntity>
        {
            public int Compare(PromoEntity p1, PromoEntity p2) => 0;            
        } 

        class DiscountComparer : IComparer<ProductEntity>
        {
            public int Compare(ProductEntity p1, ProductEntity p2)
            {
                if (p1 == null) return 0;
                return p2 != null ?
                    -(float.Parse(p1.RegPrice, CultureInfo.InvariantCulture) - float.Parse(p1.ActPrice, CultureInfo.InvariantCulture)).
                    CompareTo((float.Parse(p2.RegPrice, CultureInfo.InvariantCulture) - float.Parse(p2.ActPrice, CultureInfo.InvariantCulture))) : 0;
            }
        }

        class FromAtoZComparer : IComparer<ProductEntity>
        {
            public int Compare(ProductEntity p1, ProductEntity p2)
            {
                if (p1 == null) return 0;
                return p2 != null ? p1.Name.CompareTo(p2.Name) : 0;
            }
        }
        class FromZtoAComparer : IComparer<ProductEntity>
        {
            public int Compare(ProductEntity p1, ProductEntity p2)
            {
                if (p1 == null) return 0;
                return p2 != null ?  p2.Name.CompareTo(p1.Name) : 0;
            }
        }
        
        class AscendingPricesComparer : IComparer<ProductEntity>
        {
            public int Compare(ProductEntity p1, ProductEntity p2)
            {
                if (p1 == null) return 0;
                return p2 != null ? 
                    float.Parse(p1.ActPrice, CultureInfo.InvariantCulture).
                        CompareTo(float.Parse(p2.ActPrice, CultureInfo.InvariantCulture)) : 0;
            }
        }
        class DescendingPricesComparer : IComparer<ProductEntity>
        {
            public int Compare(ProductEntity p1, ProductEntity p2)
            {
                if (p1 == null) return 0;
                return p2 != null ? 
                    - float.Parse(p1.ActPrice, CultureInfo.InvariantCulture).
                        CompareTo(float.Parse(p2.ActPrice, CultureInfo.InvariantCulture)) : 0;
            }
        }
    }
}
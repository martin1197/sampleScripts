using System.Collections;

using Delivery.Prefabs;
using Entity;
using Interactor;
using UnityEngine;
using Zenject;
using UniRx;
using UnityEngine.UI;

namespace Delivery.Views
{
    public class Catalog : MonoBehaviour
    {
        [SerializeField] private Sprite[] categorySprites;
        [SerializeField] private GameObject categoryPrefab;
        [SerializeField] private GameObject categoryParent;        
        
        [Inject] private ShopInteractor shopInteractor;
        [Inject] private ChangeViewInteractor changeViewInteractor;
        [Inject] private NotificationInteractor notificationInteractor;
        [Inject] private LoadingInteractor loadingInteractor;

        private CatalogEntity catalog;

        void OnEnable()
        {
            StartCoroutine(AwaitCatalog());
        }
        private IEnumerator AwaitCatalog()
        {
            while (true)
            {
                yield return new WaitForSeconds(8);
                if (catalog == null) shopInteractor.GetLatestCatalog();
                else break;
            }
        }
        void Start()
        {
            loadingInteractor.StartLoad();
            shopInteractor.OnCatalogUpdate.Subscribe(UpdateCategories);
            shopInteractor.GetLatestCatalog();
        }
        
        
        private void UpdateCategories(CatalogEntity newCatalog)
        {
            catalog = newCatalog;
            DestroyOldCategories();
            CreateNewCategories();
            loadingInteractor.EndLoad();
        }

        private void CreateNewCategories()
        {
            for(var i = 0; i < catalog.Categories.Length; i++)
            {
                catalog.Categories[i].Image = categorySprites[i];
                var temp = Instantiate(categoryPrefab, categoryParent.transform, false);
                temp.GetComponent<CategoryPrefab>().catalogView = this;
                temp.GetComponent<CategoryPrefab>().SetupCategory(catalog.Categories[i], this);
            }
        }
        
        private void DestroyOldCategories()
        {
            foreach (Transform child in categoryParent.transform) {
                Destroy(child.gameObject);
            }
        }
        
        public void GoBackToMainPage()
        {
            changeViewInteractor.ChangeView(0);
        }
        
        public void UpdateLayout()
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(categoryParent.GetComponent<RectTransform>());            
        }
        
        public void GoToSubCatalog(CategoryEntity category)
        {
            if(category.Name == "Алкогольные напитки" || category.Name == "Табачная продукция")
                notificationInteractor.ShowNotification("Внимание!", "Табачные и алкогольные изделия доступны только для самовывоза.");
            shopInteractor.SetCurrentCategory(category);
            changeViewInteractor.ChangeView(8);
        }
        
        public void GoToProducts(CategoryEntity category)
        {
            shopInteractor.SetCurrentSubCategory(category);
            changeViewInteractor.ChangeView(3);
        }
    }
}
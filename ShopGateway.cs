using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BestHTTP;
using Entity;
using Interactor;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Sirenix.Utilities;
using UnityEngine;
using Zenject;

namespace Gateway
{
    public class ShopGateway
    {
        private static string ApiUrl = "https://somedomain.com/api/";
        private OrderEntity LastOrder;
        [Inject] private NotificationInteractor notificationInteractor;

        public void Catalog(Action<CatalogEntity> onSuccess, Action<string> onError)
        {
            Debug.LogWarning("--- Getting Catalog ---");

            var u = new Uri(ApiUrl + "catalog.php");
            var request = new HTTPRequest(u, HTTPMethods.Get,
                (req, res) =>
                {
                    if (res == null)
                    {
                        Debug.LogError("Connection error");
                        notificationInteractor.NoInternetNotification();
                        return;
                    }

                    if (res.DataAsText.Contains("error"))
                    {
                        onError(res.DataAsText);
                        return;
                    }
                    var categories = JsonConvert.DeserializeObject<CategoryEntity[]>(res.DataAsText);
                    var catalog = new CatalogEntity { Categories = categories };
                    onSuccess(catalog);
                });

            request.Send();
        }

        public void Goods(int shopId, int catalogId, Action<ProductEntity[]> onSuccess, Action<string> onError)
        {
            Debug.LogWarning("--- Getting Goods ---, shop id: " + shopId);


            var u = new Uri(ApiUrl + "goods.php?id_catalog=" + catalogId + "&id_shop=" + shopId);


            var request = new HTTPRequest(u, HTTPMethods.Get,
                (req, res) =>
                {
                    if (res == null)
                    {
                        notificationInteractor.NoInternetNotification();
                        onError("Connection error");
                        return;
                    }

                    if (res.DataAsText.Contains("error"))
                    {
                        onError(res.DataAsText);
                        return;
                    }

                    var products = JsonConvert.DeserializeObject<ProductEntity[]>(res.DataAsText);
                    onSuccess(products);
                });

            request.AddHeader("Content-Type", "application/json");
            request.Send();
        }

        public void Promo(Action<PromoEntity[]> onSuccess, Action<string> onError)
        {
            Debug.LogWarning("--- Getting Promo ---");

            var u = new Uri(ApiUrl + "/getpromo.php");

            var request = new HTTPRequest(u, HTTPMethods.Get,
                (req, res) =>
                {
                    if (res == null)
                    {
                        notificationInteractor.NoInternetNotification();
                        onError("Connection error");
                        return;
                    }

                    if (res.DataAsText.Contains("error"))
                    {
                        onError(res.DataAsText);
                        return;
                    }

                    var val = JsonConvert.DeserializeObject<Dictionary<string, object>>(res.DataAsText);
                    PromoDataEntity promoDataEntity = JsonConvert.DeserializeObject<PromoDataEntity>(val.Values.First().ToString());
                    onSuccess(promoDataEntity.informationsystem_item);
                });

            request.AddHeader("Content-Type", "application/json");
            request.Send();
        }

        public void Search(int shopId, string searchString, Action<ProductEntity[]> onSuccess, Action<string> onError)
        {
            Debug.LogWarning("--- Getting Goods ---");


            var u = new Uri(ApiUrl + "goods.php?id_shop=" + shopId + "&search=" + searchString);


            var request = new HTTPRequest(u, HTTPMethods.Get,
                (req, res) =>
                {
                    if (res == null)
                    {
                        notificationInteractor.NoInternetNotification();
                        onError("Connection error");
                        return;
                    }

                    if (res.DataAsText.Contains("error"))
                    {
                        onError(res.DataAsText);
                        return;
                    }

                    var products = JsonConvert.DeserializeObject<ProductEntity[]>(res.DataAsText);
                    onSuccess(products);
                });

            request.AddHeader("Content-Type", "application/json");
            request.Send();
        }

        public void Order(AccountEntity account, ProductEntity[] products, Action<string> onSuccess = null, Action<string> onError = null)
        {
            Debug.LogWarning("--- Ordering ---");


            var u = new Uri(ApiUrl + "order.php");


            var request = new HTTPRequest(u, HTTPMethods.Post,
                (req, res) =>
                {
                    if (res == null)
                    {
                        notificationInteractor.NoInternetNotification();
                        onError?.Invoke("Connection error");
                        return;
                    }

                    var response = JsonConvert.DeserializeObject<Dictionary<string, string>>(res.DataAsText);

                    if (response.ContainsKey("error"))
                    {
                        onError?.Invoke(response["error"]);
                        return;
                    }

                    if (response.ContainsKey("id_order"))
                    {
                        var result = response["id_order"];
                        foreach (var item in response)
                        {
                            Debug.Log($"key: {item.Key}  value: {item.Value}");
                        }
                        onSuccess?.Invoke(result);
                    }
                });

            request.AddHeader("Connection", "keep -alive");
            request.AddHeader("Content-Type", "application/json;charset=UTF-8");
            request.AddHeader("sec-ch-ua-mobile", "?0");
            request.AddHeader("sec-ch-ua-platform", "macOs");
            request.AddHeader("Origin", "https://linia-market.ru");
            request.AddHeader("Sec-Fetch-Site", "same-origin");
            request.AddHeader("Sec-Fetch-Mode", "cors");
            request.AddHeader("Sec-Fetch-Dest", "empty");
            request.AddHeader("Referer", "https://linia-market.ru/");

            var data = CreateOrderDataString(account, products);
            Debug.Log($"{data}");
            request.RawData = Encoding.UTF8.GetBytes(data);
            request.Send();
        }

        private void PrintResponseKeys(Dictionary<string, string> response)
        {
            foreach (var pair in response)
                Debug.Log("Response. Key: " + pair.Key + ", Value: " + pair.Value);
        }
        public void Order(AccountEntity account, Basket basket, Action<string> onSuccess = null, Action<string> onError = null)
        {
            Debug.LogWarning("--- Ordering ---");


            var u = new Uri(ApiUrl + "order.php");


            var request = new HTTPRequest(u, HTTPMethods.Post,
                (req, res) =>
                {
                    if (res == null)
                    {
                        notificationInteractor.NoInternetNotification();
                        onError?.Invoke("Connection error");
                        return;
                    }

                    var response = JsonConvert.DeserializeObject<Dictionary<string, string>>(res.DataAsText);

                    if (response.ContainsKey("error"))
                    {
                        onError?.Invoke(response["error"]);
                        return;
                    }

                    if (response.ContainsKey("id_order"))
                    {
                        var result = response["id_order"];
                        foreach (var item in response)
                        {
                            Debug.Log($"key: {item.Key}  value: {item.Value}");
                        }
                        LastOrder.id = result;
                        onSuccess?.Invoke(result);
                    }
                    foreach (var item in response)
                    {
                        Debug.Log(item);
                    }
                });

            request.AddHeader("Connection", "keep -alive");
            request.AddHeader("Content-Type", "application/json;charset=UTF-8");
            request.AddHeader("sec-ch-ua-mobile", "?0");
            request.AddHeader("sec-ch-ua-platform", "macOs");
            request.AddHeader("Origin", "https://linia-market.ru");
            request.AddHeader("Sec-Fetch-Site", "same-origin");
            request.AddHeader("Sec-Fetch-Mode", "cors");
            request.AddHeader("Sec-Fetch-Dest", "empty");
            request.AddHeader("Referer", "https://somedomain.com/");

            var data = CreateOrderDataString(account, basket);
            request.RawData = Encoding.UTF8.GetBytes(data);
            request.Send();
        }
         private string CreateOrderDataString(AccountEntity account, Basket basket)
        {
            var order = GenerateOrder(account, basket);
            LastOrder = order;
            var orderAsText = JsonConvert.SerializeObject(order);
            Debug.Log("Example order: " + orderAsText);
            return orderAsText;
        }

        private OrderEntity GenerateOrder(AccountEntity account, Basket _basket)
        {
            var retVal = new OrderEntity
            {
                id = account.Id,
                sessid = account.SessId,
                id_shop = account.ShopId,
                basket = _basket,
            };

            var dateToFill = DateTime.Parse(retVal.basket.orderDeliveryDate);
            retVal.basket.orderDeliveryDate = dateToFill.Day + "/" + dateToFill.Month + "/" + dateToFill.Year;
            retVal.basket.orderType = _basket.deliveryAvail ? "0" : "1";
            _basket.hasCard = account.Card.IsNullOrWhitespace();
            return retVal;
        }

        [CanBeNull] public OrderEntity GetLastOrder() => LastOrder;
    }
}
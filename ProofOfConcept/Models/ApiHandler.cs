﻿using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using ProofOfConcept.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web;

namespace ProofOfConcept.Models
{
    public static class ApiHandler
    {
        public static IConfigurationRoot Configuration { get; set; }
        private static readonly HttpClient Client = new HttpClient();

        /// <summary>
        /// Constructor to access variables in appsettings.json
        /// </summary>
        static ApiHandler()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json");

            Configuration = builder.Build();
        }

  
        /// <summary>
        /// Returns Json for Index view
        /// </summary>
        public static IndexVM[] GetAllProducts()
        {
            var responseString = string.Empty;

            try
            {
                var token = RequestTokenAsync().Result;
                responseString = GetAllProductsFromOneBrandCallApi(token).Result;
            }
            catch (Exception exception)
            {
                responseString = exception.Message;
                return new IndexVM[0];
            }

            return JObject.Parse(responseString)["data"].ToObject<IndexVM[]>();
        }

        /// <summary>
        /// Returns Json for Brand View
        /// </summary>
        public static BrandVM[] GetProductsDetailsBrands()
        {
            var responseString = string.Empty;

            try
            {
                var token = RequestTokenAsync().Result;
                responseString = GetDetailedProductsViewBrands(token).Result;
            }
            catch (Exception exception)
            {
                responseString = exception.Message;
                return new BrandVM[0];
            }

            return JObject.Parse(responseString)["data"].ToObject<BrandVM[]>();
        }

        /// <summary>
        /// Returns Json for Brands details view (all products from one brand)
        /// </summary>
        public static BrandVM[] GetProductsDetailsBrands(string id)
        {
            var responseString = string.Empty;
            try
            {
                var token = RequestTokenAsync().Result;
                responseString = GetAllProductsFromBrand(id, token).Result;
            }
            catch (Exception exception)
            {
                responseString = exception.Message;
                return new BrandVM[0];
            }

            return JObject.Parse(responseString)["data"].ToObject<BrandVM[]>();
        }

        /// <summary>
        /// Returns Json for Detail View
        /// </summary>
        public static Tuple<DetailsVM, string> GetProductsDetails(string id)
        {
            var responseString = string.Empty;
            string token;

            try
            {
                token = RequestTokenAsync().Result;
                responseString = GetOneProductFromApi(id, token).Result;
            }
            catch (Exception exception)
            {
                responseString = exception.Message;
                return Tuple.Create(new DetailsVM(), responseString);
            }

            var detailedView = JObject.Parse(responseString)["data"].ToObject<DetailsVM>();
            return Tuple.Create(detailedView, token);
        }

        /// <summary>
        /// Gets a token with the clientId and client secret
        /// </summary>
        private static async Task<string> RequestTokenAsync()
        {
            var tokenUrl = Configuration["baseUrls:tokenUrl"];
            var clientId = Configuration["clientInfo:clientId"];
            var clientSecret = Configuration["clientInfo:clientSecret"];

            var request = new HttpRequestMessage(HttpMethod.Post, tokenUrl);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/x-www-form-urlencoded"));

            request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "client_id", clientId },
                { "client_secret", clientSecret },
                { "scope", "search_api search_api_downloadbinary" },
                { "grant_type", "client_credentials" }
            });

            var response = await Client.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var payload = JObject.Parse(await response.Content.ReadAsStringAsync());
            return payload.Value<string>("access_token");
        }

        /// <summary>
        /// Redirects the user to login
        /// </summary>
        public static Tuple<string, string, string, string, string> RedirectAndDownload(string id, string productId, string fileId)
        {
            var clientId2 = Configuration["clientInfo:clientId2"];
            var redirectUri = Configuration["baseUrls:redirectUri"];
            var authUrl = Configuration["baseUrls:authUrl"];
            //var url = $"{authUrl}?client_id={clientId2}&response_type=code&redirect_uri={redirectUri}&scope=search_api search_api_downloadbinary offline_access&state={productId}_{fileId}";

            // url is divided like this because Javascript are unable to parse & properly.
            var url1 = $"{authUrl}?client_id={clientId2}";
            var url2 = $"response_type=code";
            var url3 = $"redirect_uri={redirectUri}";
            var url4 = $"scope=search_api search_api_downloadbinary offline_access";
            var url5 = $"state ={productId}_{fileId}";

            return Tuple.Create(url1, url2, url3, url4, url5);
        }

        /// <summary>
        /// Api call to get details on one product 
        /// </summary>
        private static async Task<string> GetAllProductsFromOneBrandCallApi(string token)
        {
            var indexViewUrl = Configuration["baseUrls:indexViewUrl"];

            var request = new HttpRequestMessage(HttpMethod.Get, indexViewUrl);

            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/x-www-form-urlencoded"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await Client.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var payload = await response.Content.ReadAsStringAsync();

            return payload;
        }

        /// <summary>
        /// Api call to get details on all products 
        /// </summary>
        private static async Task<string> GetOneProductFromApi(string id, string code)
        {
            var detailViewUrl = Configuration["baseUrls:detailViewUrl"];

            var request = new HttpRequestMessage(HttpMethod.Get, detailViewUrl + id);

            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/x-www-form-urlencoded"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", code);

            var response = await Client.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var payload = await response.Content.ReadAsStringAsync();

            return payload;
        }

        /// <summary>
        /// Api call to get all products from brand
        /// </summary>
        private static async Task<string> GetAllProductsFromBrand(string id, string code)
        {
            var brandProductsUrl = Configuration["baseUrls:brandProductsUrl"];

            var request = new HttpRequestMessage(HttpMethod.Get, brandProductsUrl + id);

            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/x-www-form-urlencoded"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", code);

            var response = await Client.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var payload = await response.Content.ReadAsStringAsync();

            return payload;
        }

        /// <summary>
        /// Api call to get details on all products for brand view
        /// </summary>
        private static async Task<string> GetDetailedProductsViewBrands(string token)
        {
            var brandUrl = Configuration["baseUrls:brandUrl"];

            var request = new HttpRequestMessage(HttpMethod.Get, brandUrl);

            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/x-www-form-urlencoded"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await Client.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var payload = await response.Content.ReadAsStringAsync();

            return payload;
        }
 
        /// <summary>
        /// Api call to get download binary
        /// </summary>
        public static async Task<HttpResponseMessage> GetDownloadRequest(string productId, string fileId, string token)
        {
            var binaryUrl = Configuration["baseUrls:binaryUrl"];
            var fullBinaryUrl = binaryUrl + productId + "/files/" + fileId + "/binary";
            var request = new HttpRequestMessage(HttpMethod.Get, fullBinaryUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            return await Client.SendAsync(request);        
        }
        
        /// <summary>
        /// Gets a refresh token
        /// </summary>
        public static async Task<string> ReAuthorize(string refreshToken)
        {
            var tokenUrl = Configuration["baseUrls:tokenUrl"];
            var clientId2 = Configuration["clientInfo:clientId2"];
            var clientSecret2 = Configuration["clientInfo:clientSecret2"];
            var request = new HttpRequestMessage(HttpMethod.Post, tokenUrl);

            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/x-www-form-urlencoded"));

            request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "client_id", clientId2 },
                { "client_secret", clientSecret2 },
                { "grant_type", "authorization_code" },
                { "refresh_token", refreshToken },
            });

            var response = await Client.SendAsync(request);
            response.EnsureSuccessStatusCode();

            // object (access_token, token_type, expires_in, refresh_token) store in cookie
            var payload = await response.Content.ReadAsStringAsync();
            return payload;
        }
    }
}

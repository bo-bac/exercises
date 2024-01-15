﻿using Goa.Ui.Mvc.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using System.Diagnostics;

namespace Goa.Ui.Mvc.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        public HomeController(
            ILogger<HomeController> logger,
            IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        [AllowAnonymous]
        public IActionResult Index() => View();

        [Authorize] //not obvious cuz Authenticated user requered policy is ON by default see: program.cs 
        public async Task<IActionResult> Privacy()
        {
            ViewBag.IdToken = await HttpContext.GetTokenAsync("id_token");
            ViewBag.AccessToken = await HttpContext.GetTokenAsync("access_token");

            return View();
        }

        public async Task<IActionResult> CallApi()
        {
            var token = await HttpContext.GetTokenAsync("id_token");

            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, "WeatherForecasts/classified")
            {
                Headers =
                {
                    { HeaderNames.Authorization, "Bearer "+ token}
                }
            };

            var client = _httpClientFactory.CreateClient("GoaApi");

            try
            {
                var response = await client.SendAsync(httpRequestMessage);
                var json = await response.Content.ReadAsStringAsync();
                dynamic parsedJson = JsonConvert.DeserializeObject(json) ?? string.Empty;
                ViewBag.ApiResponse = JsonConvert.SerializeObject(parsedJson, Formatting.Indented);
            }
            catch (Exception e)
            {
                ViewBag.ApiResponse = e.Message;
            }

            return View("CallApi");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
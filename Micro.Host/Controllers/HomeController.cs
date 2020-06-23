using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Micro.Host.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Micro.Host.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ServiceHost _serviceHost;

        public HomeController(ILogger<HomeController> logger, ServiceHost serviceHost)
        {
            _logger = logger;
            _serviceHost = serviceHost;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel {RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier});
        }

        public async Task<IActionResult> Start()
        {
            if (!_serviceHost.Running)
            {
                await _serviceHost.Start();
            }
            return View("Index");
        }
        public async Task<IActionResult> Stop()
        {
            if (_serviceHost.Running)
            {
                await _serviceHost.Stop();
            }
            return View("Index");
        }
    }
}
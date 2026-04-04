using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ReQuest.Models;

namespace ReQuest.Controllers;

public class HomeController : Controller
{
    private readonly IConfiguration _configuration;

    public HomeController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Auth()
    {
        ViewData["BackendApiBaseUrl"] = _configuration["BackendApi:BaseUrl"] ?? "http://localhost:5134";
        return View();
    }

    public IActionResult Cabinet()
    {
        return View();
    }

    public IActionResult History()
    {
        return View();
    }

    public IActionResult Game()
    {
        ViewData["BackendApiBaseUrl"] = _configuration["BackendApi:BaseUrl"] ?? "http://localhost:5134";
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
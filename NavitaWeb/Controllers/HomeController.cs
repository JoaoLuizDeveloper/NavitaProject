﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NavitaWeb.Models;
using NavitaWeb.Models.ViewModel;
using NavitaWeb.Repository.IRepository;

namespace NavitaWeb.Controllers
{
    public class HomeController : Controller
    {
        #region Construtor/Injection
        private readonly ILogger<HomeController> _logger;
        private readonly IMarcaRepository _npRepo;
        private readonly IAccountRepository _AccountRepo;
        private readonly IPatrimonioRepository _npTrail;

        public HomeController(ILogger<HomeController> logger, IMarcaRepository npRepo, IPatrimonioRepository npTrail, IAccountRepository accountRepository)
        {
            _logger = logger;
            _npRepo = npRepo;
            _npTrail = npTrail;
            _AccountRepo = accountRepository;
        }
        #endregion

        #region Index
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            IndexVM model = new IndexVM()
            {
                MarcaList = await _npRepo.GetAllAsync(SD.MarcaAPIPath, HttpContext.Session.GetString("JWToken")),
                PatrimonioList = await _npTrail.GetAllAsync(SD.PatrimonioAPIPath, HttpContext.Session.GetString("JWToken"))
            };
            return View(model);
        }
        #endregion

        #region Login
        [HttpGet]
        public IActionResult Login()
        {
            User obj = new User();
            return View(obj);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(User model)
        {
            User objUser = await _AccountRepo.LoginAsync(SD.AccountAPIPath + "authenticate/", model);

            if(objUser.Token == null)
            {
                return View();
            }

            var identity = new ClaimsIdentity(CookieAuthenticationDefaults.AuthenticationScheme);
            identity.AddClaim(new Claim(ClaimTypes.Name, objUser.UserName));
            identity.AddClaim(new Claim(ClaimTypes.Role, objUser.Role));
            var principal = new ClaimsPrincipal(identity);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            HttpContext.Session.SetString("JWToken", objUser.Token);
            TempData["alert"] = "Welcome " + objUser.UserName;

            return RedirectToAction("Index");
        }
        #endregion

        #region Registro
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(User model)
        {
            bool result = await _AccountRepo.RegisterAsyn(SD.AccountAPIPath + "register/", model);

            if (result == false)
            {
                return View();
            }
            TempData["alert"] = "Registeration Successful ";
            //+ objUser.UserName
            return RedirectToAction("Login");
        }
        #endregion

        #region LogOut, Acesso Negado e Erro
        public async Task<IActionResult> LogOut()
        {
            await HttpContext.SignOutAsync();
            HttpContext.Session.SetString("JWToken", "");

            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
        #endregion
    }
}
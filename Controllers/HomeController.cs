﻿using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;
using System.Diagnostics;
using TippingProject.Models;
using System.Data.SqlClient;
using System.Text.RegularExpressions;
using System.Reflection.PortableExecutable;
using System.Web;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using System.Net;
using Newtonsoft.Json;

namespace TippingProject.Controllers
{
    public class HomeController : Controller
    {
        
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }
        public IActionResult AddedPage()
        {
            return View();
        }
       

        [HttpPost]
        public IActionResult HandleInput(LoginCredentials details)
        {



            string? password = null;
            if (Regex.IsMatch(details.name,$"[a-zA-Z0-9]"))
            {
                if (details.name.Length > 15)
                {
                    return Content("too long");
                }
                else
                {
                    using (SqlConnection conn = new SqlConnection("Data Source=(LocalDB)\\MSSQLLocalDB;AttachDbFilename=C:\\Users\\borod\\OneDrive\\Documents\\Databases\\User_Details.mdf;Integrated Security=True;Connect Timeout=30"))
                    {
                        conn.Open();
                        using (SqlCommand command = new SqlCommand("SELECT password FROM User_Details WHERE username = @Username", conn))
                        {
                            command.Parameters.AddWithValue("@Username", details.name);
                            using (SqlDataReader reader = command.ExecuteReader())
                            {
                                if (reader.Read()) {
                                    string? tempVal = reader["password"].ToString();
                                    if (tempVal == null)
                                    {
                                        return Content("username or password invalid");
                                    }
                                    else
                                    {
                                        password = tempVal;
                                    }
                                }
                            }
                        }
                        conn.Close();
                    }
                }
            }
            else
            {
                return Content("character invalid");
            }


            if (password == details.password)
            {


                IPAddress? ipAddress = HttpContext.Connection.RemoteIpAddress;
                if (ipAddress == null)
                {
                    return Content("no ip");
                }
                Dictionary<string, string> data = new Dictionary<string, string>();
                data.Add("IpAddress", ipAddress.ToString());

                int randNum = RandomNumberGenerator.GetInt32(int.MaxValue);
                data.Add("AuthID", randNum.ToString());


                string userName = details.name;
                data.Add("Username",userName);

                string expiryDate = DateTime.Now.AddHours(1).ToString();
                data.Add("ExpTime", expiryDate);

                CookieOptions cookieOptions = CreateCookieOptions();
                Response.Cookies.Append("AuthCookie",JsonConvert.SerializeObject(data), cookieOptions);

                
                
            }
            return Redirect(nameof(LoggedOnPage));
        }
        public IActionResult LoggedOnPage()
        {
            return View();
        }
        private CookieOptions CreateCookieOptions()
        {
            CookieOptions cookie = new CookieOptions();
            cookie.HttpOnly = false;
            cookie.Secure = false;
            cookie.Expires = DateTime.Now.AddHours(1);
            cookie.IsEssential = true;
            return cookie;
        }
        public IActionResult CheckForCookies()
        {
            string? cookie = Request.Cookies["AuthCookie"];
            if (cookie == null)
            {
                return Redirect("AddedPage");
            }
            else
            {
                return Redirect("LoggedOnPage");
            }
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
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
﻿using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using TippingProject.Models;
using System.Data.SqlClient;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using System.Net;
using Newtonsoft.Json;

namespace TippingProject.Controllers
{
    public class HomeController : Controller
    {
        const string connectionString = "Data Source=(LocalDB)\\MSSQLLocalDB;AttachDbFilename=\"C:\\Users\\borod\\OneDrive\\Documents\\Asp.Net Database\\Uset_Authentication_Database.mdf\";Integrated Security=True;Connect Timeout=30";


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
                    using (SqlConnection conn = new SqlConnection(connectionString))
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
                DateTime expDate = DateTime.Now.AddHours(1);
                IPAddress? ipAddress = HttpContext.Connection.RemoteIpAddress;
                if (ipAddress == null)
                {
                    return Content("no ip");
                }
                Dictionary<string, string> data = new Dictionary<string, string>();
                data.Add("IpAddress", ipAddress.ToString());

                int randNum = RandomNumberGenerator.GetInt32(int.MaxValue);

                string userName = details.name;
                data.Add("Username",userName);

                string expiryDate = expDate.ToString();
                data.Add("ExpTime", expiryDate);
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    using (SqlCommand command = new SqlCommand("SELECT * FROM User_Auth_Data WHERE Username == @username", conn))
                    {
                        //check if user already authenticated
                        command.Parameters.AddWithValue("@username", details.name);
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                if (reader != null)
                                {
                                    return Content("user already authenticated");
                                }
                            }
                            
                        }

                        //check if a duplicate key exists without the expired time, if date is expired, then it is overwritten

                        while (true)
                        {
                            command.CommandText = $"SELECT expDate FROM User_Auth_Data WHERE username == {randNum}";
                            using (SqlDataReader reader = command.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    if (reader == null)
                                    {
                                        break;
                                    }
                                    DateTime authExpiryDate;
                                    DateTime.TryParse(reader["ExpDate"].ToString(), out authExpiryDate);
                                    if (authExpiryDate < DateTime.Now)
                                    {
                                        break;
                                    }
                                    randNum = RandomNumberGenerator.GetInt32(int.MaxValue);

                                }
                            }
                        }
                        data["AuthID"] = randNum.ToString();
                        command.CommandText = "INSERT INTO User_Auth_Data(AuthID, IpAddress, Username, ExpDate) VALUES(@auth,@ip,@username,@expTime)";
                        command.Parameters.AddWithValue("@ip", data["IpAddress"]);
                        command.Parameters.AddWithValue("@auth", data["AuthID"]);
                        command.Parameters.AddWithValue("@username", data["userName"]);
                        command.Parameters.AddWithValue("@expTime", data["ExpTime"]);
                        command.ExecuteNonQuery();
                        Response.Cookies.Append("AuthCookie", JsonConvert.SerializeObject(data), CreateCookieOptions(expDate));
                    }
                }

            }
            return Redirect(nameof(LoggedOnPage));
        }
        public IActionResult LoggedOnPage()
        {

            string? cookie = Request.Cookies["AuthCookie"];
            if (cookie == null)
            {
                return Redirect(nameof(CheckForCookies));
            }
            int firstIndex = cookie.IndexOf('{');
            int lastIndex = cookie.LastIndexOf('}');
            string data = cookie.Substring(firstIndex,lastIndex - firstIndex+1);

            Dictionary<string, string> deserializedData= JsonConvert.DeserializeObject<Dictionary<string, string>>(data);
            data.ElementAt(1);
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    using (SqlCommand command = new SqlCommand("SELECT * FROM User_Auth_Data WHERE AuthId == @ID",conn))
                    {
                    command.Parameters.AddWithValue("@ID", deserializedData["AuthID"]);
                    }

                }


        }
        private CookieOptions CreateCookieOptions(DateTime DateTimeToSet)
        {
            CookieOptions cookie = new CookieOptions();
            cookie.HttpOnly = false;
            cookie.Secure = false;
            cookie.Expires = DateTimeToSet;
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
using Microsoft.AspNetCore.Mvc;
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
        const string ConnectionString = "Data Source=(LocalDB)\\MSSQLLocalDB;AttachDbFilename=\"C:\\USERS\\BOROD\\ONEDRIVE\\DOCUMENTS\\DATABASES\\USER_DETAILS.MDF\"; Integrated Security=True;Connect Timeout=30";
        const string AuthDbConnectionString = "Data Source=(LocalDB)\\MSSQLLocalDB;AttachDbFilename=\"C:\\Users\\borod\\OneDrive\\Documents\\Asp.Net Database\\User_Authentication_Database.mdf\";Integrated Security=True;Connect Timeout=30";
        const string AuthenticatedUserColumnInsertionQuery = "INSERT INTO User_Auth_Data(AuthID, IpAddress, Username, ExpDate) VALUES(@auth,@ip,@username,@expTime)";
        const string AuthenticationValidationSubString = "SELECT * FROM User_Auth_Data WHERE AuthId = @ID";

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
                    using (SqlConnection conn = new SqlConnection(ConnectionString))
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
                                        reader.Close();
                                        return Content("username or password invalid");
                                    }
                                    else
                                    {
                                        password = tempVal;
                                    }
                                    reader.Close();
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
                using (SqlConnection conn = new SqlConnection(AuthDbConnectionString))
                {
                    using (SqlCommand command = new SqlCommand("SELECT * FROM User_Auth_Data WHERE Username = @username", conn))
                    {
                        conn.Open();
                        //check if user already authenticated
                        command.Parameters.AddWithValue("@username", details.name);
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                if (reader != null)
                                {
                                    conn.Close();
                                    reader.Close();
                                    return Content("user already authenticated");
                                }
                            }

                        }

                        //check if a duplicate key exists without the expired time, if date is expired, then it is overwritten

                        while (true)
                        {
                            command.CommandText = $"SELECT expDate FROM User_Auth_Data WHERE AuthID = {randNum}";
                            using (SqlDataReader reader = command.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    if (reader == null)
                                    {
                                        reader.Close();
                                        break;
                                    }
                                    DateTime authExpiryDate;
                                    DateTime.TryParse(reader["ExpDate"].ToString(), out authExpiryDate);
                                    if (authExpiryDate < DateTime.Now)
                                    {

                                        reader.Close();
                                        break;
                                    }

                                    reader.Close();
                                    randNum = RandomNumberGenerator.GetInt32(int.MaxValue);

                                }
                                else
                                {

                                    reader.Close();
                                    break;
                                }
                            }
                        }
                    }
                    using (SqlCommand command = new SqlCommand("INSERT INTO User_Auth_Data(AuthID, IpAddress, Username, ExpDate) VALUES(@auth,@ip,@username,@expTime)",conn))
                    {
                        data["AuthID"] = randNum.ToString();
                        command.CommandText = AuthenticatedUserColumnInsertionQuery;
                        command.Parameters.AddWithValue("@ip", data["IpAddress"]);
                        command.Parameters.AddWithValue("@auth", data["AuthID"]);
                        command.Parameters.AddWithValue("@username", data["Username"]);
                        command.Parameters.AddWithValue("@expTime", data["ExpTime"]);
                        command.ExecuteNonQuery();
                        conn.Close();
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
                using (SqlConnection conn = new SqlConnection(AuthDbConnectionString))
                {
                conn.Open();
                using (SqlCommand command = new SqlCommand(AuthenticationValidationSubString,conn))
                    {
                        command.Parameters.AddWithValue("@ID", deserializedData["AuthID"]);
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                if (reader == null)
                                {
                                return Content("no data");
                                }
                                if (deserializedData["AuthID"] == reader[3].ToString())
                                {
                                    if (deserializedData["userName"] == reader[2].ToString())
                                    {
                                        if (deserializedData["IpAddress"] == reader[1].ToString() && HttpContext.Connection.RemoteIpAddress.ToString() == reader[1].ToString())
                                        {
                                        conn.Close();
                                        return View();
                                        }
                                    }
                                }
                            }
                        }
                    }
                conn.Close();
                }
            return Redirect(nameof(AddedPage));
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
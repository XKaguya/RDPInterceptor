using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Linq;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using RDPInterceptor.API.Controllers;

namespace RDPInterceptor.API
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.Cookie.HttpOnly = true;
                    options.ExpireTimeSpan = TimeSpan.FromMinutes(10);
                    options.LoginPath = "/Process/LoginIn";
                    options.LogoutPath = "/Process/LoginOut";
                    options.SlidingExpiration = true;
                });
            services.AddAuthorization();
            services.AddRouting();
            services.AddControllers()
                .AddApplicationPart(typeof(ProcessController).Assembly);
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            app.Run(async (context) =>
            {
                var assembly = Assembly.GetExecutingAssembly();
                string resourceName;
                if (context.Request.Path.Value.EndsWith("/Login"))
                {
                    resourceName = "RDPInterceptor.Web.Login.html";
                }
                else if (context.Request.Path.Value.EndsWith("/Data"))
                {
                    resourceName = "RDPInterceptor.Web.Data.html";
                }
                else
                {
                    resourceName = "RDPInterceptor.Web.Login.html";
                }

                using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                using (StreamReader reader = new StreamReader(stream))
                {
                    string result = reader.ReadToEnd();

                    context.Response.ContentType = "text/html";

                    await context.Response.WriteAsync(result);
                }
            });

            Logger.Log("Web service has been started.");
        }
    }
}

namespace RDPInterceptor.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ProcessController : ControllerBase
    {
        [Authorize]
        [HttpPost("StartCapture")]
        public async Task<IActionResult> StartCapture()
        {
            try
            {
                await NetworkInterceptor.StartCapture();
                
                return Ok("Called success.");
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message + ex.StackTrace);
                throw;
            }
        }

        [Authorize]
        [HttpPost("StopCapture")]
        public async Task<IActionResult> StopCapture()
        {
            try
            {
                await NetworkInterceptor.StopCapture();
                
                return Ok("Called success.");
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message + ex.StackTrace);
                throw;
            }
        }
        
        [Authorize]
        [HttpPost("DeleteIpAddr")]
        public IActionResult DeleteIpAddr()
        {
            try
            {
                using (StreamReader reader = new StreamReader(Request.Body, Encoding.UTF8))
                {  
                    string ipAddr = reader.ReadToEndAsync().Result;
                    IPAddress IpAddr = null;
                    if (IPAddress.TryParse(ipAddr, out IpAddr))
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            NetworkInterceptor.IpAddrList.Remove(IpAddr);
                        });
                    }
                    else
                    {
                        return Ok("Failed.");
                    }
                }

                return Ok("Called success.");
            }
            catch (Exception e)
            {
                Logger.Error(e.Message + e.StackTrace);
                throw;
            }
        }
        
        [Authorize]
        [HttpPost("AddIpAddr")]
        public IActionResult PostNewIp()
        {
            try
            {
                using (StreamReader reader = new StreamReader(Request.Body, Encoding.UTF8))
                {  
                    string ipAddr = reader.ReadToEndAsync().Result;
                    IPAddress IpAddr = null;
                    if (IPAddress.TryParse(ipAddr, out IpAddr))
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            NetworkInterceptor.IpAddrList.Add(IpAddr);
                        });
                    }
                    else
                    {
                        return Ok("Failed.");
                    }
                }

                return Ok("Called success.");
            }
            catch (Exception e)
            {
                Logger.Error(e.Message + e.StackTrace);
                throw;
            }
        }

        [Authorize]
        [HttpGet("GetLog")]
        public IActionResult GetLog()
        {
            return Ok(Logger.GetLogs());
        }
        
        [Authorize]
        [HttpGet("GetIpAddrList")]
        public IActionResult GetIpAddrList()
        {
            try
            {
                var ipAddrList = NetworkInterceptor.IpAddrList;
                
                var ipAddrStrList = ipAddrList.Select(ip => ip.ToString()).ToList();
        
                return Ok(ipAddrStrList);
            }
            catch (Exception e)
            {
                Logger.Error(e.Message + e.StackTrace);
                throw;
            }
        }
        
        public class AuthInfo
        {
            public string Username { get; set; }
            public string Password { get; set; }
        }
        
        [AllowAnonymous]
        [HttpPost("LoginOut")]
        public async Task<IActionResult> LoginOut()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Ok();
        }
        
        [AllowAnonymous]
        [HttpPost("LoginIn")]
        public async Task<IActionResult> LoginIn([FromBody] AuthInfo authInfo)
        {
            string filePath = "auth.xml";
            string defaultUsername = "admin";
            string defaultPassword = "8c6976e5b5410415bde908bd4dee15dfb167a9c873fc4bb8a81f6f2ab448a918";

            if (!System.IO.File.Exists(filePath))
            {
                XDocument doc = new XDocument(
                    new XElement("Auth",
                        new XElement("Username", defaultUsername),
                        new XElement("Password", defaultPassword)
                    )
                );
                doc.Save(filePath);
            }
    
            XDocument authDoc = XDocument.Load(filePath);
            string username = authDoc.Root.Element("Username").Value;
            string password = authDoc.Root.Element("Password").Value;
            
            if (authInfo.Username == username && authInfo.Password == password)
            {
                try
                {
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, authInfo.Username)
                    };

                    var claimsIdentity = new ClaimsIdentity(
                        claims, CookieAuthenticationDefaults.AuthenticationScheme);

                    var authProperties = new AuthenticationProperties();

                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme, 
                        new ClaimsPrincipal(claimsIdentity), 
                        authProperties);
                }
                catch (Exception e)
                {
                    Logger.Error(e.Message + e.StackTrace);
                    throw;
                }

                Logger.Log("Login success.");
                
                return Ok();
            }
            else
            {
                return Unauthorized();
            }
        }
        
        [Authorize]
        [HttpPost("ChangePasswd")]
        public IActionResult ChangePasswd([FromBody] AuthInfo authInfo)
        {
            Logger.Log("Called changepwd");
            
            string filePath = "auth.xml";

            if (!System.IO.File.Exists(filePath))
            {
                return NotFound("The auth file does not exist.");   
            }
            
            XDocument authDoc = XDocument.Load(filePath);
            string username = authDoc.Root.Element("Username").Value;
            
            Logger.Log(username);
            Logger.Log(authInfo.Username);
        
            if (authInfo.Username != username)
            {
                Logger.Log("Username not fit.");
                return Unauthorized();
            }
        
            authDoc.Root.Element("Password").Value = authInfo.Password;
            authDoc.Save(filePath);
            
            return Unauthorized("Please login again.");
        }
    }
}

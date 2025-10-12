using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MY_PORTFOLIO.Models;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Net.Http;
using System.Collections.Generic;

// सुनिश्चित करें कि MimeKit, MailKit, SendGrid के सारे पुराने using statements हटा दिए गए हैं।

namespace MY_PORTFOLIO.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IConfiguration _config;

        public HomeController(ILogger<HomeController> logger, IConfiguration config)
        {
            _logger = logger;
            _config = config;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SendMessage(ContactForm model)
        {
            if (!ModelState.IsValid)
                return View("Index", model);

            // 1. Environment Variables से Values पढ़ें
            var apiKey = _config["ElasticEmail:ApiKey"];
            var fromEmail = _config["EmailSettings:FromEmail"]; 
            var adminEmail = fromEmail; // Admin और Sender ID एक ही होगी

            if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(fromEmail))
            {
                _logger.LogError("❌ Elastic Email configuration missing. Check API Key/From Email on Render.");
                TempData["ErrorMessage"] = "❌ Email service is not fully configured.";
                return RedirectToAction("Index");
            }

            // Elastic Email API URL (v2.5)
            var requestUrl = "https://api.elasticemail.com/v2/email/send";
            
            try
            {
                using (var client = new HttpClient())
                {
                    // 1. Admin को ईमेल भेजें (Client Message)
                    var adminData = new Dictionary<string, string>
                    {
                        {"apikey", apiKey},
                        {"from", fromEmail},
                        {"fromName", "Syed Gufran Kazmi"},
                        {"to", adminEmail},
                        {"subject", $"New message from {model.Name}"},
                        {"bodyHtml", $"Name: {model.Name}<br>Email: {model.Email}<br>Subject: {model.Subject}<br><br>Message:<br>{model.Message}"}
                    };
                    var responseAdmin = await client.PostAsync(requestUrl, new FormUrlEncodedContent(adminData));

                    // 2. User को ऑटो-रिप्लाई भेजें
                    var autoReplyData = new Dictionary<string, string>
                    {
                        {"apikey", apiKey},
                        {"from", fromEmail},
                        {"fromName", "Syed Gufran Kazmi"},
                        {"to", model.Email},
                        {"subject", "Thanks for contacting me!"},
                        {"bodyHtml", $"Hello {model.Name},<br><br>Thanks for reaching out! I've received your message and will get back to you soon.<br><br>Best regards,<br>Syed Gufran Kazmi"}
                    };
                    var responseReply = await client.PostAsync(requestUrl, new FormUrlEncodedContent(autoReplyData));

                    if (responseAdmin.IsSuccessStatusCode && responseReply.IsSuccessStatusCode)
                    {
                        _logger.LogInformation("✅ Emails sent successfully via Elastic Email API!");
                        TempData["SuccessMessage"] = "✅ Your message has been sent successfully!";
                    }
                    else
                    {
                        // Error Response हैंडल करें
                        var errorBody = await responseAdmin.Content.ReadAsStringAsync();
                        _logger.LogError($"❌ Elastic Email API call failed. Status: {responseAdmin.StatusCode}. Response: {errorBody}");
                        TempData["ErrorMessage"] = "❌ Something went wrong while sending your message.";
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Elastic Email sending failed.");
                TempData["ErrorMessage"] = "❌ Something went wrong. Please try again later.";
            }

            return RedirectToAction("Index");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}

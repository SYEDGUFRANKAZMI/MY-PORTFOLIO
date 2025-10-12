
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MY_PORTFOLIO.Models;
using System.Diagnostics;
using System.Threading.Tasks;

using SendGrid;
using SendGrid.Helpers.Mail; 


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

      // --- नया HomeController.cs ---

// --- MY_PORTFOLIO.Controllers/HomeController.cs में SendMessage Method ---

[HttpPost]
public async Task<IActionResult> SendMessage(ContactForm model)
{
    if (!ModelState.IsValid)
        return View("Index", model);

    // Render Environment Variables से सीधे API Key और Email पढ़ें
    // 'SendGrid:ApiKey' को Environment Variable में 'SendGrid__ApiKey' के रूप में सेट किया जाना चाहिए।
    // 'EmailSettings:FromEmail' को Environment Variable में 'EmailSettings__FromEmail' के रूप में सेट किया जाना चाहिए।
    var sendGridKey = _config["SendGrid:ApiKey"];
    var senderEmail = _config["EmailSettings:FromEmail"];

    if (string.IsNullOrEmpty(sendGridKey) || string.IsNullOrEmpty(senderEmail))
    {
        _logger.LogError("❌ Email configuration missing. Check environment variables (SendGrid__ApiKey, EmailSettings__FromEmail) on Render.");
        TempData["ErrorMessage"] = "❌ Email service is not configured properly.";
        return RedirectToAction("Index");
    }

    var client = new SendGridClient(sendGridKey);
    var fromAddress = new EmailAddress(senderEmail, "Syed Gufran Kazmi");
    var toAddress = new EmailAddress(senderEmail, "Syed Gufran Kazmi (Admin)");

    try
    {
        // 1. Admin को ईमेल भेजें
        var subjectToMe = $"New message from {model.Name}";
        var contentToMe = $"Name: {model.Name}<br>Email: {model.Email}<br>Subject: {model.Subject}<br><br>Message:<br>{model.Message}";
        var msgToMe = MailHelper.CreateSingleEmail(fromAddress, toAddress, subjectToMe, null, contentToMe);

        // 2. User को ऑटो-रिप्लाई भेजें
        var subjectAutoReply = "Thanks for contacting me!";
        var contentAutoReply = $"Hello {model.Name},<br><br>Thanks for reaching out! I've received your message and will get back to you soon.<br><br>Best regards,<br>Syed Gufran Kazmi";
        var msgAutoReply = MailHelper.CreateSingleEmail(fromAddress, new EmailAddress(model.Email, model.Name), subjectAutoReply, null, contentAutoReply);

        // दोनों ईमेल SendGrid API के माध्यम से भेजें
        var responseToMe = await client.SendEmailAsync(msgToMe);
        var responseAutoReply = await client.SendEmailAsync(msgAutoReply);

        if (responseToMe.IsSuccessStatusCode && responseAutoReply.IsSuccessStatusCode)
        {
            _logger.LogInformation("✅ Email sent successfully via SendGrid API!");
            TempData["SuccessMessage"] = "✅ Your message has been sent successfully!";
        }
        else
        {
            // SendGrid API से Error body को लॉग करें
            var errorBody = await responseToMe.Body.ReadAsStringAsync();
            _logger.LogError($"❌ SendGrid API call failed. Status: {responseToMe.StatusCode}. Response: {errorBody}");
            TempData["ErrorMessage"] = "❌ Something went wrong while sending your message. Please try again later.";
        }
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Email sending failed: {Message}", ex.Message);
        TempData["ErrorMessage"] = "❌ Something went wrong. Please try again later.";
    }

    return RedirectToAction("Index");
}
// --- SendMessage Method समाप्त ---

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}

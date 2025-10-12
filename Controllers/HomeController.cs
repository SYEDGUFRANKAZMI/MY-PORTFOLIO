using MimeKit;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Configuration;
using MY_PORTFOLIO.Models;
using Microsoft.AspNetCore.Mvc;
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

[HttpPost]
public async Task<IActionResult> SendMessage(ContactForm model)
{
    if (!ModelState.IsValid)
        return View("Index", model);

    // Render Environment Variable से API Key और Sender Email पढ़ें
    var sendGridKey = _config["SendGrid:ApiKey"];
    var senderEmail = _config["EmailSettings:FromEmail"]; // या 'SendGrid:SenderEmail'

    if (string.IsNullOrEmpty(sendGridKey) || string.IsNullOrEmpty(senderEmail))
    {
        _logger.LogError("❌ Email configuration missing. Check environment variables (SendGrid:ApiKey, EmailSettings:FromEmail) on Render.");
        TempData["ErrorMessage"] = "❌ Email service is not configured properly.";
        return RedirectToAction("Index");
    }

    var client = new SendGridClient(sendGridKey);
    var fromAddress = new EmailAddress(senderEmail, "Syed Gufran Kazmi");
    var toAddress = new EmailAddress(senderEmail, "Syed Gufran Kazmi (Admin)");

    // 1. Email to yourself (Admin)
    var subjectToMe = $"New message from {model.Name}";
    var contentToMe = $"Name: {model.Name}<br>Email: {model.Email}<br>Subject: {model.Subject}<br><br>Message:<br>{model.Message}";
    var msgToMe = MailHelper.CreateSingleEmail(fromAddress, toAddress, subjectToMe, null, contentToMe);

    // 2. Auto-reply email to user
    var subjectAutoReply = "Thanks for contacting me!";
    var contentAutoReply = $"Hello {model.Name},<br><br>Thanks for reaching out! I've received your message and will get back to you soon.<br><br>Best regards,<br>Syed Gufran Kazmi";
    var msgAutoReply = MailHelper.CreateSingleEmail(fromAddress, new EmailAddress(model.Email, model.Name), subjectAutoReply, null, contentAutoReply);

    try
    {
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
            _logger.LogError($"SendGrid API failed. Admin status: {responseToMe.StatusCode}, Reply status: {responseAutoReply.StatusCode}");
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
// --- नया HomeController.cs समाप्त ---

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}

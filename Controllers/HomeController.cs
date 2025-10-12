using MimeKit;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Configuration;
using MY_PORTFOLIO.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Threading.Tasks;

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

            // Read credentials from environment or fallback to config
            var fromEmail = Environment.GetEnvironmentVariable("EMAIL_FROM");
            var fromPassword = Environment.GetEnvironmentVariable("EMAIL_PASSWORD");

            if (string.IsNullOrEmpty(fromEmail))
                fromEmail = _config["EmailSettings:FromEmail"];
            if (string.IsNullOrEmpty(fromPassword))
                fromPassword = _config["EmailSettings:Password"];

            if (string.IsNullOrEmpty(fromEmail) || string.IsNullOrEmpty(fromPassword))
            {
                _logger.LogError("❌ Email configuration missing. Check environment variables on Render.");
                TempData["ErrorMessage"] = "❌ Email service is not configured properly.";
                return RedirectToAction("Index");
            }

            // Email to yourself
            var messageToMe = new MimeMessage();
            messageToMe.From.Add(new MailboxAddress("Portfolio Contact Form", fromEmail));
            messageToMe.To.Add(new MailboxAddress("Syed Gufran Kazmi", fromEmail));
            messageToMe.Subject = $"New message from {model.Name}";
            messageToMe.Body = new TextPart("plain")
            {
                Text = $"Name: {model.Name}\nEmail: {model.Email}\nSubject: {model.Subject}\n\nMessage:\n{model.Message}"
            };

            // Auto-reply email to user
            var autoReply = new MimeMessage();
            autoReply.From.Add(new MailboxAddress("Syed Gufran Kazmi", fromEmail));
            autoReply.To.Add(new MailboxAddress(model.Name, model.Email));
            autoReply.Subject = "Thanks for contacting me!";
            autoReply.Body = new TextPart("plain")
            {
                Text = $"Hello {model.Name},\n\nThanks for reaching out! I've received your message and will get back to you soon.\n\nBest regards,\nSyed Gufran Kazmi"
            };

            try
            {
                using (var client = new SmtpClient())
                {
                    client.ServerCertificateValidationCallback = (s, c, h, e) => true;

                    _logger.LogInformation("Connecting to SendGrid SMTP...");
                    await client.ConnectAsync("smtp.sendgrid.net", 587, MailKit.Security.SecureSocketOptions.StartTls);

                    _logger.LogInformation("Authenticating with SendGrid...");
                    await client.AuthenticateAsync("apikey", fromPassword); // username always "apikey"

                    _logger.LogInformation("Sending admin email...");
                    await client.SendAsync(messageToMe);

                    _logger.LogInformation("Sending auto-reply email...");
                    await client.SendAsync(autoReply);

                    await client.DisconnectAsync(true);
                    _logger.LogInformation("✅ Email sent successfully via SendGrid!");
                }

                TempData["SuccessMessage"] = "✅ Your message has been sent successfully!";
            }
            catch (MailKit.Security.AuthenticationException authEx)
            {
                _logger.LogError(authEx, "Authentication failed — check your SendGrid API key!");
                TempData["ErrorMessage"] = "❌ Email authentication failed. Please contact the administrator.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Email sending failed: {Message}", ex.Message);
                TempData["ErrorMessage"] = "❌ Something went wrong while sending your message. Please try again later.";
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

using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace PicPool.Infrastructure.Services
{
    public class ServeiNotificacions
    {

        private readonly IConfiguration _configuration;

        public ServeiNotificacions(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public async Task EnviarCorreuAsync(string correuElectronic, string assumpte, string textCorreu)
        {
            var smtpHost = _configuration["Email:SmtpHost"];
            var smtpPortText = _configuration["Email:SmtpPort"];
            var smtpUser = _configuration["Email:SmtpUser"];
            var smtpPassword = _configuration["Email:SmtpPassword"];
            var emailFrom = _configuration["Email:From"];

            if (string.IsNullOrWhiteSpace(smtpHost))
                throw new InvalidOperationException("Falta la configuració Email:SmtpHost.");

            if (string.IsNullOrWhiteSpace(smtpPortText))
                throw new InvalidOperationException("Falta la configuració Email:SmtpPort.");

            if (!int.TryParse(smtpPortText, out var smtpPort))
                throw new InvalidOperationException("Email:SmtpPort no és un número vàlid.");

            if (string.IsNullOrWhiteSpace(smtpUser))
                throw new InvalidOperationException("Falta la configuració Email:SmtpUser.");

            if (string.IsNullOrWhiteSpace(smtpPassword))
                throw new InvalidOperationException("Falta la configuració Email:SmtpPassword.");

            if (string.IsNullOrWhiteSpace(emailFrom))
                throw new InvalidOperationException("Falta la configuració Email:From.");

            using var client = new SmtpClient(smtpHost, smtpPort)
            {
                Credentials = new NetworkCredential(smtpUser, smtpPassword),
                EnableSsl = true
            };

            using var missatge = new MailMessage
            {
                From = new MailAddress(emailFrom, "PicPool"),
                Subject = assumpte,
                Body = textCorreu,
                IsBodyHtml = false
            };

            missatge.To.Add(correuElectronic);

            await client.SendMailAsync(missatge);
        }
    }
}

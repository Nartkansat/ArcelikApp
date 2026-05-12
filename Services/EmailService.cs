using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace ArcelikApp.Services
{
    public static class EmailService
    {
        // TODO: GMAIL VEYA KENDI SMTP SUNUCU BILGILERINIZI BURAYA GIRINIZ
        private const string SmtpHost = "smtp.gmail.com";
        private const int SmtpPort = 587;
        private const string SmtpUsername = "synccasecompany@gmail.com";
        // Gmail kullanıyorsanız buraya normal şifrenizi değil, "Uygulama Şifresi" (App Password) girmelisiniz.
        private const string SmtpPassword = "ulbl wyit hpiy qyzo"; 

        public static async Task<bool> SendPasswordResetCodeAsync(string toEmail, string code)
        {
            try
            {
                using var client = new SmtpClient(SmtpHost, SmtpPort)
                {
                    Credentials = new NetworkCredential(SmtpUsername, SmtpPassword),
                    EnableSsl = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(SmtpUsername, "Synctool Güvenlik"),
                    Subject = "Synctool - Şifre Sıfırlama Kodu",
                    Body = $@"
                        <div style='font-family: Arial, sans-serif; color: #333; padding: 20px;'>
                            <h2 style='color: #1C2B4A;'>Synctool Şifre Sıfırlama Talebi</h2>
                            <p>Merhaba,</p>
                            <p>Hesabınız için şifre sıfırlama talebinde bulunulmuştur. İşleminize devam edebilmek için aşağıdaki güvenlik kodunu kullanınız:</p>
                            <div style='background-color: #F4F6F8; padding: 15px; border-radius: 5px; text-align: center; font-size: 24px; font-weight: bold; letter-spacing: 5px; color: #E02020; margin: 20px 0;'>
                                {code}
                            </div>
                            <p>Bu kod <strong>3 dakika</strong> boyunca geçerlidir.</p>
                            <p style='font-size: 12px; color: #777;'>Eğer bu işlemi siz yapmadıysanız lütfen bu e-postayı dikkate almayınız.</p>
                        </div>
                    ",
                    IsBodyHtml = true
                };
                mailMessage.To.Add(toEmail);

                await client.SendMailAsync(mailMessage);
                return true;
            }
            catch (Exception ex)
            {
                // Hata loglanabilir: Debug.WriteLine(ex.Message);
                return false;
            }
        }
    }
}

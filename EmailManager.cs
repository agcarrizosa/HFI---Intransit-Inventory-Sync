using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace Intransit_Inventory_Sync
{
    class EmailManager
    {
        private String SmtpServer = "";

        public EmailManager(String SmtpServer)
        {
            this.SmtpServer = SmtpServer;
        }

        public int SendEmail(String ToAddress, String FromAddress, String Subject, String Body, ref String ErrorMessage)
        {
            int tempValue;
            tempValue = System.Net.ServicePointManager.MaxServicePointIdleTime;
            try
            {
                MailMessage MiCorreo = new MailMessage();
                MiCorreo.From = new MailAddress(FromAddress, "Internal transfer Manager");
                MiCorreo.To.Add(ToAddress);
                MiCorreo.Subject = Subject;
                MiCorreo.Body = Body;
                MiCorreo.BodyEncoding = System.Text.Encoding.UTF8;
                MiCorreo.Priority = System.Net.Mail.MailPriority.Normal;
                MiCorreo.IsBodyHtml = true;
                System.Net.ServicePointManager.MaxServicePointIdleTime = 1;
                SmtpClient MiSmtp = new SmtpClient();
                MiSmtp.Host = this.SmtpServer;
                MiSmtp.Send(MiCorreo);
                System.Net.ServicePointManager.MaxServicePointIdleTime = tempValue;
                return 1;
            }
            catch (Exception e)
            {
                ErrorMessage = e.Message;
                return 0;
            }
            finally
            {
                System.Net.ServicePointManager.MaxServicePointIdleTime = tempValue;
            }


        }
    }
}

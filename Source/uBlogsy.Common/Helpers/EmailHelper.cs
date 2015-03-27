namespace uBlogsy.Common.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Mail;
    using System.Text;
    using System.Threading;

    using umbraco.BusinessLogic;

    public class EmailHelper
    {

        public static bool Send(string emailBody, string subject, string senderAddress, string recipientAddresses, Dictionary<string, string> dictionary, bool asych)
        {
            var recipients = recipientAddresses.Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim());

            string body = Detokenize(emailBody, dictionary);

            try
            {
                var smtp = new SmtpClient();

                // send to all recipients
                foreach (var r in recipients)
                {
                    // create message and send
                    var m = new MailMessage(new MailAddress(senderAddress), new MailAddress(r))
                                        {
                                            Subject = subject,
                                            IsBodyHtml = true,
                                            Body = body
                                        };
                    if (asych)
                    {
                        SendAsync(smtp, m);
                    }
                    else
                    {
                        smtp.Send(m);
                    }
                }
            }
            catch (Exception ex)
            {
                string log = string.Format("Sender {0}. Message", senderAddress, emailBody);

                // log exception here!
                Log.Add(LogTypes.Error, -1, ex.Message + log);
                return false;
            }

            return true;
        }


        /// <summary>
        /// Sends email in new thread.
        /// </summary>
        /// <param name="smtp"></param>
        /// <param name="message"></param>
        private static void SendAsync(SmtpClient smtp, MailMessage message)
        {
            Thread newThread = new Thread(SendAsync);
            newThread.Start(new { smtp, message });
        }



        /// <summary>
        /// Used for parameterized threading
        /// </summary>
        /// <param name="o"></param>
        private static void SendAsync(object o)
        {
            try
            {
                SmtpClient smtp = ((dynamic)o).smtp;
                MailMessage message = ((dynamic)o).message;
                smtp.Send(message);
            }
            catch (Exception ex)
            {
                // log exception here!
                Log.Add(LogTypes.Error, -1, ex.Message);
            }
        }



        /// <summary>
        /// Replaces tokens in emailBody.
        /// </summary>
        /// <param name="emailBody"></param>
        /// <param name="dictionary"></param>
        /// <returns></returns>
        private static string Detokenize(string emailBody, Dictionary<string, string> dictionary)
        {
            string s = emailBody;
            foreach (var key in dictionary.Keys)
            {
                s = s.Replace(key, dictionary[key]);
            }
            return s;
        }





        /// <summary>
        /// Sends email to a comma delimited list of recipients
        /// </summary>
        /// <param name="formName"></param>
        /// <param name="senderAddress"></param>
        /// <param name="recipientAddresses"></param>
        /// <param name="dictionary"></param>
        public static void Send(string formName, string senderAddress, string recipientAddresses, Dictionary<string, string> dictionary)
        {
            // parse recipients
            string[] recipients = recipientAddresses.Split(",".ToCharArray());

            // get values
            string emailBody = GetValuesForEmail(dictionary);

            // create smtp
            SmtpClient smtp = new SmtpClient();

            foreach (var r in recipients)
            {
                // create message and send
                MailMessage m = new MailMessage(new MailAddress(senderAddress), new MailAddress(r))
                                    {
                                        Subject = formName,
                                        Body = emailBody
                                    };
                smtp.Send(m);
            }
        }




        /// <summary>
        /// Returns the dictionary as a string.
        /// Dictionary items are "\n" delimited.
        /// Key-value pairs are ":" delimited. 
        /// </summary>
        /// <param name="dictionary"></param>
        /// <returns></returns>
        protected static string GetValuesForEmail(Dictionary<string, string> dictionary)
        {
            // get key value pairs
            StringBuilder s = new StringBuilder();
            foreach (var key in dictionary.Keys)
            {
                s.Append(key);
                s.Append(": ");
                s.Append(dictionary[key].Trim());
                s.Append(Environment.NewLine);
            }

            return s.ToString();
        }

    }
}
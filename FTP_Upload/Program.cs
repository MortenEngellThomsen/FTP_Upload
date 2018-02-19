using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace FTP_Upload
{
    class Program
    {
        
        static int Main(string[] args)
        {
            // file / host / port / user / pass / mail
            if (args.Length == 6)
            {
                try
                {
                    string source = args[0];
                    string filename = Path.GetFileName(source);
                    string destination = "";
                    string host = args[1];
                    string username = args[3];
                    string password = args[4];
                    int port = Int32.Parse(args[2]);

                    Console.WriteLine($"Trying upload of file: {source}");
                    UploadSFTPFile(host, username, password, source, destination, port, filename, args[5]);
                }
                catch (Exception e)
                {
                    RunErrorMailSend(e.Message, args[5], args[0]);
                    Console.WriteLine($"Something went wrong with FTP upload: {e.Message}");
                    return -1;
                }
            }
            else
            {
                Console.WriteLine("Critical!: Not enough arguments at runtime!");
                return -1;
            }
            return 0;
        }


        private static void RunErrorMailSend(string message, string mail, string source)
        {
            try
            {
                string mailMsg = $"Some0thing went wrong with the FTP upload of {source}: {message}";
                var mailMessage = new MailMessage("FTP-Upload@balk.dk", mail, $"Fejl i FTP overførsel: {source}", mailMsg);

                var smtpClient = new SmtpClient("exchange.balk.dk");
                smtpClient.Send(mailMessage);
                mailMessage.Dispose();
                smtpClient.Dispose();
            }
            catch
            {
                Console.WriteLine("Error!: Couldn't send error mail to recipient!");
            }
        }

        public static void UploadSFTPFile(string host, string username,
                                          string password, string sourcefile, string destinationpath, int port, string fileName, string mail)
        {
            RemoteCertificateValidationCallback orgCallback = ServicePointManager.ServerCertificateValidationCallback;
            try
            {
                // This statement is to ignore certification validation warning
                ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(OnValidateCertificate);
                ServicePointManager.Expect100Continue = true;


                // Connect to the server and do what ever you want here



                FtpWebRequest request = (FtpWebRequest)WebRequest.Create(host + @"/" + fileName);
                request.EnableSsl = true;
                request.Method = WebRequestMethods.Ftp.UploadFile;
                request.Credentials = new NetworkCredential(username, password);



                Stream requestStream = request.GetRequestStream();
                FileStream fileStream = File.Open(sourcefile, FileMode.Open);
                byte[] buffer = new byte[1024];
                int bytesRead;
                while (true)
                {
                    bytesRead = fileStream.Read(buffer, 0, buffer.Length);
                    if (bytesRead == 0)
                        break;
                    requestStream.Write(buffer, 0, bytesRead);
                }
                //The request stream must be closed before getting
                //the response.
                requestStream.Close();

                FtpWebResponse response = (FtpWebResponse)request.GetResponse();

                if (response.StatusCode != FtpStatusCode.ClosingData)
                {
                    RunErrorMailSend(response.StatusDescription, mail, sourcefile);
                }
                Console.WriteLine($"Server responded: {response.StatusDescription}");
                bool OnValidateCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
                {
                    return true;
                }
            }
            finally
            {
                ServicePointManager.ServerCertificateValidationCallback = orgCallback;
            }
        }
        
    }
}

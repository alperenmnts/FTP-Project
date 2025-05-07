using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;

namespace DigitialSignage_System.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        private static bool isPaused = false;

        [HttpPost]
        public ActionResult Index(HttpPostedFileBase[] postedFiles)
        {
            // FTP Server URL
            string ftp = "ftp://ftp.testimage.your-site.com/";
            string ftpFolder = "b/";
            string ftpUserName = "testimage";
            string ftpPassword = "example";

            List<string> successfulFiles = new List<string>();

            foreach (var postedFile in postedFiles)
            {
                if (postedFile != null && postedFile.ContentLength > 0)
                {
                    string fileName = Path.GetFileName(postedFile.FileName);
                    try
                    {
                        // FTP yükleme isteği oluşturma
                        FtpWebRequest request = (FtpWebRequest)WebRequest.Create(ftp + ftpFolder + fileName);
                        request.Method = WebRequestMethods.Ftp.UploadFile;

                        // FTP kimlik bilgilerini ayarlama
                        request.Credentials = new NetworkCredential(ftpUserName, ftpPassword);
                        request.UsePassive = true; // Pasif modda çalış
                        request.UseBinary = true; // İkili dosya transferi (resimler, dosyalar vs. için)
                        request.EnableSsl = false; // SSL bağlantısı kullanma

                        int bufferSize = 10240; // 10 KB burada istediğiniz gibi ayarlayabilirsiniz
                        byte[] buffer = new byte[bufferSize];
                        int bytesRead = 0;
                        long totalBytesSent = 0;

                        // Dosya akışını açma
                        using (Stream fileStream = postedFile.InputStream)
                        {
                            fileStream.Seek(0, SeekOrigin.Begin); // Dosya okuma işlemine başlangıçtan itibaren başla
                            request.ContentLength = fileStream.Length; // FTP isteğine gönderilecek dosyanın boyutu

                            using (Stream requestStream = request.GetRequestStream())
                            {
                                while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) > 0)
                                {
                                    // Eğer işlem durdurulmuşsa, işlem devam edene kadar bekleme
                                    while (isPaused)
                                    {
                                        System.Threading.Thread.Sleep(100);
                                    }

                                    // Okunan veriyi FTP'ye gönderme
                                    requestStream.Write(buffer, 0, bytesRead);
                                    totalBytesSent += bytesRead;

                                    // Yüzde ilerlemesini hesaplama
                                    double progress = (double)totalBytesSent / fileStream.Length * 100;
                                 
                                }
                            }
                        }

                        // FTP yanıtını al ve kapat
                        using (FtpWebResponse response = (FtpWebResponse)request.GetResponse())
                        {

                            // Başarı durumu kontrolü
                            if (response.StatusCode == FtpStatusCode.ClosingData)
                            {
                                // Başarılı dosyayı listeye ekle
                                successfulFiles.Add(fileName);
                            }
                        }
                    }
                    catch (WebException ex)
                    {
                        throw new Exception((ex.Response as FtpWebResponse).StatusDescription);
                    }
                }
            }
            // başarılı dosyaları viewbage aktarma
            if (successfulFiles.Count > 0)
            {
                ViewBag.Message = $"{successfulFiles.Count} dosya başarıyla yüklendi: {string.Join(", ", successfulFiles)}";
            }
            else
            {
                ViewBag.Message = "Hiçbir dosya yüklenemedi.";
            }
            return View();
        }




        // durdurma işlemi
        [HttpPost]
        public ActionResult PauseUpload()
        {
            isPaused = true;
            return Json(new { success = true });
        }

        // devam ettirme işlemi
        [HttpPost]
        public ActionResult ResumeUpload()
        {
            isPaused = false;
            return Json(new { success = true });
        }


    }
}
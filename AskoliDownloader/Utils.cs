using NLog;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Xml.Serialization;
using HtmlAgilityPack;

namespace AskoliDownloader
{
    public static class Utils
    {
        #region NLOG

        private const string ErrorLog = "ErrorLog";
        private const string MyCustomLog = "MyCustomLog";

        public static void WriteStatusLog(string msg)
        {
            Console.WriteLine(msg);
            LogManager.GetLogger(ErrorLog).Debug(msg);
        }

        public static void WriteErrorLog(string msg)
        {
            if (msg.IsNullOrEmpty())
            {
                LogManager.GetLogger(ErrorLog).Debug("Empty Message");
            }

            LogManager.GetLogger(ErrorLog).Debug(msg);
        }

        public static void WriteErrorLog(Exception error, string additionalMsg = "")
        {
            var addMsg = string.Empty;

            if (error.InnerException != null)
                error = error.InnerException;

            if (additionalMsg.IsNotNullOrEmpty())
                addMsg = $@"Additional Information: {additionalMsg}";

            var msg =
                $@"=================== Exception ==================
                    Exception message:
                    {error.Message}
                    Exception stack trace:
                    {error.StackTrace}
                    {addMsg}
                    -----------------------------------------------";

            LogManager.GetLogger(ErrorLog).Debug(msg);
        }

        public static void WriteMyCustomLog(string body, string title = "Test")
        {
            var msg = $@"{body}";
            LogManager.GetLogger(MyCustomLog).Debug(msg);
            msg = @"===========================================";
            LogManager.GetLogger(MyCustomLog).Debug(msg);
        }

        #endregion NLOG

        #region Serializer

        public static string Serialize<T>(this T toSerialize)
        {
            if (toSerialize == null) return null;

            try
            {
                var xmlSerializer = new XmlSerializer(toSerialize.GetType());

                using (var textWriter = new StringWriter())
                {
                    xmlSerializer.Serialize(textWriter, toSerialize);
                    return textWriter.ToString().Replace("utf-16", "utf-8");    // didn't find quick and better solution
                }
            }
            catch (Exception ex)
            {
                WriteErrorLog(ex, "Cann't Serialize Item");
                return null;
            }
        }

        public static T Deserialize<T>(this string toDeserialize)
        {
            try
            {
                var xmlSerializer = new XmlSerializer(typeof(T));
                var textReader = new StringReader(toDeserialize);
                return (T)xmlSerializer.Deserialize(textReader);
            }
            catch (Exception ex)
            {
                WriteErrorLog(ex, "Cann't Deserialize Item");
                return default(T);
            }
        }

        #endregion Serializer

        #region Misc

        public static void CreateUrlShortcut(string directory, string linkName,  string linkUrl)
        {
            try
            {
                using (var writer = new StreamWriter(directory + "\\" + linkName + ".url"))
                {
                    writer.WriteLine("[InternetShortcut]");
                    writer.WriteLine("URL=" + linkUrl);
                    writer.Flush();
                }
            }
            catch (Exception ex)
            {
                WriteErrorLog(ex);
            }
        }

        public static bool IsValidUrl(string source)
        {
            Uri uriResult;
            return Uri.TryCreate(source, UriKind.Absolute, out uriResult) && uriResult.Scheme == Uri.UriSchemeHttp;
        }

        public static string CleanFileName(string fileName, string replaceWith = "_")
        {
            return Path.GetInvalidFileNameChars().Aggregate(fileName, (current, c) => current.Replace(c.ToString(), replaceWith));
        }

        public static HtmlNode GetHtml(string url)
        {
            if (url.IsNullOrEmpty()) return null;

            var html = new HtmlDocument();
            html.LoadHtml(new WebClient().DownloadString(url));
            html.DocumentNode.InnerHtml = html.DocumentNode.InnerHtml.EncodingUtf8();
            return html.DocumentNode;
        }

        public static string GetResponseStream(string url)
        {
            if (url.IsNullOrEmpty()) return null;

            var objRequest = (HttpWebRequest)WebRequest.Create(url);
            var objResponse = (HttpWebResponse)objRequest.GetResponse();

            var responseStream = new StreamReader(objResponse.GetResponseStream());
            var responseRead = responseStream.ReadToEnd();

            responseStream.Close();
            responseStream.Dispose();

            return responseRead;
        }
        #endregion
    }
}
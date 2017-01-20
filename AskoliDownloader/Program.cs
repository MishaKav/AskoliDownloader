using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using HtmlAgilityPack;
using Newtonsoft.Json;

namespace AskoliDownloader
{
    public static class Program
    {
        private static ProgressBar _progress;
        private const string TestLink = "http://www.askoli.co.il/%D7%A7%D7%95%D7%A8%D7%A1-%D7%9C%D7%99%D7%A0%D7%A7%D7%93%D7%90%D7%99%D7%9F-Linkedin-%D7%9C%D7%9E%D7%97%D7%A4%D7%A9%D7%99-%D7%A2%D7%91%D7%95%D7%93%D7%94";
        private static readonly string WorkingPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

        public static void Main(string[] args)
        {
            Console.Title = "Askoli Downloader by Misha Kav";
            Console.OutputEncoding = new UTF8Encoding();  // unicode the console
            string url;

            if (args.Length > 0 && args[0].IsNotNullOrEmpty())
            {
                url = args[0];
                Console.WriteLine($"Your urls is: {url}");
            }
            else
            {
                Console.WriteLine("Please enter valid url of course, from Askoli site.");
                url = Console.ReadLine(); // Get string from user
            }
            if (Utils.IsValidUrl(url)) // Check string
            {
                var course = GetCourseInfoByUrl(url);
                DownloadCourse(course);
                _progress.Dispose();
            }
            else
            {
                Utils.WriteStatusLog("Bad Url");
            }
            Console.ReadKey();
        }

        private static Course GetCourseInfoByUrl(string url)
        {
            if (url.IsNullOrEmpty()) return null;

            Utils.WriteStatusLog("Start download html of course");
            var htmlDoc = new HtmlWeb().Load(url);
            var root = htmlDoc.DocumentNode;
            var sections = root.SelectNodes("//*[contains(@class,'chapters_list_item')]").ToList().DistinctBy(s => s.SelectSingleNode("h2").InnerText).ToList();
            var courseName = sections[0].SelectSingleNode("h2").InnerText;
            Utils.WriteStatusLog($"Course Name: {courseName}");
            Utils.WriteStatusLog($"Sections: {sections.Count}");
            var course = new Course { Title = courseName, Url = url, SectionList = new List<Section>() };

            foreach (var sectionHtml in sections)
            {
                var sectionTitle = sectionHtml.SelectSingleNode("h2").InnerText;
                var section = new Section { Title = sectionTitle , ChapterList = new List<ChapterItem>()};
                var chapterInSection = sectionHtml.SelectSingleNode("div[contains(@class, 'subChapters_wrapper')]").SelectNodes("div").ToList();
                Utils.WriteStatusLog($"Section: {sectionTitle}");

                foreach (var chapterHtml in chapterInSection)
                {
                    var dataId = chapterHtml.Descendants("span").First(s => s.Attributes.Contains("data-id")).Attributes["data-id"].Value;
                    Utils.WriteStatusLog($"SectionId: {dataId}");

                    if (dataId.IsNotNullOrEmpty())
                    {
                        var chapter = GetVideoChapterFromUrl($"http://www.askoli.co.il/player?chapterID={dataId}");
                        if (chapter != null)
                        {
                            section.ChapterList.Add(chapter);
                        }
                    }
                }

                course.SectionList.Add(section);
            }

            return course;
        }

        private static ChapterItem GetVideoChapterFromUrl(string url)
        {
            if (url.IsNullOrEmpty()) return null;

            Utils.WriteStatusLog($"Loading Chapter by url: {url}");
            var htmlDoc = new HtmlWeb().Load(url);
            var allHtml = htmlDoc.DocumentNode.InnerHtml;

            var startIndex = allHtml.IndexOf("window.askoli", StringComparison.Ordinal);
            var endIndex = allHtml.IndexOf("};", StringComparison.Ordinal);
            var jsonString = allHtml.Substring(startIndex, endIndex - startIndex + 1).Replace("window.askoli = ", string.Empty);

            var ascoliObject = JsonConvert.DeserializeObject<AscoliObject>(jsonString);
            Utils.WriteStatusLog("Try to get Json object from html");
            if (ascoliObject.IsNotEmptyObject())
            {
                var chapter = ascoliObject.chapters.items[0];
                return chapter;
            }
            return null;
        }

        private static void DownloadCourse(Course course)
        {
            if (course == null || course.Title.IsNullOrEmpty() || !course.SectionList.IsAny())
            {
                Utils.WriteStatusLog("Didn't has course to download.");
                return;
            }

            var currentDir = $"{WorkingPath}/{Utils.CleanFileName(course.Title)}";
            var coursePath = $"{currentDir}/";

            if (!Directory.Exists(currentDir))
            {
                Directory.CreateDirectory(currentDir);
                Utils.WriteStatusLog($"Create Folder for Cource: {course.Title.PrintHebrew()}\n");
                File.WriteAllText($"{currentDir}/Course.json", JsonConvert.SerializeObject(course));
            }

            foreach (var section in course.SectionList)
            {
                var sectionIndex = course.SectionList.IndexOf(section) + 1;

                currentDir = $"{coursePath}{sectionIndex} {Utils.CleanFileName(section.Title)}";
                if (!Directory.Exists(currentDir))
                {
                    Directory.CreateDirectory(currentDir);
                    Utils.WriteStatusLog($"Create Section: {section.Title.PrintHebrew()}\n");
                }

                foreach (var chapter in section.ChapterList)
                {
                    var index = section.ChapterList.IndexOf(chapter) + 1;
                    var videoName = $"{currentDir}/{index} {Utils.CleanFileName(chapter.videoname)}.{Path.GetExtension(chapter.movieurl)}";
                    DownloadFileWithProgress(chapter.movieurl, videoName);
                    Console.WriteLine();
                }

                Utils.WriteStatusLog($"Chapter {section.Title.PrintHebrew()} Downloaded successfully.");
                var size = new DirectoryInfo(currentDir).FolderSize().ToReadableFileSize();
                Utils.WriteStatusLog($"Size: {size}");
            }

            Utils.WriteStatusLog("_________________Finish_________________");
            var totalSize = new DirectoryInfo(coursePath).FolderSize().ToReadableFileSize();
            Utils.WriteStatusLog($"Size: {totalSize} |  Videos: {course.SectionList.Sum(s => s.ChapterList.Count)}");

            Utils.CreateUrlShortcut(coursePath, course.Title, course.Url);
            Process.Start(new ProcessStartInfo { FileName = coursePath, UseShellExecute = true, Verb = "open" });
        }

        private static void DownloadFileWithProgress(string url, string filePath)
        {
            if (url.IsNullOrEmpty() || filePath.IsNullOrEmpty()) return;

            using (var client = new WebClient())
            {
                 Console.WriteLine();
                _progress = new ProgressBar();
                client.DownloadFileCompleted += DownloadFileCompleted;
                client.DownloadProgressChanged += DownloadProgressChanged;
                client.DownloadFile(new Uri(url), filePath);
            }
        }

        private static void DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            _progress.Report((double)100 / 100);
            Thread.Sleep(200);
            _progress.Dispose();
            Console.WriteLine();

            var webClient = sender as WebClient;
            if (webClient != null)
            {
                var totalFileLength = webClient.ResponseHeaders["Content-Length"];
                if (totalFileLength.IsNotNullOrEmpty())
                {
                    Utils.WriteStatusLog($"File Downloaded Completed - {Convert.ToInt64(totalFileLength).ToReadableFileSize()}");
                }
            }
        }

        private static void DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            _progress.Report((double)e.BytesReceived / e.TotalBytesToReceive);
        }
    }
}

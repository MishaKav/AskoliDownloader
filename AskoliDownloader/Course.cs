using System.Collections.Generic;

namespace AskoliDownloader
{
    public class Course
    {
        public string Title;
        public string Url;
        public List<Section> SectionList;
    }

    public class Section
    {
        public string Title;
        public List<ChapterItem> ChapterList;
    }


    public class Volume
    {
        public string cookieName { get; set; }
        public double value { get; set; }
    }

    public class ChapterItem
    {
        public int id { get; set; }
        public string movieurl { get; set; }
        public string srtfile { get; set; }
        public object isfree { get; set; }
        public string videoname { get; set; }
        public string lecturename { get; set; }
        public string poster { get; set; }
        public object chapterorder { get; set; }
        public string webvtt { get; set; }
    }

    public class Chapters
    {
        public int currentIndex { get; set; }
        public bool autoSetTrack { get; set; }
        public string autoSetTrackCookieName { get; set; }
        public List<ChapterItem> items { get; set; }
    }

    public class AscoliObject
    {
        public Volume volume { get; set; }
        public Chapters chapters { get; set; }
        public int mainId { get; set; }
        public bool allowTracking { get; set; }
        public bool autoPlay { get; set; }
        public string autoPlayCookieName { get; set; }
    }
}

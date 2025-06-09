using System;

namespace JobAutomation.Console.Models
{
    public class JobPost
    {
        public required string Title { get; set; }
        public required string Company { get; set; }
        public required string Location { get; set; }
        public required string Description { get; set; }
        public required string Url { get; set; }
        public DateTime PostedDate { get; set; }
        public required string SeoOptimizedContent { get; set; }
        public string? FocusKeyword { get; set; }
    }
} 
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using JobAutomation.Console.Models;
using JobAutomation.Console.Services;

namespace JobAutomation.Console.Services
{
    public class GoogleJobsScraper : IJobScraper
    {
        private readonly WordPressService _wordPressService;

        public GoogleJobsScraper(WordPressService wordPressService)
        {
            _wordPressService = wordPressService;
        }

        public string SourceName => "GoogleJobs";

        public async Task<List<JobPost>> ScrapeJobsAsync(string searchTerm, string location)
        {
            var jobs = new List<JobPost>();
            var options = new ChromeOptions();
            // Removed headless mode to make browser visible
            options.AddArgument("--disable-gpu");
            options.AddArgument("--no-sandbox");
            options.AddArgument("--disable-dev-shm-usage");
            options.AddArgument("--disable-blink-features=AutomationControlled");
            options.AddArgument("--user-agent=Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");

            using (var driver = new ChromeDriver(options))
            {
                try
                {
                    // Navigate to Google Jobs directly with software engineer search
                    driver.Navigate().GoToUrl("https://www.google.com/search?q=software+engineer+jobs&jbr=sep:0&udm=8&ved=2ahUKEwj4obOohOWNAxVKnf0HHb8lAeYQ3L8LegQIIxAN");
                    await Task.Delay(5000); // Increased initial wait time for page load
                    
                    // Check for captcha verification
                    if (driver.Url.Contains("captcha") || driver.Url.Contains("sorry"))
                    {
                        System.Console.WriteLine("\nCaptcha verification detected!");
                        System.Console.WriteLine("Please complete the verification in the browser window.");
                        System.Console.WriteLine("Press Enter after completing the verification...");
                        System.Console.ReadLine();
                        
                        // Wait a bit after verification
                        await Task.Delay(5000);
                    }
                    
                    // Wait for the jobs to load with increased timeout
                    var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));

                    // Handle the popup if it appears
                    try {
                        var notNowButton = wait.Until(d => d.FindElement(By.CssSelector("div.sjVJQd.pt054b")));
                        if (notNowButton != null && notNowButton.Displayed) {
                            System.Console.WriteLine("Found popup, clicking 'Not now'...");
                            notNowButton.Click();
                            await Task.Delay(3000); // Increased wait time after popup
                        }
                    } catch {
                        // No popup found, continue
                        System.Console.WriteLine("No popup found, continuing...");
                    }

                    System.Console.WriteLine("Proceeding with job scraping...");

                    // Get all job links using class gmxZue
                    var jobLinks = wait.Until(d => d.FindElements(By.CssSelector("span.gmxZue")).ToList());
                    System.Console.WriteLine($"Found {jobLinks.Count} job listings.");

                    if (jobLinks.Count == 0)
                    {
                        System.Console.WriteLine("No job listings found.");
                        return jobs;
                    }

                    for (int i = 0; i < jobLinks.Count; i++)
                    {
                        try
                        {
                            // Debug point 1: Start of job processing
                            System.Diagnostics.Debug.WriteLine($"\n=== Starting to process job {i + 1} ===");
                            
                            // Refresh job links list to get fresh elements
                            jobLinks = driver.FindElements(By.CssSelector("span.gmxZue")).ToList();
                            if (i >= jobLinks.Count)
                            {
                                System.Console.WriteLine("Job list changed, stopping...");
                                break;
                            }

                            var jobLink = jobLinks[i];
                            System.Console.WriteLine($"\nProcessing job {i + 1} of {jobLinks.Count}...");
                            
                            // Get the job URL from the parent anchor
                            var parentAnchor = jobLink.FindElement(By.XPath("./ancestor::a"));
                            string jobUrl = parentAnchor.GetAttribute("href");
                            System.Console.WriteLine($"Job URL: {jobUrl}");

                            // Debug point 2: Before clicking
                            System.Diagnostics.Debug.WriteLine($"About to click job link: {jobUrl}");

                            // Click the job link to open side panel using JavaScript
                            System.Console.WriteLine("Clicking job link...");
                            try {
                                // First try to click using JavaScript
                                ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", parentAnchor);
                                System.Diagnostics.Debug.WriteLine("JavaScript click successful");
                            } catch (Exception ex) {
                                System.Diagnostics.Debug.WriteLine($"JavaScript click failed: {ex.Message}");
                                // If JavaScript click fails, try regular click
                                try {
                                    parentAnchor.Click();
                                    System.Diagnostics.Debug.WriteLine("Regular click successful");
                                } catch (Exception ex2) {
                                    System.Diagnostics.Debug.WriteLine($"Regular click failed: {ex2.Message}");
                                    System.Console.WriteLine("Both click methods failed, trying to navigate directly...");
                                    driver.Navigate().GoToUrl(jobUrl);
                                }
                            }
                            
                            // Wait for the side panel to load
                            await Task.Delay(3000);

                            // Debug point 3: After clicking
                            System.Diagnostics.Debug.WriteLine("Checking for side panel...");

                            // Wait for the side panel to be visible and loaded
                            var sidePanel = wait.Until(d => d.FindElement(By.CssSelector("div.tNxQIb.PUpOsf")));
                            if (sidePanel == null || !sidePanel.Displayed)
                            {
                                System.Console.WriteLine("Side panel not visible, skipping this job...");
                                continue;
                            }

                            // Debug point 4: Side panel found
                            System.Diagnostics.Debug.WriteLine("Side panel found and visible");

                            // Wait a bit more to ensure all content is loaded
                            await Task.Delay(2000);

                            // Force refresh the side panel content
                            try {
                                // Scroll the side panel into view
                                ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView(true);", sidePanel);
                                await Task.Delay(1000);

                                // Try to force a refresh of the side panel
                                ((IJavaScriptExecutor)driver).ExecuteScript(@"
                                    var event = new Event('scroll');
                                    window.dispatchEvent(event);
                                ");
                                await Task.Delay(1000);
                                System.Diagnostics.Debug.WriteLine("Side panel refreshed");
                            } catch (Exception ex) {
                                System.Diagnostics.Debug.WriteLine($"Failed to refresh side panel: {ex.Message}");
                                System.Console.WriteLine("Could not refresh side panel");
                            }
                            
                            // Extract job information
                            System.Console.WriteLine("Extracting job information...");
                            
                            // Clear previous values
                            string title = string.Empty;
                            string company = string.Empty;
                            string jobLocation = string.Empty;
                            string description = string.Empty;

                            // Debug point 5: Before extracting title
                            System.Diagnostics.Debug.WriteLine("Attempting to extract title...");

                            // Try to get title first to confirm we're on the right job
                            try {
                                // Get the parent anchor of the current job link
                                var currentJobAnchor = jobLinks[i].FindElement(By.XPath("./ancestor::a"));
                                
                                // Find the title element within this specific job's context
                                var titleElement = currentJobAnchor.FindElement(By.CssSelector("div.tNxQIb.PUpOsf"));
                                title = titleElement.Text;
                                System.Diagnostics.Debug.WriteLine($"Title element found: {title}");
                                System.Console.WriteLine($"Raw title element text: {title}");
                                
                                if (string.IsNullOrEmpty(title))
                                {
                                    System.Console.WriteLine("Title is empty, might be wrong job panel, skipping...");
                                    continue;
                                }
                                System.Console.WriteLine($"Found title: {title}");

                                // Now get other details within the same job context
                                try {
                                    var companyElement = currentJobAnchor.FindElement(By.CssSelector("div.wHYlTd.MKCbgd.a3jPc"));
                                    company = companyElement.Text;
                                    System.Diagnostics.Debug.WriteLine($"Company element found: {company}");
                                    System.Console.WriteLine($"Raw company element text: {company}");
                                    System.Console.WriteLine($"Found company: {company}");
                                } catch (Exception ex) {
                                    System.Diagnostics.Debug.WriteLine($"Failed to get company: {ex.Message}");
                                    System.Console.WriteLine($"Could not find company name element: {ex.Message}");
                                }

                                try {
                                    var locationElement = currentJobAnchor.FindElement(By.CssSelector("div.wHYlTd.FqK3wc.MKCbgd"));
                                    jobLocation = locationElement.Text;
                                    System.Diagnostics.Debug.WriteLine($"Location element found: {jobLocation}");
                                    System.Console.WriteLine($"Raw location element text: {jobLocation}");
                                    System.Console.WriteLine($"Found location: {jobLocation}");
                                } catch (Exception ex) {
                                    System.Diagnostics.Debug.WriteLine($"Failed to get location: {ex.Message}");
                                    System.Console.WriteLine($"Could not find location element: {ex.Message}");
                                }

                                // Try to get description
                                try {
                                    System.Console.WriteLine("\n=== Starting Description Extraction ===");
                                    
                                    // Wait a bit for the description to load
                                    await Task.Delay(2000);

                                    // Create a longer wait specifically for description
                                    var descWait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
                                    
                                    // Try to get first description
                                    try
                                    {
                                        System.Console.WriteLine("Looking for first description...");
                                        var descElement = descWait.Until(d => {
                                            var element = driver.FindElement(By.CssSelector("#Sva75c > div.A8mJGd.NDuZHe > div.LrPjRb > div > div.BIB1wf.EIehLd.fHE6De.Emjfjd > c-wiz > div > c-wiz:nth-child(1) > c-wiz > c-wiz > div:nth-child(6) > div > span.hkXmid"));
                                            if (element != null && element.Displayed)
                                            {
                                                System.Console.WriteLine("Found first description element");
                                                return element;
                                            }
                                            throw new NoSuchElementException("First description element not found");
                                        });

                                        description = descElement.Text;
                                        if (!string.IsNullOrEmpty(description))
                                        {
                                            System.Console.WriteLine($"First description length: {description.Length} characters");
                                            System.Console.WriteLine($"First description: {description}");
                                        }
                                        else
                                        {
                                            var innerHtml = descElement.GetAttribute("innerHTML");
                                            if (!string.IsNullOrEmpty(innerHtml))
                                            {
                                                description = innerHtml.Replace("<br>", "\n")
                                                                     .Replace("<br/>", "\n")
                                                                     .Replace("<br />", "\n")
                                                                     .Replace("&amp;", "&")
                                                                     .Trim();
                                                System.Console.WriteLine("Got first description from innerHTML");
                                                System.Console.WriteLine($"First description length: {description.Length} characters");
                                                System.Console.WriteLine($"First description: {description}");
                                            }
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        System.Console.WriteLine($"Failed to find first description: {ex.Message}");
                                    }

                                    // Try to get second description
                                    try
                                    {
                                        System.Console.WriteLine("\nLooking for second description...");
                                        var descElement2 = descWait.Until(d => {
                                            try {
                                                var element = driver.FindElement(By.CssSelector("#Sva75c > div.A8mJGd.NDuZHe > div.LrPjRb > div > div.BIB1wf.EIehLd.fHE6De.Emjfjd > c-wiz > div > c-wiz:nth-child(1) > c-wiz > c-wiz > div:nth-child(6) > div > span.us2QZb"));
                                                if (element != null)
                                                {
                                                    System.Console.WriteLine("Found second description element");
                                                    return element;
                                                }
                                            } catch (Exception ex) {
                                                System.Console.WriteLine($"Error finding second description element: {ex.Message}");
                                            }
                                            throw new NoSuchElementException("Second description element not found");
                                        });

                                        // Try to get text even if element is not displayed
                                        string description2 = "";
                                        try {
                                            description2 = descElement2.Text;
                                            System.Console.WriteLine("Got text from second description element");
                                        } catch (Exception ex) {
                                            System.Console.WriteLine($"Error getting text from second description: {ex.Message}");
                                        }

                                        if (!string.IsNullOrEmpty(description2))
                                        {
                                            System.Console.WriteLine($"Second description length: {description2.Length} characters");
                                            System.Console.WriteLine($"Second description: {description2}");
                                            
                                            // Combine both descriptions if first one exists
                                            if (!string.IsNullOrEmpty(description))
                                            {
                                                description = description + "\n\n" + description2;
                                                System.Console.WriteLine("Combined both descriptions");
                                            }
                                            else
                                            {
                                                description = description2;
                                            }
                                        }
                                        else
                                        {
                                            // Try to get innerHTML even if element is not displayed
                                            try {
                                                var innerHtml = descElement2.GetAttribute("innerHTML");
                                                if (!string.IsNullOrEmpty(innerHtml))
                                                {
                                                    var desc2 = innerHtml.Replace("<br>", "\n")
                                                                       .Replace("<br/>", "\n")
                                                                       .Replace("<br />", "\n")
                                                                       .Replace("&amp;", "&")
                                                                       .Trim();
                                                    System.Console.WriteLine("Got second description from innerHTML");
                                                    System.Console.WriteLine($"Second description length: {desc2.Length} characters");
                                                    System.Console.WriteLine($"Second description: {desc2}");
                                                    
                                                    // Combine both descriptions if first one exists
                                                    if (!string.IsNullOrEmpty(description))
                                                    {
                                                        description = description + "\n\n" + desc2;
                                                        System.Console.WriteLine("Combined both descriptions");
                                                    }
                                                    else
                                                    {
                                                        description = desc2;
                                                    }
                                                }
                                            } catch (Exception ex) {
                                                System.Console.WriteLine($"Error getting innerHTML from second description: {ex.Message}");
                                            }
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        System.Console.WriteLine($"Failed to find second description: {ex.Message}");
                                        // Try one more time with a different approach
                                        try {
                                            var elements = driver.FindElements(By.CssSelector("span.us2QZb"));
                                            if (elements != null && elements.Count > 0) {
                                                var element = elements[0];
                                                var text = element.Text;
                                                if (!string.IsNullOrEmpty(text)) {
                                                    System.Console.WriteLine("Found second description using alternative method");
                                                    if (!string.IsNullOrEmpty(description)) {
                                                        description = description + "\n\n" + text;
                                                    } else {
                                                        description = text;
                                                    }
                                                }
                                            }
                                        } catch (Exception ex2) {
                                            System.Console.WriteLine($"Failed to find second description with alternative method: {ex2.Message}");
                                        }
                                    }

                                    if (string.IsNullOrEmpty(description))
                                    {
                                        System.Console.WriteLine("Could not find any description");
                                        description = "Description not found";
                                    }
                                }
                                catch (Exception ex)
                                {
                                    System.Console.WriteLine($"Failed to find descriptions: {ex.Message}");
                                    description = "Description not found";
                                }
                                System.Console.WriteLine("=== End Description Extraction ===\n");
                            
                            } catch (Exception ex) {
                                System.Diagnostics.Debug.WriteLine($"Failed to get job details: {ex.Message}");
                                System.Console.WriteLine($"Could not find job details: {ex.Message}");
                                continue;
                            }

                            // Debug point 10: Before creating job post
                            System.Diagnostics.Debug.WriteLine("Creating job post object...");

                            if (!string.IsNullOrEmpty(title) && !string.IsNullOrEmpty(company))
                            {
                                var jobPost = new JobPost
                                {
                                    Title = title,
                                    Company = company,
                                    Location = jobLocation,
                                    Description = description,
                                    Url = jobUrl,
                                    PostedDate = DateTime.Now,
                                    SeoOptimizedContent = $"{title} - {company} - {jobLocation}. {description}"
                                };

                                System.Console.WriteLine("\n=== JOB DETAILS ===");
                                System.Console.WriteLine("------------------");
                                System.Console.WriteLine($"Title: {title}");
                                System.Console.WriteLine($"Company: {company}");
                                System.Console.WriteLine($"Location: {jobLocation}");
                                System.Console.WriteLine($"URL: {jobUrl}");
                                System.Console.WriteLine("\nDescription:");
                                System.Console.WriteLine("------------------");
                                System.Console.WriteLine(description);
                                System.Console.WriteLine("------------------");
                                System.Console.WriteLine("=== END OF JOB DETAILS ===\n");

                                // Debug point 11: Before WordPress posting
                                System.Diagnostics.Debug.WriteLine("Attempting to post to WordPress...");

                                // Post to WordPress
                                System.Console.WriteLine("Posting to WordPress...");
                                try
                                {
                                      var success = await _wordPressService.PostJobAsync(jobPost);
                                    if (success)
                                    {
                                        System.Console.WriteLine("Successfully posted to WordPress!");
                                    }
                                    else
                                    {
                                        System.Console.WriteLine("Failed to post to WordPress.");
                                    }
                                }
                                catch (Exception ex)
                                {
                                    System.Diagnostics.Debug.WriteLine($"WordPress posting failed: {ex.Message}");
                                    System.Console.WriteLine($"Error posting to WordPress: {ex.Message}");
                                }
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine("Skipping job due to missing title or company");
                                System.Console.WriteLine("Skipping job due to missing title or company...");
                            }

                            // Wait before processing next job
                            await Task.Delay(2000);
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error in job processing: {ex.Message}");
                            System.Console.WriteLine($"Error processing job link: {ex.Message}");
                        }
                    }

                    System.Console.WriteLine($"\nProcessing complete. Total jobs processed: {jobs.Count}");
                    System.Console.WriteLine("Press Enter to close the browser and continue...");
                    System.Console.ReadLine();
                }
                catch (Exception ex)
                {
                    System.Console.WriteLine($"Error in Google Jobs scraping: {ex.Message}");
                }
                finally
                {
                    driver.Quit();
                }
            }

            return jobs;
        }

        private string GetElementTextWithRetry(IWebDriver driver, WebDriverWait wait, By by)
        {
            var maxRetries = 2; // Reduced from 3 to 2
            var retryCount = 0;
            
            while (retryCount < maxRetries)
            {
                try
                {
                    var element = wait.Until(d => d.FindElement(by));
                    return element.Text;
                }
                catch (Exception ex)
                {
                    retryCount++;
                    System.Console.WriteLine($"Retry {retryCount}/{maxRetries} for {by}: {ex.Message}");
                    Task.Delay(500).Wait(); // Reduced from 1000 to 500
                }
            }
            
            return string.Empty;
        }

        private string GetElementAttributeWithRetry(IWebDriver driver, WebDriverWait wait, By by, string attribute)
        {
            var maxRetries = 2; // Reduced from 3 to 2
            var retryCount = 0;
            
            while (retryCount < maxRetries)
            {
                try
                {
                    var element = wait.Until(d => d.FindElement(by));
                    return element.GetAttribute(attribute);
                }
                catch (Exception ex)
                {
                    retryCount++;
                    System.Console.WriteLine($"Retry {retryCount}/{maxRetries} for {by} attribute {attribute}: {ex.Message}");
                    Task.Delay(500).Wait(); // Reduced from 1000 to 500
                }
            }
            
            return string.Empty;
        }
    }
} 
using Hspi.OpenZWaveDB;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HSPI_ZWaveParametersTest
{
    [TestClass]
    public class SanitizeHtmlTest
    {
        [DataTestMethod]
        [DataRow("abc def", "abc def", DisplayName ="Not Html" )]
        [DataRow("<a href=\"https://www.w3schools.com\">Visit W3Schools.com!</a> Page", "Visit W3Schools.com! Page", DisplayName ="a Tag" )]
        [DataRow("<script>alert('Hello');</script>", "alert('Hello');", DisplayName ="script Tag" )]
        [DataRow("<p>Press <kbd>Ctrl</kbd> + <kbd>C</kbd> to copy text (Windows).</p>", "Press Ctrl + C to copy text (Windows).", DisplayName ="p Tag" )]
        public void StripHtml(string html, string sanitizedHtml)
        {
            var htmlSanitizer = new SanitizeHtml();
            Assert.AreEqual(htmlSanitizer.Strip(html), sanitizedHtml);
        }

        [DataTestMethod]
        [DataRow("abc def", "abc def", DisplayName ="Not Html" )]
        [DataRow("<a href=\"https://www.w3schools.com\">Visit W3Schools.com!</a> Page", "Visit W3Schools.com! Page", DisplayName ="a Tag" )]
        [DataRow("<script>alert('Hello');</script>", "alert('Hello');", DisplayName ="script Tag" )]
        [DataRow("<p>Press <kbd>Ctrl</kbd> + <kbd>C</kbd> to copy text (Windows).</p>", "<p>Press Ctrl + C to copy text (Windows).</p>", DisplayName ="p Tag" )]
        public void SanitizeHtml(string html, string sanitizedHtml)
        {
            var htmlSanitizer = new SanitizeHtml();
            Assert.AreEqual(htmlSanitizer.Sanitize(html), sanitizedHtml);
        }
    }
}
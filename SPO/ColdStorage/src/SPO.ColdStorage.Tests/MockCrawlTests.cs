using Microsoft.VisualStudio.TestTools.UnitTesting;
using SPO.ColdStorage.Migration.Engine;
using SPO.ColdStorage.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SPO.ColdStorage.Tests
{
    [TestClass]
    public class MockCrawlTests : AbstractTest
    {
        private List<SharePointFileInfoWithList> _foundFiles = new();

        [TestMethod]
        public async Task MockCrawl()
        {
            const int PAGE_COUNT = 100;
            const int PAGES = 5;
            var loader = new MockSiteLoader(PAGE_COUNT, PAGES);

            var crawl = new SiteListsAndLibrariesCrawler<int?>(loader, _tracer);
            await crawl.StartSiteCrawl(new SiteListFilterConfig(),
                (SharePointFileInfoWithList foundFile) => Crawler_SharePointFileFound(foundFile),
                () => CrawlComplete());

            Assert.IsTrue(_foundFiles.Count == PAGE_COUNT * PAGES);
            foreach (var ff in _foundFiles)
            {
                Assert.IsTrue(_foundFiles.Where(f=> f.FullSharePointUrl == ff.FullSharePointUrl).Count() == 1);
            }
        }

        private void CrawlComplete()
        {
            // Nowt
        }

        private Task Crawler_SharePointFileFound(SharePointFileInfoWithList foundFile)
        {
            _foundFiles.Add(foundFile);
            return Task.CompletedTask;
        }
    }
}

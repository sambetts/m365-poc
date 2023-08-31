using SPO.ColdStorage.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SPO.ColdStorage.Tests
{
    public abstract class BaseMockLoader
    {
        protected BaseMockLoader(int itemsPerPage, int pages)
        {
            ItemsPerPage = itemsPerPage;  
            Pages = pages;
        }
        public int ItemsPerPage { get; set; } = 0;
        public int Pages { get; set; } = 0;
    }
    public class MockSiteLoader : BaseMockLoader, ISiteCollectionLoader<int?>
    {
        public MockSiteLoader(int itemsPerPage, int pages) : base(itemsPerPage, pages)
        {
        }

        public Task<List<IWebLoader<int?>>> GetWebs()
        {
            var list = new List<IWebLoader<int?>>();
            list.Add(new MockWebLoader(ItemsPerPage, Pages));
            return Task.FromResult(list);
        }
    }

    public class MockWebLoader : BaseMockLoader, IWebLoader<int?>
    {
        public MockWebLoader(int itemsPerPage, int pages) : base(itemsPerPage, pages)
        {
        }

        public Task<List<IListLoader<int?>>> GetLists()
        {
            return Task.FromResult(new List<IListLoader<int?>>() { new MockListLoader(ItemsPerPage, Pages) });
        }
    }

    public class MockListLoader : BaseMockLoader, IListLoader<int?>
    {
        public MockListLoader(int itemsPerPage, int pages) : base(itemsPerPage, pages)
        {
        }

        public string Title { get; set; } = "Mock list";
        public Guid ListId { get; set; } = Guid.Empty;

        public Task<PageResponse<int?>> GetListItems(int? page)
        {
            // Generate fake data depending on mock config
            var result = new PageResponse<int?>();
            int wantedPage = 1;
            if (page.HasValue)
            {
                wantedPage = page.Value;
            }

            for (int i = 0; i < ItemsPerPage; i++)
            {
                var siteRoot = $"https://m365x352268.sharepoint.com";
                var siteRelativeUrl = $"{siteRoot}/page{wantedPage}";
                var siteFQDN = $"{siteRoot}{siteRelativeUrl}";
                result.FilesFound.Add(new SharePointFileInfoWithList
                {
                    ServerRelativeFilePath = $"{siteRelativeUrl}/subweb1/file" + i,
                    SiteUrl = siteFQDN,
                    WebUrl = $"{siteFQDN}/subweb1",
                    LastModified = DateTime.Now
                });

                result.FoldersFound.Add($"Folder {i}, page {page}");
            }
            if (wantedPage < Pages)
            {
                result.NextPageToken = ++wantedPage;
            }
            else
            {
                result.NextPageToken = null;
            }

            return Task.FromResult(result);
        }
    }
}

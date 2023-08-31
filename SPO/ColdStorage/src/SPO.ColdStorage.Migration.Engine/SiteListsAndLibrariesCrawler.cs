using Microsoft.Graph;
using Microsoft.SharePoint.Client;
using SPO.ColdStorage.Models;

namespace SPO.ColdStorage.Migration.Engine
{
    /// <summary>
    /// Finds files in a SharePoint site collection
    /// </summary>
    public class SiteListsAndLibrariesCrawler<T>
    {
        #region Constructors & Privates

        private readonly DebugTracer _tracer;
        private readonly ISiteCollectionLoader<T> _crawlConnector;

        public SiteListsAndLibrariesCrawler(ISiteCollectionLoader<T> crawlConnector, DebugTracer tracer)
        {
            _crawlConnector = crawlConnector;
            this._tracer = tracer;
        }

        #endregion

        public async Task StartSiteCrawl(SiteListFilterConfig siteFolderConfig, Func<SharePointFileInfoWithList, Task>? foundFileCallback, Action? crawlComplete)
        {
            var webs = await _crawlConnector.GetWebs();

            foreach (var subSweb in webs)
            {
                await ProcessWeb(subSweb, siteFolderConfig, foundFileCallback);
            }
            crawlComplete?.Invoke();
        }

        private async Task ProcessWeb(IWebLoader<T> web, SiteListFilterConfig siteFolderConfig, Func<SharePointFileInfoWithList, Task>? foundFileCallback)
        {
            var lists = await web.GetLists();

            foreach (var list in lists)
            {
                if (siteFolderConfig.IncludeListInMigration(list.Title))
                {
                    var listConfig = siteFolderConfig.GetListFolderConfig(list.Title);
                    await CrawlList(list, listConfig, foundFileCallback);
                }
                else
                {
                    _tracer.TrackTrace($"Skipping list '{list.Title}'");
                }
            }
        }
        public async Task<SiteCrawlContentsAndStats> CrawlList(IListLoader<T> parentList, ListFolderConfig listConfig, Func<SharePointFileInfoWithList, Task>? foundFileCallback)
        {
            PageResponse<T>? listPage = null;

            var listResultsAll = new SiteCrawlContentsAndStats();
            T? token = default(T);

            var allFolders = new List<string>();

            int pageCount = 1;
            while (listPage == null || listPage.NextPageToken != null)
            {
                try
                {
                    listPage = await parentList.GetListItems(token);
                }
                catch (ServerException ex)
                {
                    _tracer.TrackTrace($"Error reading list '{parentList.Title}'", Microsoft.ApplicationInsights.DataContracts.SeverityLevel.Error);
                    _tracer.TrackTrace(ex.Message, Microsoft.ApplicationInsights.DataContracts.SeverityLevel.Error);
                    return listResultsAll;
                }
                token = listPage.NextPageToken;

                // Filter files
                foreach (var file in listPage.FilesFound)
                {
                    if (listConfig.IncludeFolder(file))
                    {
                        if (foundFileCallback != null)
                        {
                            await foundFileCallback.Invoke(file);
                        }
                        listResultsAll.FilesFound.Add(file);
                    }
                    else
                    {
                        listResultsAll.IgnoredFiles++;
                    }
                }
                _tracer.TrackTrace($"Loaded {listPage.FilesFound.Count.ToString("N0")} files and {listPage.FoldersFound.Count.ToString("N0")} folders from list '{parentList.Title}' on page {pageCount}...");

                allFolders.AddRange(listPage.FoldersFound);

                pageCount++;
            }
            if (pageCount > 1)
            {
                _tracer.TrackTrace($"List '{parentList.Title}' totals: {listResultsAll.FilesFound.Count.ToString("N0")} files in scope, " +
                    $"{listResultsAll.IgnoredFiles.ToString("N0")} files ignored, and {listResultsAll.FoldersFound.Count.ToString("N0")} folders");
            }


            // Add unique folders
            listResultsAll.FoldersFound.AddRange(allFolders.Where(newFolderFound => !listResultsAll.FoldersFound.Contains(newFolderFound)));

            return listResultsAll;

        }
    }
}

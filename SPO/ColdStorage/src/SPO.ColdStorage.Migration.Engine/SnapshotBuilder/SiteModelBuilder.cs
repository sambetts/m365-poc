using SPO.ColdStorage.Entities;
using SPO.ColdStorage.Entities.Configuration;
using SPO.ColdStorage.Entities.DBEntities;
using SPO.ColdStorage.Migration.Engine.Utils;
using SPO.ColdStorage.Models;
using SPO.ColdStorage.Migration.Engine.Utils.Extentions;
using SPO.ColdStorage.Migration.Engine.Utils.Http;
using SPO.ColdStorage.Migration.Engine.Connectors;
using Microsoft.SharePoint.Client;

namespace SPO.ColdStorage.Migration.Engine.SnapshotBuilder
{
    /// <summary>
    /// Builds a snapshot for a single site
    /// </summary>
    public class SiteModelBuilder : BaseComponent, IDisposable
    {
        #region Privates & Constructors

        private readonly TargetMigrationSite _site;
        private readonly SPOColdStorageDbContext _db;
        private readonly SiteListFilterConfig _siteFilterConfig;
        private readonly SiteSnapshotModel _model;
        private SecureSPThrottledHttpClient _httpClient;

        private bool _showStats = false;
        private List<SharePointFileInfoWithList> _fileFoundBuffer = new();
        private List<Task<BackgroundUpdate>> _backgroundMetaTasksAll = new();


        public SiteModelBuilder(Config config, DebugTracer debugTracer, TargetMigrationSite site) : base(config, debugTracer)
        {
            this._site = site;
            _db = new SPOColdStorageDbContext(this._config);
            _model = new SiteSnapshotModel();
            _httpClient = new SecureSPThrottledHttpClient(_config, true, _tracer);

            // Figure out what to analyse
            SiteListFilterConfig? siteFilterConfig = null;
            if (!string.IsNullOrEmpty(site.FilterConfigJson))
            {
                try
                {
                    siteFilterConfig = SiteListFilterConfig.FromJson(site.FilterConfigJson);
                }
                catch (Exception ex)
                {
                    _tracer.TrackTrace($"Couldn't deserialise filter JSon for site '{site.RootURL}': {ex.Message}", Microsoft.ApplicationInsights.DataContracts.SeverityLevel.Warning);
                }
            }

            // Instantiate "allow all" config if none can be found in the DB
            if (siteFilterConfig == null)
                _siteFilterConfig = new SiteListFilterConfig();
            else
            {
                _siteFilterConfig = siteFilterConfig;
            }
        }
        public void Dispose()
        {
            _db.Dispose();
        }

        #endregion

        /// <summary>
        /// Background tasks getting item analytics
        /// </summary>
        public List<Task<BackgroundUpdate>> BackgroundMetaTasksAll { get => _backgroundMetaTasksAll; }

        public async Task<SiteSnapshotModel> Build()
        {
            return await Build(100, null, null);
        }
        public async Task<SiteSnapshotModel> Build(int batchSize, Action<List<SharePointFileInfoWithList>>? newFilesCallback, Action<List<DocumentSiteWithMetadata>>? filesUpdatedCallback)
        {
            if (batchSize < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(batchSize));
            }

            if (!_model.Finished.HasValue)
            {
                ClientContext? ctx = null;
                try
                {
                    ctx = await AuthUtils.GetClientContext(_config, _site.RootURL, _tracer, null);
                }
                catch (System.Net.WebException ex)
                {
                    _tracer.TrackTrace($"ERROR: '{ex.Message}' reading site {_site.RootURL}", Microsoft.ApplicationInsights.DataContracts.SeverityLevel.Error);
                    return _model;
                }

                var spConnector = new SPOSiteCollectionLoader(_config, _site.RootURL, _tracer);
                var crawler = new SiteListsAndLibrariesCrawler<ListItemCollectionPosition>(spConnector, _tracer);

                // Begin and block until all files crawled
                _model.Started = DateTime.Now;

                // Run background tasks
                _ = Task.Run(() => StartAnalysisStatsUpdates()).ConfigureAwait(false);

                await crawler.StartSiteCrawl(_siteFilterConfig, (SharePointFileInfoWithList foundFile) => Crawler_SharePointFileFound(foundFile, batchSize, newFilesCallback),
                    () => CrawlComplete(newFilesCallback));

                _tracer.TrackTrace($"STAGE 1/2: Finished crawling site files. Waiting for background update tasks to finish...");
                await Task.WhenAll(BackgroundMetaTasksAll);

                var filesToGetAnalysisFor = true;
                while (filesToGetAnalysisFor)
                {
                    // Check every second
                    await Task.Delay(5000);

                    // Load pending & non-fatal error files
                    var filesToLoad = _model.DocsByState(SiteFileAnalysisState.AnalysisPending);
                    filesToLoad.AddRange(_model.DocsByState(SiteFileAnalysisState.TransientError));

                    if (filesToLoad.Count > 0)
                    {
                        // Start metadata update any doc with "pending" state
                        Console.WriteLine($"Have completed {_model.DocsCompleted.Count} of {_model.AllFiles.Count}. Pending: {filesToLoad.Count} ({_model.DocsByState(SiteFileAnalysisState.TransientError).Count} errors to retry)");
                        await UpdatePendingFilesAsync(batchSize, filesToLoad.Cast<SharePointFileInfoWithList>().ToList(), filesUpdatedCallback);
                    }
                    else
                    {
                        Console.WriteLine("Waiting for update tasks to finish...");
                    }

                    // Check again if anything to do
                    filesToGetAnalysisFor = !_model.AnalysisFinished;
                }
                StopAnalysisStatsUpdates();
                _model.InvalidateCaches();
                _model.Finished = DateTime.Now;
                var ts = _model.Finished.Value.Subtract(_model.Started);
                _tracer.TrackTrace($"STAGE 2/2: Finished getting metadata for site files. All done in {ts.TotalMinutes.ToString("N2")} minutes.");
            }

            return _model;
        }


        #region Stats Update

        private void StopAnalysisStatsUpdates()
        {
            lock (this)
            {
                _showStats = false;
            }
        }

        async Task StartAnalysisStatsUpdates()
        {
            _showStats = true;
            while (_showStats)
            {
                lock (this)
                {
                    if (_model.DocsByState(SiteFileAnalysisState.AnalysisPending).Count > 0)
                        Console.WriteLine($"{_model.DocsByState(SiteFileAnalysisState.AnalysisPending).Count.ToString("N0")} files pending analytics & version data: " +
                            $"{_httpClient.CompletedCalls.ToString("N0")} calls completed; {_httpClient.ThrottledCalls.ToString("N0")} throttled (total); {_httpClient.ConcurrentCalls} currently active");

                }
                await Task.Delay(TimeSpan.FromMinutes(1));
            }
        }

        #endregion


        private SemaphoreSlim _bgTasksLimit = new(1, 1);
        async Task UpdatePendingFilesAsync(int batchSize, List<SharePointFileInfoWithList> filesToUpdate, Action<List<DocumentSiteWithMetadata>>? filesUpdatedCallback)
        {
            var backgroundTasksThisChunk = new List<Task<BackgroundUpdate>>();

            // Begin background loading of extra metadata
            var pendingFilesToAnalyse = new List<DocumentSiteWithMetadata>();

            // Throttle requests to one set of files to update at once
            await _bgTasksLimit.WaitAsync();

            foreach (var fileToUpdate in filesToUpdate)
            {
                // We only get stats for docs, not attachments
                if (fileToUpdate is DocumentSiteWithMetadata)
                {
                    var docToUpdate = (DocumentSiteWithMetadata)fileToUpdate;

                    // Avoid analysing more than once
                    docToUpdate.State = SiteFileAnalysisState.AnalysisInProgress;
                    pendingFilesToAnalyse.Add(docToUpdate);
                }

                // Start new background every $CHUNK_SIZE
                if (pendingFilesToAnalyse.Count >= batchSize)
                {
                    var newFileChunkCopy = new List<DocumentSiteWithMetadata>(pendingFilesToAnalyse);
                    pendingFilesToAnalyse.Clear();

                    // Background process chunk
                    backgroundTasksThisChunk.Add(newFileChunkCopy.GetDriveItemsAnalytics(_site.RootURL, _httpClient, _tracer));
                    backgroundTasksThisChunk.Add(newFileChunkCopy.GetDriveItemsHistory(_site.RootURL, _httpClient, _tracer));
                }
            }

            // Background process the rest
            if (pendingFilesToAnalyse.Count > 0)
            {
                backgroundTasksThisChunk.Add(pendingFilesToAnalyse.GetDriveItemsAnalytics(_site.RootURL, _httpClient, _tracer));
                backgroundTasksThisChunk.Add(pendingFilesToAnalyse.GetDriveItemsHistory(_site.RootURL, _httpClient, _tracer));
            }
            else
            {
                return;
            }

            // Update global tasks
            lock (this)
            {
                BackgroundMetaTasksAll.AddRange(backgroundTasksThisChunk);
            }

            // Compile results as they come
            var versionUpdates = new Dictionary<DriveItemSharePointFileInfo, DriveItemVersionInfo>();
            var analyticsUpdates = new Dictionary<DriveItemSharePointFileInfo, ItemAnalyticsRepsonse.AnalyticsItemActionStat>();

            await Task.WhenAll(backgroundTasksThisChunk);

            foreach (var finishedTask in backgroundTasksThisChunk)
            {
                foreach (var stat in finishedTask.Result.UpdateResults)
                {
                    if (stat.Value is DriveItemVersionInfo)
                    {
                        versionUpdates.Add(stat.Key, (DriveItemVersionInfo)stat.Value);
                    }
                    else if (stat.Value is ItemAnalyticsRepsonse)
                    {
                        analyticsUpdates.Add(stat.Key, ((ItemAnalyticsRepsonse)stat.Value).AccessStats ?? new ItemAnalyticsRepsonse.AnalyticsItemActionStat());
                    }
                }
            }

            // Release throttle now chunk is completed
            _bgTasksLimit.Release();

            // Update model with metadata & fire event
            var updatedFiles = new List<DocumentSiteWithMetadata>();
            foreach (var fileUpdated in analyticsUpdates)
            {
                lock (this)
                {
                    // Update model
                    var itemVersionInfo = versionUpdates.Where(i => i.Key.Equals(fileUpdated.Key)).SingleOrDefault();
                    updatedFiles.Add(_model.UpdateDocItemAndInvalidateCaches(fileUpdated.Key, fileUpdated.Value, itemVersionInfo.Value.Versions.ToVersionStorageInfo()));
                }
            }

            filesUpdatedCallback?.Invoke(updatedFiles);
        }

        private void CrawlComplete(Action<List<SharePointFileInfoWithList>>? remainderFilesCallback)
        {
            // Handle remaining files
            if (remainderFilesCallback != null)
            {
                remainderFilesCallback.Invoke(_fileFoundBuffer);
            }

            _fileFoundBuffer.Clear();
        }

        private Task Crawler_SharePointFileFound(SharePointFileInfoWithList foundFile, int batchSize, Action<List<SharePointFileInfoWithList>>? newFilesCallback)
        {
            SharePointFileInfoWithList? newFile = null;

            if (foundFile is DriveItemSharePointFileInfo)
            {
                var driveArg = (DriveItemSharePointFileInfo)foundFile;

                // Set newly found file as "pending" analysis data
                newFile = new DocumentSiteWithMetadata(driveArg) { State = SiteFileAnalysisState.AnalysisPending };
            }
            else
            {
                // Nothing to analyse for list item attachments
                newFile = foundFile;
            }

            // Add new found files to model & event buffer
            lock (this)
            {
                _fileFoundBuffer.Add(newFile);
                _model.AddFile(newFile, foundFile.List);

                // Do things every $batchSize
                if (_fileFoundBuffer.Count == batchSize)
                {
                    var bufferCopy = new List<SharePointFileInfoWithList>(_fileFoundBuffer);
                    if (newFilesCallback != null)
                    {
                        newFilesCallback.Invoke(bufferCopy);
                    }
                    _fileFoundBuffer.Clear();
                }
            }

            return Task.CompletedTask;
        }
    }
}

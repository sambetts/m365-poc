using Entities;
using Entities.Configuration;
using Entities.DBEntities;
using Microsoft.Extensions.Logging;
using Migration.Engine.Adapters;
using Migration.Engine.Utils.Extensions;
using Models;
using System.Collections.Concurrent;

namespace Migration.Engine.SnapshotBuilder;

/// <summary>
/// Builds a snapshot for a single SharePoint site using Microsoft Graph.
/// Crawls every drive (document library) in the site and collects access analytics
/// and version history for every file via <see cref="GraphDriveSnapshotBuilder"/> and
/// <see cref="GraphFileAnalyticsAdapter"/>.
/// </summary>
public class SiteModelBuilder : BaseComponent, IDisposable
{
    private readonly TargetMigrationSite _site;
    private readonly SiteSnapshotModel _model;
    private readonly IFileAnalyticsProvider _analyticsProvider;

    private readonly SemaphoreSlim _bgTasksLimit = new(1, 1);

    private volatile bool _showStats = false;
    private readonly ConcurrentBag<Task<BackgroundUpdate>> _backgroundMetaTasksAll = [];

    public SiteModelBuilder(
        Config config,
        ILogger ILogger,
        TargetMigrationSite site,
        IFileAnalyticsProvider? analyticsProvider = null) : base(config, ILogger)
    {
        _site = site;
        _model = new SiteSnapshotModel();
        _analyticsProvider = analyticsProvider ?? new GraphFileAnalyticsAdapter(config, site.RootURL, ILogger);
    }

    public void Dispose()
    {
        if (_analyticsProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
        _bgTasksLimit.Dispose();
    }

    /// <summary>
    /// Background tasks getting item analytics
    /// </summary>
    public IEnumerable<Task<BackgroundUpdate>> BackgroundMetaTasksAll => _backgroundMetaTasksAll;

    public Task<SiteSnapshotModel> Build()
    {
        return Build(100, null, null);
    }

    public async Task<SiteSnapshotModel> Build(int batchSize, Action<List<SharePointFileInfoWithList>>? newFilesCallback, Action<List<DocumentSiteWithMetadata>>? filesUpdatedCallback)
    {
        if (batchSize < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(batchSize));
        }

        if (_model.Finished.HasValue)
        {
            return _model;
        }

        try
        {
            _model.Started = DateTime.UtcNow;

            // STAGE 1: Crawl all drives in the site and add files to the model
            var driveBuilder = new GraphDriveSnapshotBuilder(_config, _site.RootURL, _logger);
            await driveBuilder.BuildSnapshotAsync(_model, batchSize, newFilesCallback).ConfigureAwait(false);

            _logger.LogInformation($"STAGE 1/2: Drive crawl complete. Files found: {_model.AllFiles.Count}");

            // STAGE 2: Collect analytics (access counts) and version history per file
            if (_model.AllFiles.Count > 0)
            {
                _logger.LogInformation("STAGE 2/2: Collecting analytics & version history for files...");
                _ = Task.Run(StartAnalysisStatsUpdates);
                await WaitForAnalysisCompletion(batchSize, filesUpdatedCallback).ConfigureAwait(false);
            }
            else
            {
                _model.Finished = DateTime.UtcNow;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"ERROR: '{ex.Message}' reading site {_site.RootURL}");
            _model.Finished = DateTime.UtcNow;
        }

        return _model;
    }

    /// <summary>
    /// Wait for file analysis to complete
    /// </summary>
    private async Task WaitForAnalysisCompletion(int batchSize, Action<List<DocumentSiteWithMetadata>>? filesUpdatedCallback)
    {
        var filesToGetAnalysisFor = true;
        while (filesToGetAnalysisFor)
        {
            // Check every 5 seconds
            await Task.Delay(5000).ConfigureAwait(false);

            // Load pending & non-fatal error files
            var filesToLoad = _model.DocsByState(SiteFileAnalysisState.AnalysisPending);
            filesToLoad.AddRange(_model.DocsByState(SiteFileAnalysisState.TransientError));

            if (filesToLoad.Count > 0)
            {
                Console.WriteLine($"Have completed {_model.DocsCompleted.Count} of {_model.AllFiles.Count}. Pending: {filesToLoad.Count} ({_model.DocsByState(SiteFileAnalysisState.TransientError).Count} errors to retry)");
                await UpdatePendingFilesAsync(batchSize, [.. filesToLoad.Cast<SharePointFileInfoWithList>()], filesUpdatedCallback).ConfigureAwait(false);
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
        _logger.LogInformation($"STAGE 2/2: Finished getting metadata for site files. All done in {ts.TotalMinutes:N2} minutes.");
    }

    private void StopAnalysisStatsUpdates()
    {
        _showStats = false;
    }

    async Task StartAnalysisStatsUpdates()
    {
        _showStats = true;
        while (_showStats)
        {
            var pendingCount = _model.DocsByState(SiteFileAnalysisState.AnalysisPending).Count;
            if (pendingCount > 0)
            {
                Console.WriteLine($"{pendingCount:N0} files pending analytics & version data");
            }

            await Task.Delay(TimeSpan.FromMinutes(1)).ConfigureAwait(false);
        }
    }

    async Task UpdatePendingFilesAsync(int batchSize, List<SharePointFileInfoWithList> filesToUpdate, Action<List<DocumentSiteWithMetadata>>? filesUpdatedCallback)
    {
        var backgroundTasksThisChunk = new List<Task<BackgroundUpdate>>();
        var pendingFilesToAnalyse = new List<DocumentSiteWithMetadata>();

        // Throttle requests to one set of files to update at once
        await _bgTasksLimit.WaitAsync().ConfigureAwait(false);

        foreach (var fileToUpdate in filesToUpdate)
        {
            // Every file added by the drive crawl is a DocumentSiteWithMetadata
            if (fileToUpdate is DocumentSiteWithMetadata docToUpdate)
            {
                // Avoid analysing more than once
                docToUpdate.State = SiteFileAnalysisState.AnalysisInProgress;
                pendingFilesToAnalyse.Add(docToUpdate);
            }

            // Start new background every $batchSize
            if (pendingFilesToAnalyse.Count >= batchSize)
            {
                var newFileChunkCopy = new List<DocumentSiteWithMetadata>(pendingFilesToAnalyse);
                pendingFilesToAnalyse.Clear();

                backgroundTasksThisChunk.Add(_analyticsProvider.GetFileAnalyticsAsync(newFileChunkCopy));
                backgroundTasksThisChunk.Add(_analyticsProvider.GetFileVersionHistoryAsync(newFileChunkCopy));
            }
        }

        // Background process the rest
        if (pendingFilesToAnalyse.Count > 0)
        {
            backgroundTasksThisChunk.Add(_analyticsProvider.GetFileAnalyticsAsync(pendingFilesToAnalyse));
            backgroundTasksThisChunk.Add(_analyticsProvider.GetFileVersionHistoryAsync(pendingFilesToAnalyse));
        }
        else
        {
            _bgTasksLimit.Release();
            return;
        }

        // Track global tasks (ConcurrentBag is thread-safe)
        foreach (var task in backgroundTasksThisChunk)
        {
            _backgroundMetaTasksAll.Add(task);
        }

        // Compile results as they come
        var versionUpdates = new Dictionary<DriveItemSharePointFileInfo, DriveItemVersionInfo>();
        var analyticsUpdates = new Dictionary<DriveItemSharePointFileInfo, ItemAnalyticsResponse.AnalyticsItemActionStat>();

        await Task.WhenAll(backgroundTasksThisChunk).ConfigureAwait(false);

        foreach (var finishedTask in backgroundTasksThisChunk)
        {
            foreach (var stat in finishedTask.Result.UpdateResults)
            {
                if (stat.Value is DriveItemVersionInfo versionInfo)
                {
                    versionUpdates[stat.Key] = versionInfo;
                }
                else if (stat.Value is ItemAnalyticsResponse analytics)
                {
                    analyticsUpdates[stat.Key] = analytics.AccessStats ?? new ItemAnalyticsResponse.AnalyticsItemActionStat();
                }
            }
        }

        // Release throttle now chunk is completed
        _bgTasksLimit.Release();

        // Update model with metadata & fire event
        var updatedFiles = new List<DocumentSiteWithMetadata>(analyticsUpdates.Count);
        foreach (var fileUpdated in analyticsUpdates)
        {
            var versionStorage = versionUpdates.TryGetValue(fileUpdated.Key, out var info)
                ? info.Versions.ToVersionStorageInfo()
                : null;

            var updatedDoc = _model.UpdateDocItemAndInvalidateCaches(fileUpdated.Key, fileUpdated.Value, versionStorage);
            updatedDoc.State = SiteFileAnalysisState.Complete;
            updatedFiles.Add(updatedDoc);
        }

        filesUpdatedCallback?.Invoke(updatedFiles);
    }
}

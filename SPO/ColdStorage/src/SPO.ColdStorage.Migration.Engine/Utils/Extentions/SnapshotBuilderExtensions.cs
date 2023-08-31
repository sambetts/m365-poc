using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SPO.ColdStorage.Entities;
using SPO.ColdStorage.Entities.Configuration;
using SPO.ColdStorage.Entities.DBEntities;
using SPO.ColdStorage.Migration.Engine.SnapshotBuilder;
using SPO.ColdStorage.Models;

namespace SPO.ColdStorage.Migration.Engine.Utils.Extentions
{
    public static class SnapshotBuilderExtensions
    {
        private static SemaphoreSlim ss = new(1, 1);
        public static async Task InsertFilesAsync(this List<SharePointFileInfoWithList> files, Config config, StagingFilesMigrator stagingFilesMigrator, DebugTracer tracer)
        {
            await ss.WaitAsync();

            try
            {
                using (var db = new SPOColdStorageDbContext(config))
                {
                    var executionStrategy = db.Database.CreateExecutionStrategy();

                    try
                    {
                        await executionStrategy.Execute(async () =>
                        {
                            using (var trans = await db.Database.BeginTransactionAsync())
                            {
                                var blockGuid = Guid.NewGuid();
                                var inserted = DateTime.Now;

                                // Insert staging data
                                var stagingFiles = new List<StagingTempFile>();
                                foreach (var insertedFile in files)
                                {
                                    if (insertedFile.IsValidInfo)
                                    {
                                        var f = new StagingTempFile(insertedFile, blockGuid, inserted);
                                        stagingFiles.Add(f);
                                    }
                                    else
                                    {
                                        tracer.TrackTrace($"Warning: found invalid file '{insertedFile.FullSharePointUrl}'. Ignoring", Microsoft.ApplicationInsights.DataContracts.SeverityLevel.Warning);
                                    }
                                }
                                await db.StagingFiles.AddRangeAsync(stagingFiles);
                                await db.SaveChangesAsync();

                                // Merge from staging to tables
                                await stagingFilesMigrator.MigrateBlockAndCleanFromStaging(db, blockGuid);

                                await trans.CommitAsync();
                            }
                        });

                    }
                    catch (SqlException ex)
                    {
                        tracer.TrackException(ex);
                        tracer.TrackTrace($"Got fatal SQL error saving file info block to SQL: {ex.Message}", Microsoft.ApplicationInsights.DataContracts.SeverityLevel.Critical);
                    }
                }
            }
            finally
            {
                ss.Release();
            }
        }
    }
}

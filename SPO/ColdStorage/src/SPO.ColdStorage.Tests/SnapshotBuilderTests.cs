using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SPO.ColdStorage.Entities;
using SPO.ColdStorage.Entities.Configuration;
using SPO.ColdStorage.Migration.Engine;
using SPO.ColdStorage.Migration.Engine.SnapshotBuilder;
using SPO.ColdStorage.Migration.Engine.Utils.Extentions;
using SPO.ColdStorage.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SPO.ColdStorage.Tests
{
    [TestClass]
    public class SnapshotBuilderTests : AbstractTest
    {
        /// <summary>
        /// Tests SiteSnapshotModel.AnalysisFinished
        /// </summary>
        [TestMethod]
        public void ModelAnalysisFinishedTests()
        {
            var m = new SiteSnapshotModel();
            var l = new DocLib();
            m.Lists.Add(l);

            var f1 = new DocumentSiteWithMetadata { State = SiteFileAnalysisState.AnalysisPending };
            var f2 = new DocumentSiteWithMetadata { State = SiteFileAnalysisState.AnalysisInProgress };
            l.Files.AddRange(new DocumentSiteWithMetadata []{ f1, f2 });

            m.InvalidateCaches();
            Assert.IsFalse(m.AnalysisFinished);

            f1.State = SiteFileAnalysisState.Complete;
            m.InvalidateCaches();
            Assert.IsFalse(m.AnalysisFinished);

            f2.State = SiteFileAnalysisState.Complete;
            m.InvalidateCaches();
            Assert.IsTrue(m.AnalysisFinished);

        }

        [TestMethod]
        public async Task SnapshotBuilderExtensionsTests()
        {
            var list = new List<SharePointFileInfoWithList>();
            var spList = new SiteList() { ServerRelativeUrl = $"/list{DateTime.Now.Ticks}" };

            const int INSERTS = 100;
            for (int i = 0; i < INSERTS; i++)
            {
                var siteUrl = "/site" + DateTime.Now.Ticks;
                var webUrl = siteUrl + "/web" + DateTime.Now.Ticks;
                var newDoc = new DocumentSiteWithMetadata
                {
                    AccessCount = i,
                    Author = $"Author {i}",
                    List = spList,
                    DriveId = DateTime.Now.Ticks.ToString(),
                    FileSize = i,
                    GraphItemId = DateTime.Now.Ticks.ToString(),
                    VersionCount = i,
                    SiteUrl = siteUrl,
                    WebUrl = webUrl,
                    ServerRelativeFilePath = webUrl + "/file.aspx",
                    LastModified = DateTime.Now
                };

                list.Add(newDoc);
                Assert.IsTrue(newDoc.IsValidInfo);
            }

            using (var db = new SPOColdStorageDbContext(_config!))
            {
                var preInsert = await db.Files.CountAsync();
                await list.InsertFilesAsync(_config!, new StagingFilesMigrator(), DebugTracer.ConsoleOnlyTracer());
                var postInsert = await db.Files.CountAsync();

                // Make sure we've actually inserted
                Assert.IsTrue(postInsert == preInsert + INSERTS);
            }
        }
    }
}

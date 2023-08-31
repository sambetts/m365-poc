using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using Microsoft.Graph;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SPOAzBlob.Engine;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace SPOAzBlob.Tests
{
    [TestClass]
    public class FileManagerTests : AbstractTest
    {
        const string FILE_CONTENTS = "En un lugar de la Mancha, de cuyo nombre no quiero acordarme, no ha mucho tiempo que vivía un hidalgo de los de lanza en astillero, adarga antigua, rocín flaco y galgo corredor";

        [TestMethod]
        public async Task StartFileEditInSpo()
        {
            var _blobServiceClient = new BlobServiceClient(_config!.ConnectionStrings.Storage);

            // Upload a fake file to blob
            var containerClient = _blobServiceClient.GetBlobContainerClient(_config.BlobContainerName);
            var fileRef = containerClient.GetBlobClient($"UnitTest-{DateTime.Now.Ticks}.txt");
            using (var fs = new MemoryStream(Encoding.UTF8.GetBytes(FILE_CONTENTS)))
            {
                await fileRef.UploadAsync(fs, true);
            }

            // Generate a new shared-access-signature
            var sasUri = _blobServiceClient.GenerateAccountSasUri(AccountSasPermissions.Read,
                DateTime.Now.AddDays(1),
                AccountSasResourceTypes.Container | AccountSasResourceTypes.Object);

            // Start edit with new file
            var fm = new FileOperationsManager(_config!, _tracer);

            var azFileUrl = fileRef.Uri.AbsoluteUri + sasUri.Query;
            var newItem = await fm.StartFileEditInSpo(azFileUrl, _config.AzureAdAppDisplayName);
            Assert.IsNotNull(newItem);

            // Try and start edit the same file again. Should fail. 
            await Assert.ThrowsExceptionAsync<SpoFileAlreadyBeingEditedException>(async () => await fm.StartFileEditInSpo(azFileUrl, "Unit test user"));

        }

        // Upload a file to Az blob; start editing; make a change; check we can see change again; finish editing.
        [TestMethod]
        public async Task FullCycleTest()
        {
            var azureStorageManager = new AzureStorageManager(_config!, _tracer);
            var _blobServiceClient = new BlobServiceClient(_config!.ConnectionStrings.Storage);

            var drive = await _client!.Sites[_config.SharePointSiteId].Drive.Request().GetAsync();

            // Clear locks
            var allLocks = await azureStorageManager.GetLocks(drive.Id);
            foreach (var l in allLocks)
            {
                await azureStorageManager.ClearLock(l);
            }
            var allLocksPostClear = await azureStorageManager.GetLocks(drive.Id);
            Assert.IsTrue(allLocksPostClear.Count == 0);


            // Prep: create file in Az and start to edit it, so we have a DriveItem
            var containerClient = _blobServiceClient.GetBlobContainerClient(_config.BlobContainerName);
            var fileRef = containerClient.GetBlobClient($"UnitTest-{DateTime.Now.Ticks}.txt");
            using (var fs = new MemoryStream(Encoding.UTF8.GetBytes(FILE_CONTENTS)))
            {
                await fileRef.UploadAsync(fs, true);
            }

            var sasUri = _blobServiceClient.GenerateAccountSasUri(AccountSasPermissions.Read, DateTime.Now.AddDays(1), AccountSasResourceTypes.Container | AccountSasResourceTypes.Object);

            // Start editing a fake file, which will create a new lock
            var fm = new FileOperationsManager(_config!, _tracer);
            var azFileUrl = fileRef.Uri.AbsoluteUri + sasUri.Query;
            var newItem = await fm.StartFileEditInSpo(azFileUrl, _config.AzureAdAppDisplayName);

            // Clear delta token
            var dm = new DriveDeltaTokenManager(_config, _tracer, azureStorageManager);
            await dm.DeleteToken();

            // Now update Azure from SPO. As no delta token, all items in drive will be read.
            // We should hit our file, but it hasn't changed so won't be updated
            var fileUpdatedBackToAzure = await fm.ProcessSpoUpdatesForActiveLocks();
            Assert.IsTrue(fileUpdatedBackToAzure.Count == 0);

            // Update SPO file again 
            const string CONTENTSv2 = FILE_CONTENTS + "v2";
            DriveItem v2File;
            using (var fs = new MemoryStream(Encoding.UTF8.GetBytes(CONTENTSv2)))
            {
                v2File = await _client.Sites[_config.SharePointSiteId].Drive.Items[newItem.Id].Content
                                .Request()
                                .PutAsync<DriveItem>(fs);
            }


            // Now with delta, trigger an update back to Az blob. Should've updated 1 record as it has changed
            fileUpdatedBackToAzure = await fm.ProcessSpoUpdatesForActiveLocks();
            Assert.IsTrue(fileUpdatedBackToAzure.Count == 1);

            // Verify Azure file has updated content
            using (var fs = new MemoryStream())
            {
                var azFileUpdated = await fileRef.DownloadToAsync(fs);
                var downloadedFileContents = Encoding.UTF8.GetString(fs.ToArray());

                Assert.AreEqual(CONTENTSv2, downloadedFileContents);
            }

            // Unlock file
            await fm.FinishEditing(v2File.Id, drive.Id);

            // Verify unlock worked
            allLocks = await azureStorageManager.GetLocks(drive.Id);
            foreach (var l in allLocks)
            {
                await azureStorageManager.ClearLock(l);
            }
            var allLocksPostUnlock = await azureStorageManager.GetLocks(drive.Id);
            Assert.IsTrue(allLocksPostUnlock.Count == 0);
        }

        // Upload x1 file to SPO manually, and x1 file via editing a new file. Clean-up & check only 1 file was deleted.
        [TestMethod]
        public async Task CleanupTests()
        {
            var drive = await _client!.Sites[_config!.SharePointSiteId].Drive.Request().GetAsync();
            var blobServiceClient = new BlobServiceClient(_config!.ConnectionStrings.Storage);
            var spManager = new SPManager(_config!, _tracer);
            var azureStorageManager = new AzureStorageManager(_config!, _tracer);

            // Prep: run an clean so we can test only our file operations in this test.
            var fm = new FileOperationsManager(_config!, _tracer);
            await fm.CleanOldFiles(drive.Id);

            // Upload a fake file to blob
            var containerClient = blobServiceClient.GetBlobContainerClient(_config.BlobContainerName);
            var fileRef = containerClient.GetBlobClient($"UnitTest-{DateTime.Now.Ticks}.txt");
            using (var fs = new MemoryStream(Encoding.UTF8.GetBytes(FILE_CONTENTS)))
            {
                await fileRef.UploadAsync(fs, true);
            }

            // Start edit
            var sasUri = blobServiceClient.GenerateAccountSasUri(AccountSasPermissions.Read,
                DateTime.Now.AddDays(1),
                AccountSasResourceTypes.Container | AccountSasResourceTypes.Object);
            var azFileUrl = fileRef.Uri.AbsoluteUri + sasUri.Query;
            var newItem = await fm.StartFileEditInSpo(azFileUrl, _config.AzureAdAppDisplayName);
            Assert.IsNotNull(newItem);


            // Upload just a random file
            const string CONTENTSv2 = FILE_CONTENTS + "v2";
            using (var fs = new MemoryStream(Encoding.UTF8.GetBytes(CONTENTSv2)))
            {
                await spManager.UploadDoc($"RandomFile {DateTime.Now.Ticks}.txt", fs);
            }

            var cleanCount = await fm.CleanOldFiles(drive.Id);

            // Our random file only should be cleaned
            Assert.IsTrue(cleanCount == 1);


            // Unlock file so we can clean
            await azureStorageManager.ClearLock(newItem);

            // Our edited file only should be cleaned
            cleanCount = await fm.CleanOldFiles(drive.Id);
            Assert.IsTrue(cleanCount == 1);

        }
    }
}

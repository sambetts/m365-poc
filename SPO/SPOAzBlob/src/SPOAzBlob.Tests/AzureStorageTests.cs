using Microsoft.Graph;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SPOAzBlob.Engine;
using SPOAzBlob.Engine.Models;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace SPOAzBlob.Tests
{
    [TestClass]
    public class AzureStorageTests: AbstractTest
    {
        const string FILE_CONTENTS = "En un lugar de la Mancha, de cuyo nombre no quiero acordarme, no ha mucho tiempo que vivía un hidalgo de los de lanza en astillero, adarga antigua, rocín flaco y galgo corredor";


        [TestMethod]
        public async Task SPOUploadTest()
        {
            DriveItem? spoDoc = null;
            var sp = new SPManager(_config!, _tracer);
            using (var fs = new MemoryStream(Encoding.UTF8.GetBytes(FILE_CONTENTS)))
            {
                var fileName = $"{DateTime.Now.Ticks}.txt";
                spoDoc = await sp.UploadDoc(fileName, fs);
            }

            Assert.IsNotNull(spoDoc);
        }

        [TestMethod]
        public async Task UploadSpoFileToAzureTests()
        {
            // Create new file & upload
            var fileName = $"{DateTime.Now.Ticks}.txt";
            DriveItem? spoDoc = null;
            var sp = new SPManager(_config!, _tracer);
            using (var fs = new MemoryStream(Encoding.UTF8.GetBytes(FILE_CONTENTS)))
            {
                spoDoc = await sp.UploadDoc(fileName, fs);
            }

            var fileUpdater = new AzureStorageManager(_config!, _tracer);
            await fileUpdater.UploadSharePointFileToAzureBlob(fileName, "Unit tester");

            // Create a lock for another user for this file
            await fileUpdater.SetOrUpdateLock(spoDoc, "whatever", "Bob");

            // Now when we upload again, we should get an error
            await Assert.ThrowsExceptionAsync<SetLockFileLockedByAnotherUserException>(async () => await fileUpdater.UploadSharePointFileToAzureBlob(fileName, "Unit tester"));

            // Update file in SPO but don't update lock. Try uploading again with old lock still in place.
            using (var fs = new MemoryStream(Encoding.UTF8.GetBytes(FILE_CONTENTS + "v2")))
            {
                spoDoc = await sp.UploadDoc(fileName, fs);
            }
            await Assert.ThrowsExceptionAsync<SetLockFileUpdateConflictException>(async () => await fileUpdater.UploadSharePointFileToAzureBlob(fileName, "Bob"));

        }

        [TestMethod]
        public async Task FileLocksReadAndWriteTest()
        {
            DriveItem? spoDoc = null;
            var sp = new SPManager(_config!, _tracer);
            using (var fs = new MemoryStream(Encoding.UTF8.GetBytes(FILE_CONTENTS)))
            {
                var fileName = $"{DateTime.Now.Ticks}.txt";
                spoDoc = await sp.UploadDoc(fileName, fs);
            }

            var azManager = new AzureStorageManager(_config!, _tracer);

            var initialLocks = await azManager.GetLocks(spoDoc.ParentReference.DriveId);

            // No lock for this new doc yet. Make sure it's null
            var fileLock = await azManager.GetLock(spoDoc);
            Assert.IsNull(fileLock);

            // Set new lock & check again
            await azManager.SetOrUpdateLock(spoDoc, "whatever", "Unit tester");
            fileLock = await azManager.GetLock(spoDoc);
            Assert.IsNotNull(fileLock);

            // Check count
            var postNewLockInsertLocks = await azManager.GetLocks(spoDoc.ParentReference.DriveId);
            Assert.IsTrue(initialLocks.Count < postNewLockInsertLocks.Count);


            // Set lock for same file but different user. Should fail
            await Assert.ThrowsExceptionAsync<SetLockFileLockedByAnotherUserException>(async () => await azManager.SetOrUpdateLock(spoDoc, "whatever", "Unit tester2"));

            // Clear lock & check again
            await azManager.ClearLock(spoDoc);
            fileLock = await azManager.GetLock(spoDoc);
            Assert.IsNull(fileLock);
        }


        [TestMethod]
        public async Task PropertyBagReadAndWriteTest()
        {
            var azManager = new AzureStorageManager(_config!, _tracer);

            var randomPropName = DateTime.Now.Ticks.ToString();
            var randomPropVal = randomPropName + "-val";

            // No lock for this new doc yet. Make sure it's null
            var propertyBag = await azManager.GetPropertyValue(randomPropName);
            Assert.IsNull(propertyBag);

            // Set new lock & check again
            await azManager.SetPropertyValue(randomPropName, randomPropVal);
            propertyBag = await azManager.GetPropertyValue(randomPropName);
            Assert.IsNotNull(propertyBag);
            Assert.IsTrue(propertyBag.Value == randomPropVal);

            // Clear lock & check again
            await azManager.ClearPropertyValue(randomPropName);
            propertyBag = await azManager.GetPropertyValue(randomPropName);
            Assert.IsNull(propertyBag);
        }

        [TestMethod]
        public async Task DriveDeltaTokenManagerTests()
        {

            // Check delta string parsing works
            const string TOKEN = "abc1232";
            var testUrl = $"https://graph.microsoft.com/v1.0/sites('contoso.sharepoint.com,guid,guid')/drive/root/microsoft.graph.delta(token='{TOKEN}')?$expand=LastModifiedByUser";
            var tokenFromValidUrl = DriveDelta.ExtractCodeFromGraphUrl(testUrl);
            Assert.IsTrue(tokenFromValidUrl == TOKEN);

            // Check false case too
            Assert.IsNull(DriveDelta.ExtractCodeFromGraphUrl("https://graph.microsoft.com/v1.0/sites('contoso.sharepoint.com,guid,guid')"));

            var azManager = new AzureStorageManager(_config!, _tracer);
            var deltaManager = new DriveDeltaTokenManager(_config!, _tracer, azManager);
            var spManager = new SPManager(_config!, _tracer);
            await deltaManager.DeleteToken();

            // Get without a delta token
            await spManager.GetUpdatedDriveItems(azManager);

            // We should now have a new delta token
            var postGetDeltaToken = await deltaManager.GetToken();
            Assert.IsNotNull(postGetDeltaToken);
        }
    }
}

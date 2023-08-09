using Microsoft.Graph;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GraphFileCopy
{
    public class GraphCalls
    {
        public GraphCalls(GraphServiceClient graphServiceClient)
        {
            this.Client = graphServiceClient;
        }

        public GraphServiceClient Client { get; set; }



        public async Task CopyFilesAndMetadata(List sourceList, List destList)
        {
            Console.WriteLine($"Copying from {sourceList.Name} to {destList.Name}...");
            var sourceItems = await Client.Sites.Root.Lists[sourceList.Id].Items.Request().GetAsync();
            foreach (var sourceItem in sourceItems)
            {

                // Read in source drive item
                var sourceDriveItem = await Client.Sites.Root.Lists[sourceList.Id].Items[sourceItem.Id].DriveItem.Request().GetAsync();
                var itemContent = await Client.Sites.Root.Lists[sourceList.Id].Items[sourceItem.Id].DriveItem.Content
                    .Request()
                    .GetAsync();

                // Load item + properties
                var sourceItemFull = await Client.Sites.Root.Lists[sourceList.Id].Items[sourceItem.Id]
                    .Request()
                    .GetAsync();

                // Get source file data
                byte[] fileData = null;

                using (var memoryStream = new System.IO.MemoryStream())
                {
                    itemContent.CopyTo(memoryStream);
                    fileData = memoryStream.ToArray();
                }

                Console.Write($"{sourceDriveItem.Name}...");

                // Write file
                await WriteDestinationFile(sourceDriveItem, sourceItemFull, destList, fileData);
                
            }
        }

        private async Task WriteDestinationFile(DriveItem sourceDriveItem, ListItem sourceItem, List destList, byte[] fileData)
        {
            DriveItem destDriveItem = new DriveItem()
            {
                File = new File(),
                Name = sourceDriveItem.Name,
                ParentReference = new ItemReference()
                {
                    DriveId = destList.ParentReference.DriveId,
                    Path = destList.ParentReference.Path + "/" + sourceItem.Name
                }
            };

            var driveItemsRequest = Client.Sites.Root.Lists[destList.Id].Drive.Items.Request();

            // Add the drive item. Will fail if item already exists
            destDriveItem = await driveItemsRequest.AddAsync(destDriveItem);

            using (System.IO.MemoryStream stream = new System.IO.MemoryStream(fileData))
            {
                // Get all SharePoint list items in destination. Will include our created DriveItem SPListItem
                // We're expanding the "Fields" property in all items so we don't have to individually get each ListItem with a seperate call.
                // In the "Fields" collection of each will be the file-name, but with a single Graph call.
                var destListItems = await Client.Sites.Root.Lists[destList.Id].Items
                    .Request().Expand("Fields").GetAsync();

                bool foundFile = false;
                foreach (var destListItem in destListItems)
                {
                    // Find the file-name from the "Fields" collection, without reading data for each item again.
                    // This reduces dramatically the calls to Graph, but is a little akward to grab the file-name. 
                    object fileNameObj = string.Empty;
                    destListItem.Fields.AdditionalData.TryGetValue("FileLeafRef", out fileNameObj);

                    if (fileNameObj != null && fileNameObj.ToString() == destDriveItem.Name)
                    {
                        foundFile = true;

                        // Does the destination item need any extra fields?
                        Dictionary<string, object> fieldsToAddToDest = GetFieldsOnlyInSource(destListItem.Fields.AdditionalData, sourceItem.Fields.AdditionalData);
                        if (fieldsToAddToDest.Count > 0)
                        {
                            // Update the destination fields
                            var fieldUpdateReq = Client.Sites.Root.Lists[destList.Id].Items[destListItem.Id].Fields.Request();

                            var fields = new FieldValueSet();
                            fields.AdditionalData = fieldsToAddToDest;
                            await fieldUpdateReq.UpdateAsync(fields);
                        }

                        // Upload the content to the driveitem just created
                        var driveItemContentsRequest = Client.Sites.Root.Lists[destList.Id].Items[destListItem.Id]
                            .DriveItem
                            .Content
                            .Request();

                        stream.Position = 0;
                        DriveItem uploadedFile = await driveItemContentsRequest.PutAsync<DriveItem>(stream);

                        // Display output
                        if (fieldsToAddToDest.Count > 0)
                        {
                            Console.WriteLine("done (plus metadata).");
                        }
                        else
                        {
                            Console.WriteLine("done.");
                        }
                        break;
                    }
                }

                // After searching all the destination files, did we find the matching one?
                if (!foundFile)
                {
                    throw new Exception($"Couldn't find the destination file ListItem for source item {sourceItem.Name}");
                }
            }
        }

        private Dictionary<string, object> GetFieldsOnlyInSource(IDictionary<string, object> destFields, IDictionary<string, object> sourceFields)
        {
            Dictionary<string, object> missingDestFields = new Dictionary<string, object>();
            foreach (var sourceFieldName in sourceFields.Keys)
            {
                if (!destFields.ContainsKey(sourceFieldName))
                {
                    missingDestFields.Add(sourceFieldName, sourceFields[sourceFieldName]);
                }
            }

            return missingDestFields;
        }
    }
}

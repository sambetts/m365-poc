
function refreshOneDrive()
{
    callWebApiWithToken("https://graph.microsoft.com/v1.0/me/drive/root/children", "GET", 
        null, 
        function(data)
        {
            // Read file data. Wait for callback to set img source.
            $("#imgOneDriveLoading").addClass("hidden");

            $("#divOneDriveFiles").empty();
            
            ProcessOneDriveData(data.value, $("#divOneDriveFiles"));
        }, 
        null,
        null);
}

function ExpandFolder(elem, id) {
    callWebApiWithToken("https://graph.microsoft.com/v1.0//me/drive/items/" + id + "/children", "GET", 
    null, 
    function(data)
    {
        ProcessOneDriveData(data.value, $(elem).parent());
    }, 
    null,
    null);
}

function ProcessOneDriveData(oneDriveResponse, folderRoot)
{
    var thisRoot = $("<ul />");
    oneDriveResponse.forEach(function(item){
        if (item.folder == null) {

            // File
            var js = "DownloadOneDriveFile(\"" + item['@microsoft.graph.downloadUrl'] + "\")";

            $("<li style='list-style-image: url(img/file.png)'><a href='#' onclick='" + js + "'>" 
                + item.name 
                + "</a></li>").appendTo(thisRoot);
        }
        else {

            // Generate JS call with item ID
            var js = "ExpandFolder(this, \"" + item.id + "\")";

            // Folder
            $("<li style='list-style-image: url(img/folder.png)'><a href='#' onclick='" + js + "'>" 
                + item.name 
                + "</a></li>").appendTo(thisRoot);
        }
    });

    folderRoot.children("ul").empty();
    thisRoot.appendTo(folderRoot);
}

function DownloadOneDriveFile(url) {
    window.location=url;
}
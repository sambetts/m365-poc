
function loadSharePointSites() {
    //logEvent("Loading last 10 emails...");
    callWebApiWithToken("https://graph.microsoft.com/v1.0/sites?search=*", "GET", 
        null, 
        function(data)
        {

            console.log(data);
            
            // Read file data. Wait for callback to set img source.
            $("#imgSharePointSitesLoading").addClass("hidden");

            $("#divSharePointSites").empty();
            
            ProcessSharePointData(data.value, $("#divSharePointSites"));
        }, 
        null,
        null);
}


function ProcessSharePointData(oneDriveResponse, folderRoot)
{
    var thisRoot = $("<ul />");
    oneDriveResponse.forEach(function(item){

        $("<li style='list-style-image: url(img/site.png)'><a href='" + item.webUrl + "' target='_blank'>" 
            + item.displayName 
            + "</a></li>").appendTo(thisRoot);

    });

    folderRoot.children("ul").empty();
    thisRoot.appendTo(folderRoot);
}
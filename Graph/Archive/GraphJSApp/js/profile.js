
function loadProfileAndPhoto() {
    loadProfile();
    refreshPhoto();
}

function loadProfile()
{
    callWebApiWithToken("https://graph.microsoft.com/v1.0/me/", "GET", 
    null, 
    function(data)
    {
        // Read file data. Wait for callback to set img source.
        console.log("Got profile data");
        console.log(data);
        $("#divProfileData").removeClass("hidden");
        $("#divProfileLoading").addClass("hidden");


        $("#profileGivenName").html(data.displayName);
        $("#txtUserEmail").val(data.mail);
        $("#txtUserMobile").val(data.mobilePhone);
        $("#txtOfficeLocation").val(data.officeLocation);
    }, 
    null,
    null);
}

function updateProfile() {

    var mobilePhoneVal = $("#txtUserMobile").val();
    var officeLocationVal = $("#txtOfficeLocation").val();

    if (mobilePhoneVal === "" || officeLocationVal === "")
    {
        alert("Profile fields are mandatory");
        return;
    }

    var body =
    {
        "mobilePhone":  mobilePhoneVal,
        "officeLocation" : officeLocationVal
    }

    callWebApiWithToken("https://graph.microsoft.com/v1.0/me/", "PATCH", 
    JSON.stringify(body), 
    function(data)
    {
        // Read file data. Wait for callback to set img source.
        alert("Updated profile data");
        
        $("#divProfileData").addClass("hidden");
        $("#divProfileLoading").removeClass("hidden");
        loadProfile();
    }, 
    null,
    null);
}

function showProfileEdit()
{
    refreshPhoto();
}

function refreshPhoto()
{
    callWebApiWithToken("https://graph.microsoft.com/v1.0/me/photo/$value", "GET", 
        null, 
        function(data)
        {
            // Read file data. Wait for callback to set img source.
            var reader = new FileReader();
            reader.onload = function(readerEvt) {
                var binaryString = readerEvt.target.result;
                var fileEncoded = window.btoa(binaryString);
                $("#imgProfile").attr("src", "data:image/jpeg;base64," + fileEncoded);
                $("#imgProfile").removeClass("hidden");
                $("#imgUserLoading").addClass("hidden");
            };
            reader.readAsBinaryString(data);

        }, 
        null,
        null);
}


// https://developer.microsoft.com/en-us/graph/docs/api-reference/v1.0/api/profilephoto_update
function uploadPhoto(event)
{
    var fileData = $('#fileUpload').prop('files');
    if (fileData.length === 1)
    {
        console.log("Uploading new photo...");
        callWebApiWithToken("https://graph.microsoft.com/v1.0/me/photo/$value", "PUT", 
            fileData[0], 
            function()
            {
                alert("Upload complete.")
                refreshPhoto();
            }, 
            null,
            null);
    }
    else
    {
        alert("Select a file.");
    }

    // Stop post-back
    //event.stopPropagation();

    return false;
}

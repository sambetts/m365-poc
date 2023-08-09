


// Do things once the page has fully loaded
window.onload = function () {

    if (!window.File || !window.FileReader || !window.FileList || !window.Blob) {
        alert("The File APIs are not fully supported in this browser. This example won't work.");
        return;
      }   

      
    // Show logon tab
    showDefaultTab();

    // Show app container & hide loading
    $("#appMainPanel").removeClass("hidden");
    $("#appLoadingContainer").addClass("hidden");
    
};

function logEvent(msg)
{
    var $actionLog = $("<p />");
    $actionLog.text(msg);
    $actionLog.appendTo($("#graphLog"));
    $("#graphLog").animate({ scrollTop: $('#graphLog').prop("scrollHeight")}, 1000);
}

function showDefaultTab()
{
    selectTab($(".nav.nav-tabs > li > a[name='divAuthentication']"));   // Show login tab by default
}

/**
 * Changes a tab
 * @param {*} clickedTabLink - a link of the clicked tab
 */
function selectTab(clickedTabLink)
{
    var divs = [];
    // Read all tab links to get div names
    $("#navMain.nav.nav-tabs > li > a").each(function()
    { 
        divs.push($(this).attr("name"));
    });

    // Hide all the tabs
    $(divs).each(function() {$("#" + this).addClass("hidden");});


    // Show selected DIV
    var selectedTabDiv = $(clickedTabLink).attr("name");
    $("#" + selectedTabDiv).removeClass("hidden")

    // Clear active tab class
    $("#navMain.nav-tabs li").removeClass("active");
    $(clickedTabLink).parent().addClass("active");

    // Do things
    if (selectedTabDiv == "divAuthentication")
    {
        getLogonStatus();
    }
    else if (selectedTabDiv == "divProfile")
    {
        loadProfileAndPhoto();
    }
    else if(selectedTabDiv == "divOneDrive")
    {
        refreshOneDrive();
    }
    else if(selectedTabDiv == "divOutlook")
    {
        refreshOutlook();
    }
    else if(selectedTabDiv == "divPlanner")
    {
        refreshPlanner();
    }
    else if(selectedTabDiv == "divSharePoint")
    {
        loadSharePointSites();
    }
    else 
    {
        alert('Not sure which tab to select!');
    }
}

function toggleGraphLog() {
    if ($("#graphLogContent").hasClass("hidden"))
    {
        $("#graphLogContent").removeClass("hidden");
    }
    else
    {
        $("#graphLogContent").addClass("hidden");
    }
}


/**
 * Call a Web API using an access token.
 * 
 * @param {any} endpoint - Web API endpoint
 * @param {any} token - Access token
 * @param {any} payloadString - body to include
 * @param {any} callbackFunction - function to call when data loaded
 * @param {any} pageElem - DOM element to include to passing loaded function
 * @param {any} eTag - eTag for data updates
 */
function callWebApiWithToken(endpoint, method, payloadString, callbackFunction, pageElem, eTag) {
    var headers = new Headers();

    // Add OAuth token
    var bearer = "Bearer " + oAuthToken;
    headers.append("Authorization", bearer);

    // Add versioning header if needed
    if (eTag)
    {
        headers.append("If-Match", eTag);
    }

    // Build request options
    var options = null;
    if (payloadString !== null)
    {
        headers.append("Content-type", "application/json");
        options = {
            method: method,
            headers: headers,
            body: payloadString
        };
    }
    else
    {
        options = {
            method: method,
            headers: headers
        };
    }

    // Logging
    logEvent("'" + method + "' to " + endpoint);

    fetch(endpoint, options)
        .then(function (response) {
            var contentType = response.headers.get("content-type");

            // Success codes == 2xx
            if (response.status >= 200 && response.status < 300) {

                // Response is jSon?
                if (contentType && contentType.indexOf("application/json") !== -1)
                {
                    response.json()
                        .then(function (data) {
                            // Display response in the page
                            console.log(endpoint + " response received.");

                            // Return response
                            callbackFunction(data, pageElem);
                        })
                        .catch(function (error) {
                            showError(endpoint, error);
                        });
                }
                else if (contentType && contentType.indexOf("image/jpeg") !== -1)
                {
                    response.blob()
                    .then(function(data)
                    {
                        callbackFunction(data);
                    });
                }
                else
                {
                    // Assume no data needed to callback
                    callbackFunction();
                }
            } else {
                response.json()
                    .then(function (data) {
                        // Display response as error in the page
                        showError(endpoint, data);
                    })
                    .catch(function (error) {
                        showError(endpoint, error);
                    });
            }
        })
        .catch(function (error) {
            showError(endpoint, error);
        });
}

/**
 * Sign-out the user
 */
function signOut() {
    userAgentApplication.logout();
}


/**
 * Show an error message in the page
 * @param {string} endpoint - the endpoint used for the error message
 * @param {string} error - error
 * @param {string} errorDesc - the error string
 */
function showError(endpoint, error, errorDesc) {
    var formattedError = JSON.stringify(error, null, 4);
    if (formattedError.length < 3) {
        formattedError = error;
    }
    console.error(error);

    // Logging
    var $actionLog = $("<p class='text-danger'/>");
    $actionLog.text("Error: '" + formattedError + "'.");
    $actionLog.appendTo($("#graphLog"));
    $("#graphLog").animate({ scrollTop: $('#graphLog').prop("scrollHeight")}, 1000);
}

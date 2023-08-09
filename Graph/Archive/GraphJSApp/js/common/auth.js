
// Graph API scopes needed for the things we want to do in Graph
var defaultAPIScopes = ["User.ReadWrite.All", "Files.Read", "Mail.Read", "Mail.Send", "Group.ReadWrite.All", "Sites.Read.All"];


// Initialize application
var userAgentApplication = new Msal.UserAgentApplication(msalconfig.clientID, null, loginCallback, {
    redirectUri: msalconfig.redirectUri
});

// Previous version of msal uses redirect url via a property
if (userAgentApplication.redirectUri) {
    userAgentApplication.redirectUri = msalconfig.redirectUri;
}

function getLogonStatus()
{
    var userLoggedIntoMSAL = false;
    // If we've been redirected back from MSAL...
    if (!userAgentApplication.isCallback(window.location.hash) && window.parent === window && !window.opener) {
        var user = userAgentApplication.getUser();

        // See if we have a user
        if (user) {
            userLoggedIntoMSAL = true;
        }
    }

    setConfiguredScopes();
    if (userLoggedIntoMSAL)
    {
        // Switch display
        $("#divLogIn").addClass("hidden");
        $("#divLoggedIn").removeClass("hidden");
        
        // Get OAuth token
        if (!this.oAuthToken)
            getOAuthTokenFromUserLogin();

        // Display user
        $("#lblYouAre").html(user.name);

        // Disable UI for modifying scopes
        $("#divPermissions tbody tr td:nth-child(2)").remove();
        $("#divPermissions tfoot").remove();

        // Show all tabs
        $("#navMain.nav.nav-tabs > li > a[name!='divAuthentication']").removeClass("hidden");
    }
    else
    {
        // Hide "not login" tabs
        $("#navMain.nav.nav-tabs > li > a[name!='divAuthentication']").addClass("hidden");

        // Show client ID
        $("#txtClientId").val(msalconfig.clientID);
    }
}



/**
 * Begin the login process if no user token yet, or get OAuth token if there is
 */
function getOAuthTokenFromUserLogin() {
    var user = userAgentApplication.getUser();

    // Get configured scopes
    var configuredScopes = getConfiguredScopes();

    if (!user) {

        logEvent("AUTH: No user token for user. Must login to Azure AD & redirect back successfully.");

        // If user is not signed in, then prompt user to sign in via loginRedirect.
        // This will redirect user to the Azure Active Directory v2 Endpoint
        userAgentApplication.loginRedirect(configuredScopes);

    } else {

        logEvent("AUTH: Have user token from Azure AD. Requesting OAuth token for permissions: " + configuredScopes + "...");

        // In order to call the Graph API, an access token needs to be acquired.
        // Try to acquire the token used to Query Graph API silently first
        userAgentApplication.acquireTokenSilent(configuredScopes)
            .then(function (token) {

                logEvent("AUTH: Got OAuth token: " + token);

                // Save token
                this.oAuthToken = token;

            }, function (error) {
                // If the acquireTokenSilent() method fails, then acquire the token interactively via acquireTokenRedirect().
                // In this case, the browser will redirect user back to the Azure Active Directory v2 Endpoint so the user 
                // can re-type the current username and password and/ or give consent to new permissions your application is requesting.
                // After authentication/ authorization completes, this page will be reloaded again and getOAuthTokenFromUserLogin() will be called.
                // Then, acquireTokenSilent will then acquire the token silently, the Graph API call results will be made and results will be displayed in the page.
                if (error) {
                    userAgentApplication.acquireTokenRedirect(configuredScopes);
                }
            });
    }
}

// Sets perms table from cookie or defaults
function setConfiguredScopes() {

    var configuredPermsJson = Cookies.get("contosodashboard");
    var configuredPerms = [];

    if (!configuredPermsJson)
    {
        configuredPerms = defaultAPIScopes;
    }
    else
    {
        configuredPerms = JSON.parse(configuredPermsJson);
    }

    $("#divPermissions tbody").empty();

    // Build perms display
    configuredPerms.forEach(function(val) {
        var permRow = $("<tr/>").appendTo("#divPermissions tbody");
        permRow.append("<td>" + val + "</td><td><a href='#' onclick='removePerm(this)'>remove</a></td>");
    });

    saveConfiguredScopesInCookie();
}

function resetConfiguredScopes() {
    Cookies.set("contosodashboard", JSON.stringify(defaultAPIScopes));
    setConfiguredScopes();
}

function saveConfiguredScopesInCookie() {
    var perms = getConfiguredScopes();
    // Save perms in cookie
    Cookies.set("contosodashboard", JSON.stringify(perms));
}

function getConfiguredScopes() {

    var perms = [];
    $("#divPermissions tbody tr td:first-child").each(function () {
        perms.push($(this).html());
    });

    return perms;
}

function removePerm(source) {
    $(source).parent().parent().remove();
    saveConfiguredScopesInCookie();
}

function addPermission() {
    var perm = $("#txtAddPerm").val();
    if (perm === null || perm === "")
    {
        alert("Type something");
        return;
    }

    $("#divPermissions tbody").append("<tr><td>" + perm + 
        "</td><td>" + 
            "<input type='button' class='btn btn-sm btn-danger' value='X' onclick='removePerm(this)'>" + 
        "</td></tr>"
    );
    saveConfiguredScopesInCookie();
}


/**
 * Callback method from sign-in: if no errors, call getOAuthTokenFromUserLogin() to show results.
 * @param {string} errorDesc - If error occur, the error message
 * @param {object} token - The token received from login
 * @param {object} error - The error 
 * @param {string} tokenType - The token type: For loginRedirect, tokenType = "id_token". For acquireTokenRedirect, tokenType:"access_token"
 */
function loginCallback(errorDesc, token, error, tokenType) {
    if (errorDesc) {
        showError(msal.authority, error, errorDesc);
    } else {
        getOAuthTokenFromUserLogin();
    }
}
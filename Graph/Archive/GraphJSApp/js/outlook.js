var emailCache = [];

function refreshOutlook()
{
    // Get users' email address
    logEvent("Loading logged-in users' email address...");
    callWebApiWithToken("https://graph.microsoft.com/v1.0/me/", "GET", 
        null, 
        function(data)
        {
            $("#txtEmailFromUser").val(data.mail);
        }, 
        null,
        null);

    logEvent("Loading last 10 emails...");
    callWebApiWithToken("https://graph.microsoft.com/v1.0/me/mailFolders/inbox/messages?$top=10", "GET", 
        null, 
        function(data)
        {
            // Read file data. Wait for callback to set img source.
            console.log(data);

            // Reset cache
            emailCache.length = 0;
            $("#divEmailList").empty();
            $("#emailSelectedMessage").removeClass("hidden");
            
            var emailListRoot = $("<div id='emails' />");

            data.value.forEach(function(item)
            {
                emailCache.push(item);

                var emailItem = $("<div class='emailListItem' onclick='LoadEmailContent(this)'>").appendTo(emailListRoot);
                $("<div class='emailListSubject'>" + item.subject + "</div>" + 
                    "<div class='emailListFrom'>" + item.sender.emailAddress.address + "</div>")
                    .appendTo(emailItem);

                emailItem.attr("data-email-id", item.id);
                emailItem.appendTo(emailListRoot);
            });



            emailListRoot.appendTo($("#divEmailList"));
        }, 
        null,
        null);
}

$("#divSendEmail").dialog({
	autoOpen: false,
	width: 500,
	buttons: [
		{
			text: "Send",
			click: function() {
                SendEmail();
				//$(this).dialog("close");
			}
		},
		{
			text: "Cancel",
			click: function() {
				$(this).dialog("close");
			}
		}
	]
});

function LoadEmailContent(emailElem) {
    
    var id = $(emailElem).attr("data-email-id");
    var selectedEmail = null;
    emailCache.forEach(function(email) {
        if (email.id === id)
        {
            selectedEmail = email;
            return;
        }
    });
    if (selectedEmail)
    {
        $("#emailSelectMessage").remove();
        $("#emailFrame").removeClass("hidden");
        var emailFrame = $("#emailFrame").contents().find('html');
        
        emailFrame.html(selectedEmail.body.content);

    }
    else
    {
        alert("Unexpected: Can't find the selected email!");
    }
}

function SendEmail()
{
    var from = $("#txtEmailFromUser").val();
    var to = $("#txtEmailToUser").val();

    if (!validateEmail(from))
    {
        alert("'From' address not valid.");
        return;
    }
    if (!validateEmail(to))
    {
        alert("'To' address not valid.");
        return;
    }

    // Build POST body
    var emailBody =
    {
        "message": {
            "subject": $("#txtEmailSubject").val(),
            "body": {
                "contentType": "Text",
                "content": $("#txtEmailBody").val()
            },
            "toRecipients": [
            {
                "emailAddress": {
                "address": to
                }
            }
            ],
            "from" :
            {
                "emailAddress": {
                    "address": from
                  }
            }
        }
    }

    // Send email - https://docs.microsoft.com/en-us/graph/outlook-send-mail-from-other-user
    callWebApiWithToken("https://graph.microsoft.com/v1.0/me/sendmail", "POST", 
    JSON.stringify(emailBody), 
    function(data)
    {
        alert("Email sent!");
        $("#txtEmailBody").val("");
        $("#txtEmailSubject").val("");
        $("#divSendEmail").dialog("close");
    }, 
    null,
    null);
}

function validateEmail(email) {
    var re = /^(([^<>()\[\]\\.,;:\s@"]+(\.[^<>()\[\]\\.,;:\s@"]+)*)|(".+"))@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\])|(([a-zA-Z\-0-9]+\.)+[a-zA-Z]{2,}))$/;
    return re.test(String(email).toLowerCase());
}
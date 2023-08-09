
function refreshPlanner()
{
    $("#plans").empty();

    //After the access token is acquired, call the Web API, sending the acquired token
    callWebApiWithToken(
        "https://graph.microsoft.com/v1.0/me/memberOf/$/microsoft.graph.group?$filter=groupTypes/any(a:a eq 'unified')", 
        "GET",
        null,
        parseGroups,
        $("#plans",
        null)
    );
}




function parseGroups(groupJson, pageElemAlink)
{    
    // Remove hidden container
    $("#plansContainer").removeClass("hidden");

    // Hide loading
    $("#imgPlannerLoading").addClass("hidden");

    // Start groups tree. Add all elements to DIV passed in
    $("<ul />", { id: "groupsTree"}).appendTo(pageElemAlink);
    for (let i = 0; i < groupJson.value.length; i++) {
        const groupData = groupJson.value[i];
        
        // Add new link to new LI, and add each LI to UL
        var link = $("<a href='javascript:void(0);'>" + groupData.displayName + "</a>", { id: "group-" + groupData.id})
            .appendTo("<li/>").parent().appendTo("#groupsTree").children();

        // Remember group ID
        link.data("groupId", groupData.id);

        // On click, load data & pass to load function
        link.click(function(event) 
        { 
            var groupId = $(this).data("groupId");
            
            var requestUrl = "https://graph.microsoft.com/v1.0/groups/" + groupId + "/planner/plans";
            callWebApiWithToken(requestUrl, "GET", null, parsePlansInGroup, $(this), null);

            event.stopPropagation();
        });
    }
}

function parsePlansInGroup(plansJson, pageElemAlink)
{
    // Append groups tree to parent LI (pageElemAlink in the A inside the LI)
    var planTree = $("<ul />").appendTo(pageElemAlink.parent());

    
    // Hide edit box
    $("#editBox").addClass("hidden");

    if (plansJson.value.length > 0)
    {
        for (let i = 0; i < plansJson.value.length; i++) {
            const planData = plansJson.value[i];
            
            // Add new link to new LI, and add each LI to UL
            var link = $("<a href='#'>" + planData.title + "</a>").appendTo("<li/>").parent().appendTo(planTree).children();

            // Remember ID
            link.data("planId", planData.id);

            // On click, load data & pass to load function
            link.click(function(event) 
            { 
                var planId = $(this).data("planId");
                var requestUrl = "https://graph.microsoft.com/v1.0/planner/plans/" + planId + "/tasks";
                callWebApiWithToken(requestUrl, "GET", null, parsePlanTasks, $(this), null);

                // Update GUI & create event-handler for creating new event
                $("#currentPlanSelection").html("plan ID '" + planId + "'. ");
                $("#currentPlanSelection").append($("<a href='#'>Create new task</a>")).click( function(){createNewTask(planId)});
                logEvent("Plan selected. You can now edit/create tasks in this plan.")

                // Remember Plan ID for creating new tasks
                lastPlanID = planId;

                event.stopPropagation();
            });
        }
    }
    else
    {
        $(pageElemAlink).parent().find("ul").remove();
        var link = $("<ul><li>No plans in group</li></ul>").appendTo($(pageElemAlink).parent());
    }
}

function parsePlanTasks(tasksJson, pageElemAlink)
{
    // Append groups tree to parent LI (pageElemAlink in the A inside the LI)
    var planTasksTree = $("<ul />").appendTo(pageElemAlink.parent());

    // Hide edit box
    $("#editBox").addClass("hidden");

    for (let i = 0; i < tasksJson.value.length; i++) {
        const planTaskData = tasksJson.value[i];
        
        // Add new link to new LI, and add each LI to UL
        var link = $("<a href='#'>" + planTaskData.title + "</a>").appendTo("<li/>").parent().appendTo(planTasksTree).children();

        // Remember ID
        link.data("taskId", planTaskData.id);

        // On click, load data & pass to load function
        link.click(function(event) 
        { 
            var taskId = $(this).data("taskId");
            var requestUrl = "https://graph.microsoft.com/v1.0/planner/tasks/" + taskId;
            callWebApiWithToken(requestUrl, "GET", null, editTask, $(this), null);

            event.stopPropagation();
        });
    }
}


var oAuthToken = null; // Cached OAuth token
var lastTaskID = null; // Last task ID that was clicked on
var lastPlanID = null; // Last plan ID that was clicked on
var lastEtag = null;   // eTag of the last task edited. Needed up updating.
function createNewTask()
{
    // Show GUI
    $("#editBox").removeClass("hidden");
    $("#editBoxHeaderPlanner").html("Create new Task");

    // Reset fields
    $("#txtTaskTitle").val("");
    $("#txtTaskDescription").val("");
    
    lastTaskID = null;
}

function closeEditScreen()
{
    $("#editBox").addClass("hidden");
}
function editTask(taskDetailJson)
{
    // Set GUI
    $("#editBoxHeaderPlanner").html("Edit Task");
    $("#editBox").removeClass("hidden");

    // Update fields
    $("#txtTaskTitle").val(taskDetailJson.title);
    
    // Remember state for edit
    lastTaskID = taskDetailJson.id;
    lastEtag = taskDetailJson["@odata.etag"];

    console.log(taskDetailJson);
}

function saveCurrentTask(event)
{
    // Create or update?
    if(lastTaskID !== null)
    {
        // Update existing...
        var planBody =
        {
            "title":  $("#txtTaskTitle").val()
        }
        console.log("Updating task ID '" + lastTaskID + "'.");
        callWebApiWithToken("https://graph.microsoft.com/v1.0/planner/tasks/" + lastTaskID, "PATCH", 
            JSON.stringify(planBody), 
            saveComplete, 
            null,
            lastEtag);
    }
    else 
    {
        // Create new...
        var planBody =
        {
            "planId": lastPlanID,
            "title":  $("#txtTaskTitle").val(),
            "assignments": {}
        }
        console.log("Create new task for '" + lastPlanID + "'.");

        callWebApiWithToken("https://graph.microsoft.com/v1.0/planner/tasks", "POST", 
            JSON.stringify(planBody), 
            saveComplete, 
            null,
            null);
    }
    $("#editBox").addClass("hidden");

    // Stop post-back
    event.stopPropagation();

    return false;
}

function saveComplete()
{
    alert("Save successful!");
    refreshPlanner();
}

function StartNotificationsHandler() {
    //alert("Notifications Handler installed");
    Notification.requestPermission().then((permission) => {
        console.log("Global Panel Refresh Rate :", 5, " seconds");
        // retreive notifications from sessionStorage
        let notifications = JSON.parse(sessionStorage.getItem("Notifications"));
        if (notifications == null)
            sessionStorage.setItem("Notifications", JSON.stringify([]));
        else {
            notifications.forEach((notification) => {
                addNotification(notification.date, notification.message, false);
            })
        }
        setInterval(function () {
            $.ajax({
                url: "/Notifications/Pop",
                success: notification => {
                    if (notification != null) {
                        var icon = "/WebApp.png";
                        var title = "Web Application";
                        var message = notification.Message;
                        var avatar = notification.Avatar;
                        if (permission === "granted")
                            new Notification(title, { body:message, icon });

                        let date = new Date().toLocaleString("fr-FR");
                        message = message.replace("[", "<span></span><span>[").replace("]","]</span>");
                        message = `<div class="UserSmallAvatar transparentBackground" style="background-image: url('${avatar}'); " title="Nicolas Chourot"></div>${message}`;
                        addNotification(date, message);

                        // store notification in sessionStorage
                        let notifications = JSON.parse(sessionStorage.getItem("Notifications"));
                        notifications.unshift({ date, message });
                        sessionStorage.setItem("Notifications", JSON.stringify(notifications));
                    }
                }
            })
        }, 5 * 1000);
    });
}


function addNotification(date, message, prepend = true) {

    let notification = $(`
        <div style="border:1px solid #ccc; border-radius:6px; background-color:#ccc; margin:4px; padding:4px;!important; width:355px !important">
        <div style="display:grid; grid-template-columns: 40px auto; align-items:center">
            ${message}
            <div><!--spacer--></div><div style="text-align:right; margin-top:6px;border-bottom:1px solid #ccc;font-size:0.7em; color:dodgerblue">${date}</div>
        </div>
    `);
    if (prepend)
        $("#notificationsPanel").prepend(notification);
    else
        $("#notificationsPanel").append(notification);
    $("#notificationsIcon").addClass("fa-solid");
    $("#notificationsIcon").removeClass("fa-regular");

    $("#notificationsIcon").off();

    $("#notificationsIcon").on("click", function () {
        $("#notificationsIcon").removeClass("fa-solid");
        $("#notificationsIcon").addClass("fa-regular");
    });


}
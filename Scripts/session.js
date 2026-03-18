/////////////////////////////////////////////////////////////////////////////////////////////////////////
//
// Author: Nicolas Chourot
// 2026
//
// Dependances :
//     - jquery version > 3.0
//     - popup.css
//
////////////////////////////////////////////////////////////////////////////////////////////////////////
//
// Todo : complete SessionManager class, add set expiredSessionHandlerUrl(url) /Accounts/ExpiredSession
const infinite = -1;
let session = null;
function newSession(timeout) {
    session = new Session(timeout);
}
class Session {
    constructor(stallingTime = infinite, timeoutCallBack_URL = "/Accounts/ExpiredSession") {
        this.maxStallingTime = stallingTime;
        this.timeBeforeRedirect = 5;
        this.currentTimeouID = null;
        this.timeLeft = this.maxStallingTime;
        this.setTimeoutCallBack_URL(timeoutCallBack_URL);
        this.createTimeoutPopup();
        this.startCountdown();
    }
    setTimeoutCallBack_URL(timeoutCallBack_URL) {
        this.timeoutCallBack_URL = timeoutCallBack_URL;
        this.timeoutCallBack = () => { window.location.replace(this.timeoutCallBack_URL); };
    }
    startCountdown() {
        clearTimeout(this.currentTimeouID);
        $(".popup").hide();
        this.timeLeft = this.maxStallingTime;
        if (this.timeLeft != infinite) {
            this.currentTimeouID = setInterval(() => {
                this.timeLeft = this.timeLeft - 1;
                if (this.timeLeft > 0) {
                    if (this.timeLeft <= 10) {
                        $(".popup").show();
                        $("#popUpMessage").text("Expiration dans " + this.timeLeft + " secondes");
                    }
                } else {
                    $("#popUpMessage").text('Redirection dans ' + (this.timeBeforeRedirect + this.timeLeft) + " secondes");
                    if (this.timeLeft <= -this.timeBeforeRedirect) {
                        clearTimeout(this.currentTimeouID);
                        this.closePopup();
                        this.timeoutCallBack();
                    }
                }
            }, 1000);
        }
    }
    createTimeoutPopup() {
        $('body').append(`
        <div class='popup'> 
            <div class='popupContent'>
                <div>
                    <div class='popupHearder'> Attention!</div> 
                    <h4 id='popUpMessage'></h4>
                </div>
                <div id='closePopup' onclick='session.closePopup(); ' class='close-btn fa fa-close'></div> 
            </div>
           
        </div> 
    `);
    }

    closePopup() {
        $(".popup").hide();
        session.startCountdown();
    }
}

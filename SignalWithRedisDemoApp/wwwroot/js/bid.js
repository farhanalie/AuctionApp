"use strict";
$(function () {

    var items = [];

    var connection = new signalR.HubConnectionBuilder().withUrl("/bidhub").build();

    //Disable send button until connection is established
    $("#placeButton").prop("disabled", true);

    connection.on("ReceiveBid", function (bid) {
        console.log(bid);

        $("#bidsTable > tbody").prepend(`<tr>
                    <td>£ ${bid.amount}</td>
                    <td>${bid.userId}</td>
                    <td>${bid.createdAt}</td>
                </tr>`);
        //var user = bid.user;
        //var message = bid.message;
        //var msg = message.replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;");
        //var encodedMsg = user + " says " + msg;
        //var li = document.createElement("li");
        //li.textContent = encodedMsg;
        //document.getElementById("messagesList").appendChild(li);
    });

    connection.start().then(function () {
        console.log("connection established");
        $("#placeButton").prop("disabled", false);
    }).catch(function (err) {
        return console.error(err.toString());
    });

    $("#signupButton").click(function () {
        $("#userId").prop("disabled", true);
        $("#maxBid").prop("disabled", true);
        $("#bid-container").show();
        $(this).hide();
    });

    $("#placeButton").click(function () {

        const userId = $("#userId").val();
        const amount = $("#amount").val();

        const bid = {
            UserId: userId,
            Amount: Number(amount)
        };

        connection.invoke("placeBid", bid).catch(function (err) {
            return console.error(err.toString());
        });

        $("#amount").val("");
        event.preventDefault();
    });

});
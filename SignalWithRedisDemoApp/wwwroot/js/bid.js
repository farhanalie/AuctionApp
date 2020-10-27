"use strict";
$(function () {

    var items = [];

    var connection = new signalR.HubConnectionBuilder().withUrl("/bidhub").build();

    //Disable send button until connection is established
    $("#placeButton").prop("disabled", true);

    connection.on("ReceiveBid", function (bid) {
        console.log(bid);

        $("#bidsList").prepend(`<li class="list-group-item"><span class="font-weight-bold">${bid.userId}</span> ${bid.createdAt} <span class="badge badge-primary badge-pill">£${bid.amount}</span></li>`);
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




    $("#connectButton").click(function () {

        $("#userId").text($("#userInput").val());
        $("#user-container").hide();
        $("#bid-container").show();

    });

    $("#placeButton").click(function () {

        const userId = $("#userId").text();
        const amount = $("#amount").val();

        const bid = {
            UserId: userId,
            Amount: Number(amount)
        };

        connection.invoke("placeBid", bid).catch(function (err) {
            return console.error(err.toString());
        });
        event.preventDefault();
    });

});
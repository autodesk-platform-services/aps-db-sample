/////////////////////////////////////////////////////////////////////
// Copyright (c) Autodesk, Inc. All rights reserved
// Written by Forge Partner Development
//
// Permission to use, copy, modify, and distribute this software in
// object code form for any purpose and without fee is hereby granted,
// provided that the above copyright notice appears in all copies and
// that both that copyright notice and the limited warranty and
// restricted rights notice below appear in all supporting
// documentation.
//
// AUTODESK PROVIDES THIS PROGRAM "AS IS" AND WITH ALL FAULTS.
// AUTODESK SPECIFICALLY DISCLAIMS ANY IMPLIED WARRANTY OF
// MERCHANTABILITY OR FITNESS FOR A PARTICULAR USE.  AUTODESK, INC.
// DOES NOT WARRANT THAT THE OPERATION OF THE PROGRAM WILL BE
// UNINTERRUPTED OR ERROR FREE.
/////////////////////////////////////////////////////////////////////

var connection = new signalR.HubConnectionBuilder().withUrl("/dbhub").build();

connection.on("ReceiveProperties", function (selecteddbId, properties) {
  addProperties(selecteddbId, properties).then(() => {
    $("div.ready").fadeIn(500).delay(1500).fadeOut(500);
  }).catch((err) => {
    console.log(err);
    $("div.failure").fadeIn(500).delay(1500).fadeOut(500);
  });
});

connection.on("ReceiveUpdate", function (selecteddbId, updateResult, message) {
  showUpdateResult(selecteddbId, updateResult);
  console.log(message);
});

connection.on("ReceiveModification", function (selecteddbId, properties, urn) {
  let disableNotification = $('#disablenotifications')[0].checked;
  if (urn.replaceAll('=', '') === _viewer.model.getSeedUrn() && !disableNotification) {
    addProperties(selecteddbId, properties);
    showNotification(selecteddbId);
  }
});

connection.start().then(function () {
  //No function for now
}).catch(function (err) {
  return console.error(err.toString());
});
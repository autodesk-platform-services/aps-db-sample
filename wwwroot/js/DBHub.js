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
  //viewer.model.getExternalIdMapping(idsMap => addProperties(idsMap, selecteddbId, properties));
  addProperties(selecteddbId, properties);
});

connection.on("ReceiveUpdate", function (selecteddbId, updateResult, message) {
  //viewer.model.getExternalIdMapping(idsMap => showUpdateResult(idsMap, externalId, updateResult));
  showUpdateResult(selecteddbId, updateResult)
  console.log(message);
});

connection.on("ReceiveModification", function (selecteddbId, properties, urn) {
  let receiveNotification = true;
  if (urn === viewer.model.getSeedUrn() && receiveNotification) {
    addProperties(selecteddbId, properties);
    showNotification(selecteddbId);
  }
});

connection.start().then(function () {
  //No function for now
}).catch(function (err) {
  return console.error(err.toString());
});
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

connection.on("ReceiveProperties", function (externalId, properties) {
  viewer.model.getExternalIdMapping(idsMap => addProperties(idsMap, externalId, properties));
});

async function addProperties(idsMap, externalId, properties) {
  let dbId = idsMap[externalId];
  let ext = viewer.getExtension('DBPropertiesExtension');

  if (!ext.panel.properties[dbId]) {
    ext.panel.properties[dbId] = {
      "Properties From DB": {

      }
    };
  }

  for (const property of Object.keys(properties)) {
    ext.panel.properties[dbId]["Properties From DB"][property] = properties[property];
  }
  $("div.ready").fadeIn(500).delay(2000).fadeOut(500);
}


connection.start().then(function () {
  //No function for now
}).catch(function (err) {
  return console.error(err.toString());
});
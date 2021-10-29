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

// *******************************************
// DB Property Panel
// *******************************************
class DBPropertyPanel extends Autodesk.Viewing.Extensions.ViewerPropertyPanel {
  constructor(viewer, options) {
    super(viewer, options);
    this.properties = options.properties || {};
    this.currentText = "";
    this.currentProperty = null;

    //This is the event for property click
    Autodesk.Viewing.UI.PropertyPanel.prototype.onPropertyClick = this.onPropertyClick;
    //This in the event for object selection changes
    viewer.addEventListener(Autodesk.Viewing.SELECTION_CHANGED_EVENT, this.queryProps.bind(this));
    //This is for pressing key down
    document.addEventListener('keydown', this.onKeyDown.bind(this));
  }

  queryProps() {
    viewer.getProperties(viewer.getSelection(), data => this.queryDB(data));
  }

  queryDB(data) {
    let externalId = data.externalId;
    externalId ? extractDBData(externalId) : $("div.failure").fadeIn(500).delay(2000).fadeOut(500);
  }

  setAggregatedProperties(propertySet) {
    Autodesk.Viewing.Extensions.ViewerPropertyPanel.prototype.setAggregatedProperties.call(this, propertySet);

    // add your custom properties here
    const dbids = propertySet.getDbIds();
    dbids.forEach(id => {
      this.setdbIdProperties(id);
    });
  }

  setdbIdProperties(dbId) {
    var propsForObject = this.properties[dbId.toString()];
    if (propsForObject) {
      for (const groupName in propsForObject) {
        const group = propsForObject[groupName];
        for (const propName in group) {
          const prop = group[propName];
          this.addProperty(propName, prop, groupName);
        }
      }
    }
  }

  onKeyDown(event) {
    console.log(event);
    if (!!this.currentProperty) {
      switch (event.keyCode) {
        case 8:
          this.currentText = "";
          this.updateCurrentProperty();
          break;
        case 13:
          this.sendChanges();
          break;
        default:
          this.currentText += event.key;
          this.updateCurrentProperty();
          break;
      }
    }
    event.handled = true;
  }

  sendChanges() {
    const requestUrl = '/api/dbconnector';
    const requestData = {
      'connectionId': connection.connection.connectionId,
      'dbProvider': $('#dboptions').find(":selected").text()
    };
    apiClientAsync(requestUrl, requestData);
    $("div.gathering").fadeIn(500).delay(2000).fadeOut(500);
    alert("Changes sent to DB!");
  }

  async updateCurrentProperty() {
    try {
      // await this.removeProperty(this.currentProperty.name, this.currentProperty.value, this.currentProperty.category);
    }
    catch { }
    // await this.removeProperty(this.currentProperty.name.slice(), this.currentProperty.value.slice(), this.currentProperty.category.slice());
    await this.addProperty(this.currentProperty.name, this.currentText, this.currentProperty.category);
    this.currentProperty.value = this.currentText;
  }

  onPropertyClick(property, event) {
    this.currentProperty = property;
    this.currentText = property.value;
    console.log("Current property changed to " + property.name + " = " + property.value);
  }
};

//Here we add the properties aquired from DB to the proper dbid proper property panel
async function addProperties(idsMap, externalId, properties) {
  let dbId = idsMap[externalId];
  let ext = viewer.getExtension('DBPropertiesExtension');

  if (!ext.panel.properties[dbId]) {
    ext.panel.properties[dbId] = {
      "Properties From DB": {

      }
    };
    for (const property of Object.keys(properties)) {
      ext.panel.properties[dbId]["Properties From DB"][property] = properties[property];
    }
    ext.panel.setdbIdProperties(dbId);
  }

  $("div.ready").fadeIn(500).delay(2000).fadeOut(500);
}

//Here we reach the server endpoint to retrieve the proper data from DB
async function extractDBData(externalId) {
  try {
    const requestUrl = '/api/dbconnector';
    const requestData = {
      'connectionId': connection.connection.connectionId,
      'externalId': externalId,
      'dbProvider': $('#dboptions').find(":selected").text()
    };
    apiClientAsync(requestUrl, requestData);
    $("div.gathering").fadeIn(500).delay(2000).fadeOut(500);
  }
  catch (err) {
    console.log(err);
    $("div.failure").fadeIn(500).delay(2000).fadeOut(500);
  }
}

// helper function for Request
function apiClientAsync(requestUrl, requestData = null, requestMethod = 'get') {
  let def = $.Deferred();

  if (requestMethod == 'post') {
    requestData = JSON.stringify(requestData);
  }

  jQuery.ajax({
    url: requestUrl,
    contentType: 'application/json',
    type: requestMethod,
    dataType: 'json',
    data: requestData,
    success: function (res) {
      def.resolve(res);
    },
    error: function (err) {
      console.error('request failed:');
      def.reject(err)
    }
  });
  return def.promise();
}

// *******************************************
// DB Properties Extension
// *******************************************
class DBPropertiesExtension extends Autodesk.Viewing.Extension {
  constructor(viewer, options) {
    super(viewer, options);

    this.panel = new DBPropertyPanel(viewer, options);
  }

  async load() {
    var ext = await this.viewer.getExtension('Autodesk.PropertiesManager');
    ext.setPanel(this.panel);

    return true;
  }

  async unload() {
    var ext = await this.viewer.getExtension('Autodesk.PropertiesManager');
    ext.setDefaultPanel();

    return true;
  }
}

Autodesk.Viewing.theExtensionManager.registerExtension('DBPropertiesExtension', DBPropertiesExtension);
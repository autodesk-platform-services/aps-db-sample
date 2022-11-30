﻿/////////////////////////////////////////////////////////////////////
// Copyright (c) Autodesk, Inc. All rights reserved
// Written by APS Partner Development
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
    this.currentProperty = null;
    this.dbId = "";
    this.modelUrn = _viewer.model.getSeedUrn();

    //This is the event for property doubleclick
    //Autodesk.Viewing.UI.PropertyPanel.prototype.onPropertyDoubleClick = this.handlePropertyUpdate;
    //This in the event for object selection changes
    viewer.addEventListener(Autodesk.Viewing.SELECTION_CHANGED_EVENT, this.queryProps.bind(this));
  }

  queryProps(method) {
    if (_viewer.getSelection().length == 1) {
      this.dbId = _viewer.getSelection()[0];
      method === 'update' ? this.updateDB(this.dbId) : this.queryDB(this.dbId);
    }
  }

  updateDB(selecteddbId) {
    let itemId = this.modelUrn;
    updateDBData(selecteddbId, this.currentProperty, itemId);
  }

  queryDB(selecteddbId) {
    let itemId = this.modelUrn;
    extractDBData(selecteddbId, itemId);
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
      let _document = document;
      for (const groupName in propsForObject) {
        const group = propsForObject[groupName];
        for (const propName in group) {
          const propValue = group[propName];
          if (!this.tree.getElementForNode({ name: propName, value: "", category: CustomCategoryName })) {
            this.addProperty(propName, "", CustomCategoryName);
          }
          let element = this.tree.getElementForNode({ name: propName, value: "", category: CustomCategoryName });
          let inputValue = _document.createElement("input");
          inputValue.type = "text";
          inputValue.placeholder = propValue;
          inputValue.value = propValue;
          inputValue.addEventListener("focusout", () => {
            this.handlePropertyUpdate.call(this, inputValue, propName);
          });
          element.children[0].children[3].innerHTML = '';
          element.children[0].children[3].appendChild(inputValue);
        }
      }
      //this.highlight(CustomCategoryName);
    }
  }

  updateCurrentProperty(newValue) {
    this.properties[this.dbId][this.currentProperty.category][this.currentProperty.name] = newValue;
    this.currentProperty.value = newValue;
  }

  handlePropertyUpdate(input, propName) {
    let newPropValue = input.value;
    let propValue = this.properties[this.dbId][CustomCategoryName][propName];
    this.currentProperty = {
      name: propName,
      value: propValue,
      category: CustomCategoryName
    };
    if (propValue !== newPropValue) {
      this.updateCurrentProperty(newPropValue);
      this.queryProps('update');
      console.log("Current property changed to " + propName + " : " + newPropValue);
    }
  }

  checkProperty(property) {
    try {
      //Here we check if the property selected is aquired from DB
      return this.properties[this.dbId][property.category][property.name] == property.value;
    }
    catch {
      return false;
    }
  }
};

var CustomCategoryName = "Properties From DB";

//Here we show the user the result about the updated parameter
async function showUpdateResult(selecteddbId, updateResult) {
  let selector = (updateResult ? "div.ready" : "div.failure")
  $(selector).fadeIn(500).delay(2000).fadeOut(500);
}

async function showNotification(selecteddbId) {
  $('#alert_boxes').append(
    `<div class="alert-box updated" onclick="highlightDbId(event, ${selecteddbId})">${selecteddbId + ' updated!'}</div>`
  );
  setTimeout(() => {
    $('#alert_boxes').find(':last-child').fadeIn(500).delay(8000).fadeOut(500, function () { $(this).remove(); });
  }, 100);
}

async function highlightDbId(event, selecteddbId) {
  event.target.remove();
  _viewer.isolate(selecteddbId);
  _viewer.fitToView(selecteddbId);
}

//Here we add the properties aquired from DB to the proper dbid proper property panel
async function addProperties(selecteddbId, properties) {
  let ext = _viewer.getExtension('DBPropertiesExtension');

  ext.panel.properties[selecteddbId] = {
    [CustomCategoryName]: {

    }
  };
  for (const property of Object.keys(properties)) {
    ext.panel.properties[selecteddbId][[CustomCategoryName]][property] = properties[property];
  }
  ext.panel.setdbIdProperties(selecteddbId);
  //ext.panel.highlightCustomProperties();
}

//Here we reach the server endpoint to update the proper data from DB
async function updateDBData(selecteddbId, property, itemId) {
  const requestUrl = '/api/db/dbconnector';
  const requestData = {
    'connectionId': connection.connection.connectionId,
    'dbProvider': 'mongo',
    'property': property,
    'selecteddbId': selecteddbId,
    'itemId': itemId
  };
  apiClientAsync(requestUrl, requestData, 'post');
  $("div.gathering").fadeIn(500).delay(1500).fadeOut(500);
}

//Here we reach the server endpoint to retrieve the proper data from DB
async function extractDBData(selecteddbId, itemId) {
  try {
    const requestUrl = '/api/db/dbconnector';
    const requestData = {
      'connectionId': connection.connection.connectionId,
      'selecteddbId': selecteddbId,
      'itemId': itemId,
      'dbProvider': 'mongo'
    };
    apiClientAsync(requestUrl, requestData);
    $("div.gathering").fadeIn(500).delay(1500).fadeOut(500);
  }
  catch (err) {
    console.log(err);
    $("div.failure").fadeIn(500).delay(1500).fadeOut(500);
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

var _viewer;

// *******************************************
// DB Properties Extension
// *******************************************
class DBPropertiesExtension extends Autodesk.Viewing.Extension {
  constructor(viewer, options) {
    super(viewer, options);
    _viewer = viewer;

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
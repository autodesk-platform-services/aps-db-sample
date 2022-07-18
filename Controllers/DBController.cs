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
///
using forge_viewer_db_properties.Hubs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

[ApiController]
[Route("api/[controller]")]
public class DBController : ControllerBase
{
	public readonly IHubContext<DBHub> _dbHub;
	private readonly ILogger<DBController> _logger;
	private readonly DBService _dbService;

	public DBController(ILogger<DBController> logger, DBService dbService, IHubContext<DBHub> dbHub)
	{
		_dbHub = dbHub;
		GC.KeepAlive(_dbHub);
		_logger = logger;
		_dbService = dbService;
	}

	[HttpPost("dbconnector")]
	public object PostDBData([FromBody] DBUpdate dBUpdate)
	{

		switch (dBUpdate.dbProvider.ToLower())
		{
			case "mongo":
				_dbService.UpdateDataFromMongoDB(dBUpdate.connectionId, dBUpdate.property, dBUpdate.selecteddbId, dBUpdate.itemId, _dbHub);
				break;
			default:
				break;
		}
		return new { Success = true };
	}

	//public async Task UpdateDataFromMongoDB(string connectionId, Property property, string selecteddbId, string itemId)
	//{
	//	string connectionString = GetAppSetting("MONGODB_CON_STRING");
	//	string dbName = GetAppSetting("MONGODG_DBNAME");
	//	string collectionName = GetAppSetting("MONGODB_COLLECTION");
	//	string propFields = GetAppSetting("DB_PROPERTIES_NAMES");

	//	var client = new MongoClient(connectionString);

	//	var database = client.GetDatabase(dbName);

	//	var collection = database.GetCollection<BsonDocument>(collectionName);

	//	string id = GetIdFromProps(itemId, selecteddbId);

	//	var filter = new BsonDocument { { "_id", id } };

	//	var updateDef = Builders<BsonDocument>.Update.Set(doc => doc[property.name], property.value);

	//	UpdateResult updateResult = collection.UpdateOne(filter, updateDef);

	//	bool createResult = false;

	//	if (updateResult.MatchedCount == 0)
	//	{
	//		createResult = await CreateNewItemFromMongo(collection, id, property);
	//	}

	//	string message = (updateResult.IsModifiedCountAvailable ? $"{updateResult.ModifiedCount} item modified!" : createResult ? "New Document created!" : "No Document created!");

	//	Dictionary<string, dynamic> newRow = new Dictionary<string, dynamic>();
	//	newRow[property.name] = property.value;

	//	DBHub.SendUpdate(_dbHub, connectionId, selecteddbId, updateResult.IsModifiedCountAvailable, message, newRow, itemId);

	//}

	[HttpGet("dbconnector")]
	public object GetDBData()
	{
		string connectionId = base.Request.Query["connectionId"];
		//string externalId = base.Request.Query["externalId"];
		string selecteddbId = base.Request.Query["selecteddbId"];
		string dbProvider = base.Request.Query["dbProvider"];
		//string projectId = base.Request.Query["projectId"];
		string itemId = base.Request.Query["itemId"];


		switch (dbProvider.ToLower())
		{
			case "mongo":
				_dbService.ExtractDataFromMongoDB(connectionId, selecteddbId, itemId, _dbHub);
				break;
			default:
				break;
		}

		return new { Success = true };
	}

	//public async Task ExtractDataFromMongoDB(string connectionId, string selecteddbId, string itemId)
	//{

	//	string connectionString = GetAppSetting("MONGODB_CON_STRING");
	//	string dbName = GetAppSetting("MONGODG_DBNAME");
	//	string collectionName = GetAppSetting("MONGODB_COLLECTION");
	//	string propFields = GetAppSetting("DB_PROPERTIES_NAMES");

	//	var client = new MongoClient(connectionString);

	//	var database = client.GetDatabase(dbName);

	//	var collection = database.GetCollection<BsonDocument>(collectionName);

	//	string id = GetIdFromProps(itemId, selecteddbId);

	//	var filter = new BsonDocument { { "_id", id } };

	//	Dictionary<string, dynamic> newRow = new Dictionary<string, dynamic>();

	//	try
	//	{
	//		BsonDocument bsonDocument = await collection.Find(filter).SingleAsync();
	//		Dictionary<string, dynamic> document = bsonDocument.ToDictionary();

	//		foreach (string field in propFields.Split(","))
	//		{
	//			try
	//			{
	//				newRow[field] = document[field];
	//			}
	//			catch (Exception keyEx)
	//			{
	//				//In this case we have a field on env variable that's not present on Mongo Document
	//				newRow[field] = "";

	//			}
	//		}
	//	}
	//	catch (Exception ex)
	//	{
	//		//In this case no the document related to this element doesn't exists
	//		foreach (string field in propFields.Split(","))
	//		{
	//			newRow[field] = "";
	//		}
	//	}
	//	await DBHub.SendData(_dbHub, connectionId, selecteddbId, newRow);
	//}

	//public async Task<bool> CreateNewItemFromMongo(dynamic collection, string id, Property property)
	//{
	//	bool response = true;

	//	string propFields = GetAppSetting("DB_PROPERTIES_NAMES");

	//	Dictionary<string, dynamic> newDocument = new Dictionary<string, dynamic>();
	//	newDocument["_id"] = id;
	//	newDocument[property.name] = property.value;

	//	//foreach (string field in propFields.Split(","))
	//	//{
	//	//	try
	//	//	{
	//	//		newDocument[field] = field == property.name ? property.value : "";
	//	//	}
	//	//	catch (Exception ex)
	//	//	{
	//	//		
	//	//	}
	//	//}

	//	try
	//	{
	//		var jsonDoc = Newtonsoft.Json.JsonConvert.SerializeObject(newDocument);
	//		var bsonDoc = BsonSerializer.Deserialize<BsonDocument>(jsonDoc);
	//		collection.InsertOne(bsonDoc);
	//	}
	//	catch (Exception ex)
	//	{
	//		response = false;
	//	}

	//	return response;

	//}

	/// <summary>
	/// Reads appsettings from web.config
	/// </summary>
	//private string GetAppSetting(string settingKey)
	//{
	//	return Environment.GetEnvironmentVariable(settingKey);
	//}

}

public class DBUpdate
{
	public Property property { get; set; }
	public string connectionId { get; set; }
	public string dbProvider { get; set; }
	public string selecteddbId { get; set; }
	public string itemId { get; set; }
}

public class Property
{
	public string category { get; set; }
	public string name { get; set; }
	public string value { get; set; }
}

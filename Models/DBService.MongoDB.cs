
using forge_viewer_db_properties.Hubs;
using Microsoft.AspNetCore.SignalR;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public partial class DBService
{
	public async Task UpdateDataFromMongoDB(string connectionId, Property property, string selecteddbId, string itemId, IHubContext<DBHub> _dbHub)
	{

		var client = new MongoClient(_mongodbdatabaseConnectionString);

		var database = client.GetDatabase(_mongodbdatabaseName);

		var collection = database.GetCollection<BsonDocument>(_mongodbcollection);

		string id = GetIdFromProps(itemId, selecteddbId);

		var filter = new BsonDocument { { "_id", id } };

		var updateDef = Builders<BsonDocument>.Update.Set(doc => doc[property.name], property.value);

		UpdateResult updateResult = collection.UpdateOne(filter, updateDef);

		bool createResult = false;

		if (updateResult.MatchedCount == 0)
		{
			createResult = await CreateNewItemFromMongo(collection, id, property);
		}

		string message = (updateResult.IsModifiedCountAvailable ? $"{updateResult.ModifiedCount} item modified!" : createResult ? "New Document created!" : "No Document created!");

		Dictionary<string, dynamic> newRow = new Dictionary<string, dynamic>();
		newRow[property.name] = property.value;

		DBHub.SendUpdate(_dbHub, connectionId, selecteddbId, updateResult.IsModifiedCountAvailable, message, newRow, itemId);

	}

	public async Task ExtractDataFromMongoDB(string connectionId, string selecteddbId, string itemId, IHubContext<DBHub> _dbHub)
	{

		var client = new MongoClient(_mongodbdatabaseConnectionString);

		var database = client.GetDatabase(_mongodbdatabaseName);

		var collection = database.GetCollection<BsonDocument>(_mongodbcollection);

		string id = GetIdFromProps(itemId, selecteddbId);

		var filter = new BsonDocument { { "_id", id } };

		Dictionary<string, dynamic> newRow = new Dictionary<string, dynamic>();

		try
		{
			BsonDocument bsonDocument = await collection.Find(filter).SingleAsync();
			Dictionary<string, dynamic> document = bsonDocument.ToDictionary();

			foreach (string field in _properties.Split(","))
			{
				try
				{
					newRow[field] = document[field];
				}
				catch (Exception keyEx)
				{
					//In this case we have a field on env variable that's not present on Mongo Document
					newRow[field] = "";

				}
			}
		}
		catch (Exception ex)
		{
			//In this case no the document related to this element doesn't exists
			foreach (string field in _properties.Split(","))
			{
				newRow[field] = "";
			}
		}
		await DBHub.SendData(_dbHub, connectionId, selecteddbId, newRow);
	}

	public async Task<bool> CreateNewItemFromMongo(dynamic collection, string id, Property property)
	{
		bool response = true;

		Dictionary<string, dynamic> newDocument = new Dictionary<string, dynamic>();
		newDocument["_id"] = id;
		newDocument[property.name] = property.value;

		//foreach (string field in propFields.Split(","))
		//{
		//	try
		//	{
		//		newDocument[field] = field == property.name ? property.value : "";
		//	}
		//	catch (Exception ex)
		//	{
		//		
		//	}
		//}

		try
		{
			var jsonDoc = Newtonsoft.Json.JsonConvert.SerializeObject(newDocument);
			var bsonDoc = BsonSerializer.Deserialize<BsonDocument>(jsonDoc);
			collection.InsertOne(bsonDoc);
		}
		catch (Exception ex)
		{
			response = false;
		}

		return response;

	}

	//Through this function we obtain the id used by Mongo based on our model
	public string GetIdFromProps(string itemId, string selecteddbId)
	{
		return $"{itemId}_{selecteddbId}";
	}
}

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
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using Newtonsoft.Json.Linq;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace forgeSample.Controllers
{
	public class DBController : ControllerBase
	{
		public readonly IHubContext<DBHub> _dbHub;

		public DBController(IHubContext<DBHub> dbHub)
		{
			_dbHub = dbHub;
			GC.KeepAlive(_dbHub);
		}

		[HttpPost]
		[Route("api/dbconnector")]
		public object PostDBData([FromBody] DBUpdate dBUpdate)
		{

			switch (dBUpdate.dbProvider.ToLower())
			{
				case "oracle":
					UpdateDataFromOracleDB(dBUpdate.connectionId, dBUpdate.property, dBUpdate.externalId);
					break;
				case "mongo":
					UpdateDataFromMongoDB(dBUpdate.connectionId, dBUpdate.property, dBUpdate.externalId, dBUpdate.projectId, dBUpdate.itemId);
					break;
				default:
					break;
			}
			return new { Success = true };
		}

		public async Task UpdateDataFromMongoDB(string connectionId, Property property, string externalId, string projectId, string itemId)
		{
			string connectionString = GetAppSetting("MONGODB_CON_STRING");
			string dbName = GetAppSetting("MONGODG_DBNAME");
			string collectionName = GetAppSetting("MONGODB_COLLECTION");

			var client = new MongoClient(connectionString);

			var database = client.GetDatabase(dbName);

			var collection = database.GetCollection<BsonDocument>(collectionName);

			string id = GetIdFromProps(projectId, externalId, itemId);

			var filter = new BsonDocument { { "_id", id } };

			var updateDef = Builders<BsonDocument>.Update.Set(doc => doc[property.name], property.value);

			UpdateResult updateResult = collection.UpdateOne(filter, updateDef);

			bool createResult = false;

			if (updateResult.MatchedCount == 0)
            {
                createResult = await CreateNewItemFromMongo(collection, id, property);
            }

			string message = (updateResult.IsModifiedCountAvailable ? $"{updateResult.ModifiedCount} items modified!" : "No item was modified!");

			DBHub.SendUpdate(_dbHub, connectionId, externalId, updateResult.IsModifiedCountAvailable, message);

		}

		//Through this function we obtain the id used by mONGOdb BASED on our model
        public string GetIdFromProps(string projectId, string externalId, string itemId)
        {
			return $"{projectId}_{itemId}_{externalId}";

		}

        public async Task UpdateDataFromOracleDB(string connectionId, Property property, string externalId)
		{

			//Create connection string. Check whether DBA Privilege is required.
			string connectionString = GetAppSetting("ORACLEDB_CON_STRING");

			using OracleConnection con = new OracleConnection(connectionString);
			using OracleCommand cmd = con.CreateCommand();
			try
			{
				con.Open();

				//Environment vars
				string propFields = GetAppSetting("DB_PROPERTIES_NAMES");
				string tableName = GetAppSetting("DB_TABLE_NAME");
				string filterField = GetAppSetting("DB_FILTER_PROPERTY");

				//Modify the anonymous PL/SQL GRANT command if you wish to modify the privileges granted
				cmd.CommandText = "update " + tableName + " set " + property.name + "= \'" + property.value + "\' where " + filterField + "= \'" + externalId + "\'";
				int result = cmd.ExecuteNonQuery();

				DBHub.SendUpdate(_dbHub, connectionId, externalId, true, $"{result} rows affected!");
			}
			catch (Exception ex)
			{
				DBHub.SendUpdate(_dbHub, connectionId, externalId, false, ex.Message);
			}
		}

		[HttpGet]
		[Route("api/dbconnector")]
		public object GetDBData()
		{
			string connectionId = base.Request.Query["connectionId"];
			string externalId = base.Request.Query["externalId"];
			string dbProvider = base.Request.Query["dbProvider"];
			string projectId = base.Request.Query["projectId"];
			string itemId = base.Request.Query["itemId"];


			switch (dbProvider.ToLower())
			{
				case "oracle":
					ExtractDataFromOracleDB(connectionId, externalId, projectId, itemId);
					break;
				case "mongo":
					ExtractDataFromMongoDB(connectionId, externalId, projectId, itemId);
					break;
				default:
					break;
			}

			return new { Success = true };
		}

		public async Task ExtractDataFromMongoDB( string connectionId, string externalId, string projectId, string itemId)
		{

			string connectionString = GetAppSetting("MONGODB_CON_STRING");
			string dbName = GetAppSetting("MONGODG_DBNAME");
			string collectionName = GetAppSetting("MONGODB_COLLECTION");
			string propFields = GetAppSetting("DB_PROPERTIES_NAMES");

			var client = new MongoClient(connectionString);

			var database = client.GetDatabase(dbName);

			var collection = database.GetCollection<BsonDocument>(collectionName);

			string id = GetIdFromProps(projectId, externalId, itemId);

			var filter = new BsonDocument { { "_id", id } };

			Dictionary<string, dynamic> newRow = new Dictionary<string, dynamic>();

			try
            {
				BsonDocument bsonDocument = await collection.Find(filter).SingleAsync();
				Dictionary<string, dynamic> document = bsonDocument.ToDictionary();

				foreach (string field in propFields.Split(","))
				{
					newRow[field] = document[field];
				}
			}
            catch (Exception ex)
            {
				//In this case no the document related to this element doesn't exists
				foreach (string field in propFields.Split(","))
				{
					newRow[field] = "";
				}
			}
            await DBHub.SendData(_dbHub, connectionId, externalId, newRow);
        }

        public async Task<bool> CreateNewItemFromMongo(dynamic collection, string id, Property property)
        {
			bool response = true;

			string propFields = GetAppSetting("DB_PROPERTIES_NAMES");

			Dictionary<string, dynamic> newDocument = new Dictionary<string, dynamic>();
			newDocument["_id"] = id;

			foreach (string field in propFields.Split(","))
			{
				try
				{
					newDocument[field] = field == property.name ? property.value : "";
				}
				catch (Exception ex)
				{

				}
			}

			try
            {
				var jsonDoc = Newtonsoft.Json.JsonConvert.SerializeObject(newDocument);
				var bsonDoc = BsonSerializer.Deserialize<BsonDocument>(jsonDoc);
				collection.InsertOne(bsonDoc);
			}
			catch(Exception ex)
            {
				response = false;
            }

			return response;

		}

        public async Task ExtractDataFromOracleDB(string connectionId, string externalId, string projectId, string itemId)
		{
			//string dbTag = await GetMappIds(externalId);

			if (true)
			{
				//Create connection string. Check whether DBA Privilege is required.
				string connectionString = GetAppSetting("ORACLEDB_CON_STRING");
				
				using OracleConnection con = new OracleConnection(connectionString);
				using OracleCommand cmd = con.CreateCommand();
				try
				{
					con.Open();

					//Environment vars
					string propFields = GetAppSetting("DB_PROPERTIES_NAMES");
					string tableName = GetAppSetting("DB_TABLE_NAME");
					string filterField = GetAppSetting("DB_FILTER_PROPERTY");

					//Modify the anonymous PL/SQL GRANT command if you wish to modify the privileges granted
					cmd.CommandText = "select " + propFields + " from " + tableName + " where " + filterField + "= \'" + externalId + "\'";
					OracleDataReader reader = cmd.ExecuteReader();
					while (reader.Read())
					{
						Dictionary<string, dynamic> newRow = new Dictionary<string, dynamic>();
						foreach (var field in propFields.Split(","))
						{
							newRow[field] = reader[field];
						}
						newRow["Status"] = "Connection Succeeded";
						await DBHub.SendData(_dbHub, connectionId, externalId, newRow);
					}

					reader.Dispose();
				}
				catch (Exception ex)
				{
					Dictionary<string, dynamic> newRow = new Dictionary<string, dynamic>();
					newRow["Status"] = "Connection Failed";
					await DBHub.SendData(_dbHub, connectionId, externalId, newRow);
				}
			}
			else
			{
				
			}
		}

        /// <summary>
        /// Reads appsettings from web.config
        /// </summary>
        private string GetAppSetting(string settingKey)
		{
			return Environment.GetEnvironmentVariable(settingKey);
		}

	}

	public class DBUpdate
	{
		public Property property { get; set; } 
		public string connectionId { get; set; } 
		public string dbProvider { get; set; }
		public string externalId { get; set; }
		public string projectId { get; set; }
		public string itemId { get; set; }
	}

	public class Property
	{
		public string category { get; set; }
		public string name { get; set; }
		public string value { get; set; }
	}
}
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

		public async Task UpdateDataFromMongoDB(string connectionId, Property property, string externalId, string projectId, string itemiD)
		{
			string connectionString = GetAppSetting("MONGODB_CON_STRING");
			string dbName = GetAppSetting("MONGODG_DBNAME");
			string collection = GetAppSetting("MONGODB_COLLECTION");

			try
			{
				BsonClassMap.RegisterClassMap<MongoItem>();
			}
			catch (Exception)
			{

			}

			var client = new MongoClient(connectionString);

			var database = client.GetDatabase(dbName);

			var items = database.GetCollection<MongoItem>(collection);

			var builder = Builders<MongoItem>.Filter;

			var filter = builder.Eq(doc => doc.ExternalId, externalId) & builder.Eq(doc => doc.ProjectId, projectId) & builder.Eq(doc => doc.ItemId, itemiD);

			var updateDef = Builders<MongoItem>.Update.Set(doc => doc[property.name], property.value);

			UpdateResult result = items.UpdateOne(filter, updateDef);

			string message = (result.IsModifiedCountAvailable ? $"{result.ModifiedCount} items modified!" : "No item was modified!");

			DBHub.SendUpdate(_dbHub, connectionId, externalId, result.IsModifiedCountAvailable, message);

		}

		public async Task UpdateDataFromOracleDB(string connectionId, Property property, string externalId)
		{
			//string dbTag = await GetMappIds(externalId);

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

			//env var
			//string dbProvider = "ORACLE";

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
			//string dbTag = await GetMappIds(externalId);

			string connectionString = GetAppSetting("MONGODB_CON_STRING");
			string dbName = GetAppSetting("MONGODG_DBNAME");
			string collectionName = GetAppSetting("MONGODB_COLLECTION");

			try
			{
				BsonClassMap.RegisterClassMap<MongoItem>();
			}
			catch (Exception)
			{

			}

			var client = new MongoClient(connectionString);

			var database = client.GetDatabase(dbName);

			var collection = database.GetCollection<MongoItem>(collectionName);

			List<MongoItem> matches = collection.Find(map => map.ExternalId == externalId && map.ProjectId == projectId && map.ItemId == itemId).ToList();

			if(matches.Count > 0)
            {
				string propFields = GetAppSetting("DB_PROPERTIES_NAMES");

				Dictionary<string, dynamic> newRow = new Dictionary<string, dynamic>();
				foreach (string field in propFields.Split(","))
				{
					try
					{
						newRow[field] = matches[0][field];
					}
					catch (Exception ex)
					{

					}
				}
				await DBHub.SendData(_dbHub, connectionId, externalId, newRow);
			}
            else
            {
				MongoItem newItem = new MongoItem()
				{
					ExternalId = externalId,
					ProjectId = projectId,
					ItemId = itemId
				};
				await CreateNewItemFromMongo(newItem, collection);
				Dictionary<string, dynamic> newRow = new Dictionary<string, dynamic>();
				newRow["Material"] = "";
				newRow["Supplier"] = "";
				newRow["Price"] = "";
				newRow["Currency"] = "";
				await DBHub.SendData(_dbHub, connectionId, externalId, newRow);
			}

		}

        public async Task CreateNewItemFromMongo(MongoItem newItem, dynamic collection)
        {
			string message = "";

            try
            {
				collection.InsertOne(newItem);
				message = "New item successfully inserted!";
			}
			catch(Exception ex)
            {
				message = "Error inserting new item: ex.Message";
            }

            Console.WriteLine(message);
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
				MongoItem newItem = new MongoItem()
				{
					ExternalId = externalId,
					ProjectId = projectId,
					ItemId = itemId
				};
				Dictionary<string, dynamic> newRow = new Dictionary<string, dynamic>();
				newRow["Material"] = "";
				newRow["Supplier"] = "";
				newRow["Price"] = "";
				newRow["Currency"] = "";
				newRow["ASSET_TAG"] = "not found!";
				newRow["ASSET_TAG"] = "not found!";
				await DBHub.SendData(_dbHub, connectionId, externalId, newRow);
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

	[BsonIgnoreExtraElements]
	public class MongoItem
	{
		//We use this to retrieve property from srting name
		//https://stackoverflow.com/questions/10283206/setting-getting-the-class-properties-by-string-name
		public object this[string propertyName]
		{
			get
			{
				// probably faster without reflection:
				// like:  return Properties.Settings.Default.PropertyValues[propertyName] 
				// instead of the following
				Type myType = typeof(MongoItem);
				PropertyInfo myPropInfo = myType.GetProperty(propertyName);
				return myPropInfo.GetValue(this, null);
			}
			set
			{
				Type myType = typeof(MongoItem);
				PropertyInfo myPropInfo = myType.GetProperty(propertyName);
				myPropInfo.SetValue(this, value, null);
			}
		}

		public string Material { get; set; }
		public string Supplier { get; set; }
		public string Price { get; set; }
		public string Currency { get; set; }
		public string ExternalId { get; set; }
		public string ProjectId { get; set; }
		public string ItemId { get; set; }
	}
}
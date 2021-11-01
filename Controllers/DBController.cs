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
					UpdateDataFromOracleDB(dBUpdate.connectionId, dBUpdate.property);
					break;
				case "mongo":
					UpdateDataFromMongoDB(dBUpdate.connectionId, dBUpdate.property, dBUpdate.externalId);
					break;
				default:
					break;
			}
			return new { Success = true };
		}

		public async Task UpdateDataFromMongoDB(string connectionId, Property property, string externalId)
		{
			string dbTag = await GetMappIds(externalId);

			if (dbTag != "")
			{
				string connectionString = Credentials.GetAppSetting("MONGODB_CON_STRING");
				string dbName = Credentials.GetAppSetting("MONGODG_ASSET_DBNAME");
				string collection = Credentials.GetAppSetting("MONGODB_ASSET_COLLECTION");

				try
				{
					BsonClassMap.RegisterClassMap<MongoTag>();
				}
				catch (Exception)
				{

				}

				var client = new MongoClient(connectionString);

				var database = client.GetDatabase(dbName);

				var items = database.GetCollection<MongoTag>(collection);

				var filter = Builders<MongoTag>.Filter.Eq(doc => doc.ASSET_TAG, dbTag);

				var updateDef = Builders<MongoTag>.Update.Set(doc => doc[property.name], property.value);

				UpdateResult result = items.UpdateOne(filter, updateDef);

				DBHub.SendUpdate(_dbHub, connectionId, externalId, result);
				//await DBHub.SendData(_dbHub, connectionId, externalId, newRow);

			}
			else
			{

			}
			
		}

		public async Task UpdateDataFromOracleDB(string connectionId, Property property)
		{
			throw new NotImplementedException();
		}

		[HttpGet]
		[Route("api/dbconnector")]
		public object GetDBData()
		{
			string connectionId = base.Request.Query["connectionId"];
			string externalId = base.Request.Query["externalId"];
			string dbProvider = base.Request.Query["dbProvider"];

			//env var
			//string dbProvider = "ORACLE";

			switch (dbProvider.ToLower())
			{
				case "oracle":
					ExtractDataFromOracleDB(connectionId, externalId);
					break;
				case "mongo":
					ExtractDataFromMongoDB(connectionId, externalId);
					break;
				default:
					break;
			}

			return new { Success = true };
		}

		public async Task ExtractDataFromMongoDB( string connectionId, string externalId)
		{
			string dbTag = await GetMappIds(externalId);

			if (dbTag != "")
			{
				string connectionString = Credentials.GetAppSetting("MONGODB_CON_STRING");
				string dbName = Credentials.GetAppSetting("MONGODG_ASSET_DBNAME");
				string collection = Credentials.GetAppSetting("MONGODB_ASSET_COLLECTION");

				try
				{
					BsonClassMap.RegisterClassMap<MongoTag>();
				}
				catch (Exception)
				{

				}

				var client = new MongoClient(connectionString);

				var database = client.GetDatabase(dbName);

				var items = database.GetCollection<MongoTag>(collection);

				List<MongoTag> matches = items.Find(asset => asset.ASSET_TAG == dbTag).ToList();

				string propFields = Credentials.GetAppSetting("DB_PROPERTIES_NAMES");

				Dictionary<string, dynamic> newRow = new Dictionary<string, dynamic>();
				foreach (string field in propFields.Split(","))
				{
					//PropertyInfo property = typeof(MongoTag).GetProperties().ToList().Find(p => p.Name == field);
					//if(property != null) newRow[field] = property.GetValue(matches[0]);

					newRow[field] = matches[0][field];
				}
				newRow["Status"] = "Connection Succeeded";
				await DBHub.SendData(_dbHub, connectionId, externalId, newRow);

			}
			else
			{
				Dictionary<string, dynamic> newRow = new Dictionary<string, dynamic>();
				newRow["Status"] = "Connection Succeeded";
				newRow["ASSET_TAG"] = "not found!";
				await DBHub.SendData(_dbHub, connectionId, externalId, newRow);
			}
		}

		public async Task ExtractDataFromOracleDB(string connectionId, string externalId)
		{
			string dbTag = await GetMappIds(externalId);

			if (dbTag != "")
			{
				//Create connection string. Check whether DBA Privilege is required.
				string connectionString = Credentials.GetAppSetting("ORACLEDB_CON_STRING");
				
				using OracleConnection con = new OracleConnection(connectionString);
				using OracleCommand cmd = con.CreateCommand();
				try
				{
					con.Open();

					//Environment vars
					string propFields = Credentials.GetAppSetting("DB_PROPERTIES_NAMES");
					string tableName = Credentials.GetAppSetting("DB_TABLE_NAME");
					string filterField = Credentials.GetAppSetting("DB_FILTER_PROPERTY");

					//Modify the anonymous PL/SQL GRANT command if you wish to modify the privileges granted
					cmd.CommandText = "select " + propFields + " from " + tableName + " where " + filterField + "= \'" + dbTag + "\'";
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
				Dictionary<string, dynamic> newRow = new Dictionary<string, dynamic>();
				newRow["Status"] = "Connection Succeeded";
				newRow["ASSET_TAG"] = "not found!";
				await DBHub.SendData(_dbHub, connectionId, externalId, newRow);
			}
		}

		public async Task<string> GetMappIds(string externalId)
		{
			//env vars
			string connectionString = Credentials.GetAppSetting("MONGODB_CON_STRING");
			string dbName = Credentials.GetAppSetting("MONGODG_MAP_DBNAME");
			string collection = Credentials.GetAppSetting("MONGODB_MAP_COLLECTION");

			try
			{
				BsonClassMap.RegisterClassMap<MapCollection>();
			}
			catch (Exception)
			{

			}
			
			var client = new MongoClient(connectionString);

			var database = client.GetDatabase(dbName);

			var items = database.GetCollection<MapCollection>(collection);

			List<MapCollection> matches = items.Find(map => map.externalId == externalId).ToList();

			List<string> tags = matches.Select(o => o.dbTag).ToList();

			return (tags.Count > 0 ? tags[0] : "" );
		}
	}

	public class DBUpdate
	{
		public Property property { get; set; } 
		public string connectionId { get; set; } 
		public string dbProvider { get; set; }
		public string externalId { get; set; }
	}

	public class Property
	{
		public string category { get; set; }
		public string name { get; set; }
		public string value { get; set; }
	}

	[BsonIgnoreExtraElements]
	public class MapCollection
	{
		public string dbTag { get; set; }
		public string externalId { get; set; }
	}

	[BsonIgnoreExtraElements]
	public class MongoTag
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
				Type myType = typeof(MongoTag);
				PropertyInfo myPropInfo = myType.GetProperty(propertyName);
				return myPropInfo.GetValue(this, null);
			}
			set
			{
				Type myType = typeof(MongoTag);
				PropertyInfo myPropInfo = myType.GetProperty(propertyName);
				myPropInfo.SetValue(this, value, null);
			}
		}

		public string ASSET_TAG { get; set; }
		public string ASSET_TYPE { get; set; }
		public string DESCRIPTION { get; set; }
		public int TAG_NUMBER { get; set; }
	}
}
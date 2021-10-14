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

		[HttpGet]
		[Route("api/dbconnector")]
		public object GetDBData()
		{
			string connectionId = base.Request.Query["connectionId"];
			string externalId = base.Request.Query["externalId"];

			//env var
			string dbProvider = "ORACLE";

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
			throw new NotImplementedException();
		}

		public async Task ExtractDataFromOracleDB(string connectionId, string externalId)
		{
			string dbTag = await GetMappIds(externalId);

			if (dbTag != "")
			{
				//Create connection string. Check whether DBA Privilege is required.
				string conStringDBA = "Data Source=(DESCRIPTION=(ADDRESS_LIST=(ADDRESS=(PROTOCOL=TCP)(HOST=test.fac.clemson.edu)(PORT=1521)))(CONNECT_DATA=(SERVICE_NAME=aimdev)));User Id=adsk;Password=clemson123;";

				using OracleConnection con = new OracleConnection(conStringDBA);
				using OracleCommand cmd = con.CreateCommand();
				try
				{
					con.Open();

					//Environment vars
					string[] propFields =
					{
							"ASSET_TAG",
							"ASSET_TYPE",
							"DESCRIPTION",
							"EDIT_DATE",
							"LONG_DESC",
							"STATUS_CODE",
							"ASSET_GROUP",
							"TAG_NUMBER"
						};
					//Environment vars
					string tableName = "aim.ae_a_asset_e";
					string filterField = "ASSET_TAG";

					//Modify the anonymous PL/SQL GRANT command if you wish to modify the privileges granted
					cmd.CommandText = "select " + String.Join(",", propFields) + " from " + tableName + " where " + filterField + "= \'" + dbTag +"\'";
					OracleDataReader reader = cmd.ExecuteReader();
					while (reader.Read())
					{
						//dynamic newRow = new System.Dynamic.ExpandoObject();
						Dictionary<string, dynamic> newRow = new Dictionary<string, dynamic>();
						foreach (var field in propFields)
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
			string connectionString = "mongodb+srv://forge_user:gQgAbqOnIoumdWUp@cluster0.bjupz.mongodb.net/hangfire?retryWrites=true&w=majority";
			string dbName = "propertydbextension";
			string collection = "externalidtotag";

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

	[BsonIgnoreExtraElements]
	public class MapCollection
	{
		public string dbTag { get; set; }
		public string externalId { get; set; }

	}
}
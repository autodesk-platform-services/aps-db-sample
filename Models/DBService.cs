public partial class DBService
{
	private readonly string _mongodbdatabaseName;
	private readonly string _mongodbcollection;
	private readonly string _mongodbdatabaseConnectionString;
	private readonly string _properties;

	public DBService(string databaseName, string collectionName, string databaseConnectionString, string properties)
	{
		_mongodbdatabaseName = databaseName;
		_mongodbcollection = collectionName;
		_mongodbdatabaseConnectionString = databaseConnectionString;
		_properties = properties;
	}

}
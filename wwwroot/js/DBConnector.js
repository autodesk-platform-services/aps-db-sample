async function extractDBData (externalId) {
  try {
    const requestUrl = '/api/dbconnector';
    const requestData = {
      'connectionId': connection.connection.connectionId,
      'externalId': externalId
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
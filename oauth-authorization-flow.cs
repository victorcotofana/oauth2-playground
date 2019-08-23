// Authorization flow, returns the byte array of the logged in user

// This is on controller
// The redirect points here
public ActionResult SyncPhotoToken()
	{
		try
		{
			var redirectResult = Request;

			var access_code = redirectResult.Params.Get("code");

			var access_token = GetAccessToken(access_code);

			var graph_photo = GetGraphPhoto(access_token);

			UploadPhoto(graph_photo);
		}
		catch (Exception)
		{
			CLogger.WriteLog(ELogLevel.ERROR, "User's photo cannot be fetched.");
		}

		return RedirectToAction("Edit", "Experts", new { id = GlobalInfo.Instance.ID });
	}

	private string GetAccessToken(string access_code)
	{
		// OAuth2 reference: https://docs.microsoft.com/en-us/azure/active-directory/develop/v2-oauth2-auth-code-flow#request-an-access-token
		string tokenUrl = ConfigurationManager.AppSettings["tokenEndpoint"];

		// POST request reference: https://docs.microsoft.com/en-us/dotnet/api/system.net.httpwebrequest.getrequeststream?redirectedfrom=MSDN&view=netframework-4.0#System_Net_HttpWebRequest_GetRequestStream
		string postData =
			"client_id=" + HttpUtility.UrlEncode(ConfigurationManager.AppSettings["clientId"]) + "&" +
			"scope=" + HttpUtility.UrlEncode(ConfigurationManager.AppSettings["tokenScope"]) + "&" +
			"code=" + access_code + "&" +
			"redirect_uri=" + HttpUtility.UrlEncode(ConfigurationManager.AppSettings["tokenRedirectUri"]) + "&" +
			"grant_type=" + "authorization_code" + "&" +
			"client_secret=" + HttpUtility.UrlEncode(ConfigurationManager.AppSettings["clientSecret"]);

		byte[] bytePostData = Encoding.ASCII.GetBytes(postData);

		HttpWebRequest WebReq = (HttpWebRequest)WebRequest.Create(tokenUrl);
		WebReq.Method = "POST";
		WebReq.ContentType = "application/x-www-form-urlencoded";
		WebReq.ContentLength = bytePostData.Length;

		Stream postRequestStream = WebReq.GetRequestStream();
		postRequestStream.Write(bytePostData, 0, bytePostData.Length);
		postRequestStream.Close();

		HttpWebResponse WebResp = (HttpWebResponse)WebReq.GetResponse();

		Stream responseStream = WebResp.GetResponseStream();
		StreamReader responseStreamReader = new StreamReader(responseStream);
		var stringResponse = responseStreamReader.ReadToEnd();

		var jsonResponse = (JObject)JsonConvert.DeserializeObject(stringResponse);
		string access_token = jsonResponse["access_token"].Value<string>();

		return access_token;
	}

	private byte[] GetGraphPhoto(string access_token)
	{
		string requestPhotoUrl = ConfigurationManager.AppSettings["microsoftGraphEndpoint"];
		HttpWebRequest WebReq = (HttpWebRequest)WebRequest.Create(requestPhotoUrl);
		WebReq.Method = "GET";
		WebReq.Headers.Add("Authorization", "Bearer " + access_token);

		HttpWebResponse WebResp = (HttpWebResponse)WebReq.GetResponse();
		Stream respStream = WebResp.GetResponseStream();

		MemoryStream memoryStream = new MemoryStream();
		respStream.CopyTo(memoryStream);

		return memoryStream.ToArray();
	}
	

// This is the initial link from the client

let authUrl = "@ConfigurationManager.AppSettings["authorizationEndpoint"]" +
            "?response_type=code" +
            "&client_id=@ConfigurationManager.AppSettings["clientId"]" +
            "&scope=@HttpUtility.UrlEncode(ConfigurationManager.AppSettings["tokenScope"])" +
            "&redirect_uri=@HttpUtility.UrlEncode(ConfigurationManager.AppSettings["tokenRedirectUri"])";

$("#sync_photo").attr("href", authUrl);


// The redirect links to another controller, which is centralized then based on the logged in userId redirects back to the edit page

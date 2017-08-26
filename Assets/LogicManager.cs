using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Amazon;
using Facebook.Unity;
using Amazon.CognitoIdentity;
using Amazon.Runtime;
using System;
using com.guywald.examples.unity.awsv4signer.AWSSigners;
using System.Text;
using UnityEngine.Events;
using TMPro;

namespace com.guywald.examples.unity.awsv4signer
{
	public class LogicManager : MonoBehaviour
	{
		[Header ("Api Gateway")]
		public InputField ApiGatewayUrlInputField;
		public InputField ApiGatewayRegionField;

		[Header ("Simple Log")]
		public TMP_InputField SimpleLogInputField;

		[Header ("Facebook")]
		public Button FacebookLoginButton;
		public InputField FacebookAppIdInputField;

		[Header ("Cognito")]
		public Button CognitoButton;
		public InputField CognitoPoolIdInputField;
		public InputField CognitoRegionInputField;

		[Header ("Create")]
		public InputField CreateMessageInputField;
		public Button CreateButton;

		[Header ("Retrieve")]
		public InputField RetrieveMessageIdInputField;
		public Button RetrieveButton;

		[Header ("Update")]
		public InputField UpdateMessageIdInputField;
		public InputField UpdateMessageTextInputField;
		public Button UpdateButton;

		[Header ("Delete")]
		public InputField DeleteMessageIdInputField;
		public Button DeleteButton;

		private const string prefsPrefix = "aws-apigateway-unity-example";

		private CognitoAWSCredentials credentials;
		private ImmutableCredentials immutableCredentials;
		// used to keep the Cognito Credentials


		void Awake ()
		{
			credentials = null;
		}

		// Use this for initialization
		void Start ()
		{
			ApiGatewayUrlInputField.text = PlayerPrefs.GetString (prefsPrefix + ".ApiGatewayUrl",string.Empty);
			ApiGatewayUrlInputField.onValueChanged.AddListener ((str)=>{ PlayerPrefs.SetString (prefsPrefix + ".ApiGatewayUrl",str); });

			ApiGatewayRegionField.text = PlayerPrefs.GetString (prefsPrefix + ".ApiGatewayRegion",string.Empty);
			ApiGatewayRegionField.onValueChanged.AddListener ((str)=>{ PlayerPrefs.SetString (prefsPrefix + ".ApiGatewayRegion",str); });

			FacebookAppIdInputField.text = PlayerPrefs.GetString (prefsPrefix + ".FacebookAppID",string.Empty);
			FacebookAppIdInputField.onValueChanged.AddListener ((str)=>{ PlayerPrefs.SetString (prefsPrefix + ".FacebookAppID",str); });

			CognitoRegionInputField.text = PlayerPrefs.GetString (prefsPrefix + ".CognitoRegion",string.Empty);
			CognitoRegionInputField.onValueChanged.AddListener ((str)=>{ PlayerPrefs.SetString (prefsPrefix + ".CognitoRegion",str); });

			CognitoPoolIdInputField.text = PlayerPrefs.GetString (prefsPrefix + ".CognitoPool",string.Empty);
			CognitoPoolIdInputField.onValueChanged.AddListener ((str)=>{ PlayerPrefs.SetString (prefsPrefix + ".CognitoPool",str); });
		}
	
		// Update is called once per frame
		void Update ()
		{
		
		}

		#region Methods To Implement

		private void DoCreate (string awsKey, string awsSecret, string awsToken, string messageInput)
		{
			var createPath = "create";
			var uri = new Uri (string.Format ("https://{0}", BuildPath (createPath)));

			var bodyJson = JsonUtility.ToJson (new CreateMessageRequest (messageInput));

			// Hashing content
			var contentHash = AWS4SignerBase.CanonicalRequestHashAlgorithm.ComputeHash (Encoding.UTF8.GetBytes (bodyJson));
			var contentHashString = AWS4SignerBase.ToHexString (contentHash, true);

			var headers = new Dictionary<string,string> { 
				{ AWS4SignerBase.X_Amz_Content_SHA256, contentHashString },
				{ "content-length", bodyJson.Length.ToString() },
				{ "content-type", "application/json" },
				{ AWS4SignerBase.X_Amz_Security_Token, awsToken }
			};


			var signer = new AWS4SignerForPOST {
				EndpointUri = uri,
				HttpMethod = "POST",
				Service = "execute-api",
				Region = ApiGatewayRegionField.text
			};

			var authorization = signer.ComputeSignature (
				                    headers,
				                    string.Empty,
				                    contentHashString,
				                    awsKey,
				                    awsSecret);
			headers.Add ("Authorization", authorization);
			var payloadBytes = Encoding.UTF8.GetBytes (bodyJson);

			headers.Remove("Host");
			WWW www = new WWW (uri.AbsoluteUri, payloadBytes, headers);

			Send(www,(resp=>{
				if (string.IsNullOrEmpty(www.error)) {
					Log(www.text);
				} else {
					Log("Error: "+www.error);
				}
			}));

		}

		private void DoRetrieve (string awsKey, string awsSecret, string awsToken, string messageId)
		{
			throw new System.NotImplementedException ("Implement this");
		}

		private void DoUpdate (string awsKey, string awsSecret, string awsToken, string messageId, string messageInput)
		{
			throw new System.NotImplementedException ("Implement this");
		}

		private void DoDelete (string awsKey, string awsSecret, string awsToken, string messageId)
		{
			throw new System.NotImplementedException ("Implement this");
		}

		#endregion

		#region Button Methods

		public void OnPressFacebookLogin ()
		{
			if (!FB.IsInitialized) {
				Log ("Initializing Facebook SDK");
				FB.Init (FacebookAppIdInputField.text, null, true, true, true, false, true, null, "en_US", FBOnHideUnity, FBInitCallback);
			} else {
				FBInitCallback ();
			}
		}

		public void OnPressCognito ()
		{
			if (!FB.IsInitialized || !FB.IsLoggedIn) {
				Log ("Must login to FB First!");
				return;
			}


			if (credentials == null)
				CognitoInit ();
		
			CognitoLogin ();
		}

		public void OnPressCreate ()
		{
			if (immutableCredentials == null) {
				Log ("No Cognito Credentials");
				return;
			}

			DoCreate (immutableCredentials.AccessKey, immutableCredentials.SecretKey, immutableCredentials.Token, CreateMessageInputField.text);
		}

		public void OnPressRetrieve ()
		{
			if (immutableCredentials == null) {
				Log ("No Cognito Credentials");
				return;
			}

			DoRetrieve (immutableCredentials.AccessKey, immutableCredentials.SecretKey, immutableCredentials.Token, RetrieveMessageIdInputField.text);
		}

		public void OnPressUpdate ()
		{
			if (immutableCredentials == null) {
				Log ("No Cognito Credentials");
				return;
			}

			DoUpdate (immutableCredentials.AccessKey, immutableCredentials.SecretKey, immutableCredentials.Token, UpdateMessageIdInputField.text, UpdateMessageTextInputField.text);
		}

		public void OnPressDelete ()
		{
			if (immutableCredentials == null) {
				Log ("No Cognito Credentials");
				return;
			}

			DoDelete (immutableCredentials.AccessKey, immutableCredentials.SecretKey, immutableCredentials.Token, DeleteMessageIdInputField.text);
		}

		#endregion


		#region Facebook Methods


		private void FBInitCallback ()
		{
			if (FB.IsInitialized) {
				// Signal an app activation App Event
				FB.ActivateApp ();
				if (!FB.IsLoggedIn)
					FBLogin ();
				else
					Log ("FB Already Logged In!");
			} else {
				Debug.Log ("Failed to Initialize the Facebook SDK");
			}
		}

		private void FBOnHideUnity (bool isGameShown)
		{
			if (!isGameShown) {
				// Pause the game - we will need to hide
				Time.timeScale = 0;
			} else {
				// Resume the game - we're getting focus again
				Time.timeScale = 1;
			}
		}

		private void FBLogin ()
		{
			FB.LogInWithReadPermissions (new List<string> () { "public_profile", "email", "user_friends" }, this.FBLoginHandleResult);
		}

		private void FBLoginHandleResult (IResult result)
		{
			if (result == null) {
				Log ("FB Login Null Response");
				return;
			}

			if (!string.IsNullOrEmpty (result.Error)) {
				Log (string.Format ("FB Login Error: {0}", result.Error));
			} else if (result.Cancelled) {
				Log (string.Format ("FB Login Canceled. {0}", result.RawResult));
			} else if (!string.IsNullOrEmpty (result.RawResult)) {
				Log (string.Format ("FB Login Success - {0}", result.RawResult));
				FacebookLoggedInHandler ();
			} else {
				Log ("FB Login Empty Response");
			}
		}


		private void FacebookLoggedInHandler ()
		{
			Log ("FB user logged in!!!");
		}

		#endregion


		#region Amazon Methods

		void CognitoInit ()
		{
			UnityInitializer.AttachToGameObject (this.gameObject);
			Amazon.AWSConfigs.HttpClient = AWSConfigs.HttpClientOption.UnityWebRequest;
			// Initialize the Amazon Cognito credentials provider
			credentials = new CognitoAWSCredentials (
				CognitoPoolIdInputField.text, // Identity Pool ID
				RegionEndpoint.GetBySystemName (CognitoRegionInputField.text) // Region
			);
		}

		/// <summary>
		/// Login to Cognito with the Facebook token
		/// </summary>
		/// <param name="facebookToken">Facebook token.</param>
		private void CognitoLogin ()
		{
			Log ("FB Cognito auth");
			credentials.AddLogin ("graph.facebook.com", AccessToken.CurrentAccessToken.TokenString);
			credentials.GetCredentialsAsync (CognitoGetCredentialsCallback, null);

		}

		private void CognitoGetCredentialsCallback (AmazonCognitoIdentityResult<ImmutableCredentials> result)
		{
			if (result.Exception == null) {
				Log (string.Format ("Cognito credentials: {0},\n{1},\n,{2}", result.Response.AccessKey, result.Response.SecretKey, result.Response.Token));
				immutableCredentials = result.Response;
			} else
				Log (result.Exception);
		}

		#endregion


		#region General Methods

		private void Log (string msg)
		{
			Debug.Log (msg);
			SimpleLogInputField.text += "\n>\t" + msg;
		}

		private void Log (System.Exception exception)
		{
			Debug.LogException (exception);
			Log (exception.Message);
		}

		private string BuildPath(string suffixUrl)
		{
			// remove either http or https suffix and a final slash if exists.
			string prefix = "://";

			int prefixPos = ApiGatewayUrlInputField.text.IndexOf (prefix, 0, "https://".Length);

			int startIdx = prefixPos == -1 ? 0 : (ApiGatewayUrlInputField.text.IndexOf (prefix, 0, "https://".Length) + prefix.Length); 
			int endIdx = ApiGatewayUrlInputField.text.Length + (ApiGatewayUrlInputField.text.EndsWith ("/") ? -1 : 0);
			return ApiGatewayUrlInputField.text.Substring (startIdx,endIdx) + "/" + suffixUrl;
		}


		private void Send(WWW www, UnityAction<WWW> callback)
		{
			Log ("Message Sent!");
			StartCoroutine (SendCoroutine(www, callback));
		}


		private IEnumerator SendCoroutine(WWW www, UnityAction<WWW> callback)
		{
			yield return www;
			callback.Invoke (www);
		}



		#endregion


	}
}
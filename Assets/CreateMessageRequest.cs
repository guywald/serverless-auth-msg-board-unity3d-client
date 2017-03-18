using System;

namespace com.guywald.examples.unity.awsv4signer
{
	[System.Serializable]
	public class CreateMessageRequest
	{
		public string Message;

		public CreateMessageRequest ()
		{
		}

		public CreateMessageRequest (string message)
		{
			this.Message = message;
		}
	}
}


using UnityEngine;
using System.Collections;
using System.Collections.Generic; //for List<> and Dictionary<>
using Boomlagoon.JSON;
using System;
using System.Text;

namespace CraftedInc.Appcrafted
{
	class Container {
		public Dictionary<string, Asset> assets 
			= new Dictionary<string, Asset>();
	}
	class Asset {
		public Dictionary<string, object> attributes 
			= new Dictionary<string, object>();
	}
	class AppcraftedManager : MonoBehaviour { //MonoBehaviour required for coroutine
		private string endpoint = "http://api.appcrafted.com/v0/assets/";
		private string accessKey;
		private string secretKey;
		private Dictionary<string, Container> containers = new Dictionary<string, Container>();
		public delegate	void AssetDelegate(Asset asset);
		public static event AssetDelegate OnLoaded;

		//Create a singleton that doesn't need to be attached to a gameobject
		#region Create Singleton
		private static AppcraftedManager instance = null;
		public AppcraftedManager()
		{
			if (instance !=null)
			{
				Debug.LogError ("Cannot have two instances of singleton.");
				return;
			}
			instance = this;
		}
		public static AppcraftedManager Instance
		{
			get
			{
				if (instance == null)
				{
					// component-based approach to use coroutine
					Debug.Log ("instantiate a AppcraftedManager");
					GameObject go = new GameObject();
					instance = go.AddComponent<AppcraftedManager>();
					go.name = "AppcraftedManager";
					
				}
				return instance;
			}
		}
		#endregion

		//a public method to register credentials
		public void RegisterCredentials(string accessKey, string secretKey) {
			this.accessKey = accessKey;
			this.secretKey = secretKey;
		}

		//Retrieves the specified Asset.
		public void GetAsset(string containerID, string assetID){
			try {
					//trigger event OnLoaded
					if (OnLoaded != null) {
						OnLoaded(this.containers[containerID].assets[assetID]);
					}
			}
			catch (KeyNotFoundException e){
				StartCoroutine(RetrieveAsset(containerID, assetID));
			}
			 
		}

		//Reset container.
		public void Reset(string containerID, string assetID){
			containers.Clear();
//			UnloadImages()
			StartCoroutine(RetrieveAsset(containerID, assetID));
		}


		//retrive all assets in a container 
		private IEnumerator RetrieveAsset(string containerID, string assetID) {
			//add container
			Container container = new Container();
			this.containers.Add(containerID, container);

			//validate credentials
			if (this.accessKey == null || this.secretKey == null) {
				throw new System.MemberAccessException("missing credentials");
			}
			Hashtable headers = new Hashtable();
			headers["Authorization"] = "Basic " +
				System.Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes(
					this.accessKey + ":" + this.secretKey));
			string url = this.endpoint + containerID + "/all";			
			WWW www = new WWW(url, null, headers);
			yield return www;

			JSONObject containerJSON = JSONObject.Parse(www.text); 

			//adding assets
			for (int i = 0; i < containerJSON.GetArray("Assets").Length; i++){

				JSONObject assetJSON = JSONObject.Parse(containerJSON.GetArray("Assets")[i].ToString());

				Asset asset = new Asset();
				string currentAssetID = assetJSON.GetString("AssetID");
				this.containers[containerID].assets.Add(currentAssetID, asset);
				Debug.Log ("--- New asset: " + currentAssetID + " ---");

				//adding attributes
				foreach (var keyValuePair in assetJSON.values) {
//					Debug.Log ("Key: " + keyValuePair.Key + "\nValue: " + keyValuePair.Value);
					if (keyValuePair.Value.Type == JSONValueType.Object){ //this is how we know if the pair is an attribute pair and not meta data

						string attributeName = keyValuePair.Key;

						JSONObject attributeJSON = JSONObject.Parse(keyValuePair.Value.ToString());

						//process attributes based on Type
						string type = attributeJSON.GetString("Type");
						switch (type)
						{
						case "URL":
						case "STRING":
							string value = attributeJSON.GetString ("Value");
							this.containers[containerID].assets[currentAssetID]
							.attributes.Add(attributeName, value);
							Debug.Log (attributeName + " : " 
							           + this.containers[containerID]
							           .assets[currentAssetID]
							           .attributes[attributeName]);
							break;
						case "IMAGE":
							string imageURL = attributeJSON.GetString ("Value");
							WWW imageObject = new WWW(imageURL); 
							yield return imageObject;
							Texture2D image = new Texture2D(imageObject.texture.width, imageObject.texture.height);
							imageObject.LoadImageIntoTexture(image);
							this.containers[containerID].assets[currentAssetID]
							.attributes.Add(attributeName, image);
							Debug.Log (attributeName + " : " 
							           + this.containers[containerID]
							           .assets[currentAssetID]
							           .attributes[attributeName]);
							break;
						case "NUMBER":
							double number = attributeJSON.GetNumber("Value");
							this.containers[containerID].assets[currentAssetID]
							.attributes.Add(attributeName, number);
							Debug.Log (attributeName + " : " 
							           + this.containers[containerID]
							           .assets[currentAssetID]
							           .attributes[attributeName]);
							break;
						case "FILE":
							object file = attributeJSON.GetObject("Value");
							this.containers[containerID].assets[currentAssetID]
							.attributes.Add(attributeName, file);
							Debug.Log (attributeName + " : " 
							           + this.containers[containerID]
							           .assets[currentAssetID]
							           .attributes[attributeName]);
							break;
						case "NUMBER_ARRAY":
							int numberArrayLength = attributeJSON.GetArray("Value").Length;
							double[] number_array = new double[numberArrayLength];
							for (int j = 0; j<numberArrayLength ;j++){
								number_array[j] = attributeJSON.GetArray("Value")[j].Number ;
							}
							this.containers[containerID].assets[currentAssetID]
							.attributes.Add(attributeName, number_array);
							break;
						case "STRING_ARRAY":
							int stringArrayLength = attributeJSON.GetArray("Value").Length;
							string[] string_array = new string[stringArrayLength];
							for (int k = 0; k<stringArrayLength ;k++){
								string_array[k] = attributeJSON.GetArray("Value")[k].Str ;
							}
							this.containers[containerID].assets[currentAssetID]
							.attributes.Add(attributeName, string_array);
							break;
						}
					}
				}
			}

			//trigger event OnLoaded
			if (OnLoaded != null) {
				OnLoaded(this.containers[containerID].assets[assetID]); 
			}

		}

	}

}
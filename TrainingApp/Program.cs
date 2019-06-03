using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Azure.CognitiveServices.FormRecognizer;
using Microsoft.Azure.CognitiveServices.FormRecognizer.Models;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;

namespace TrainingApp
{
	class Program
	{
		static void Main()
		{
			// Configure these options
			string storageAccountConnectionString = "";
			string pathToImages = @"C:\temp\images";
			string formRecogniserKey = "";
			string formRecogniserEndpoint = "";

			string containerName = "formsimages" + DateTime.Now.ToString("yyyyMMddhhmmss");

			// Connect and setup our storage accound
			CloudStorageAccount storageAccount = CloudStorageAccount.Parse(storageAccountConnectionString);
			CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
			CloudBlobContainer container = blobClient.GetContainerReference(containerName);
			
			// Create the container if it doesn't exists
			container.CreateIfNotExists();

			// Upload our images
			foreach(string imageFile in Directory.EnumerateFiles(pathToImages))
			{
				string imageFileName = Path.GetFileName(imageFile);

				Console.WriteLine("Uploading " + imageFileName);

				CloudBlockBlob imageBlob = container.GetBlockBlobReference(imageFileName);
				imageBlob.UploadFromFile(imageFile);
			}

			// Create a SAS token for our storage account container
			//  This should have list and read permissions
			SharedAccessBlobPolicy sasPolicy = new SharedAccessBlobPolicy {
				Permissions = SharedAccessBlobPermissions.List | SharedAccessBlobPermissions.Read,
				SharedAccessStartTime = DateTimeOffset.UtcNow.AddMinutes(-5),
				SharedAccessExpiryTime = DateTimeOffset.UtcNow.AddMinutes(60)
			};
			string sasToken = container.GetSharedAccessSignature(sasPolicy);

			string fullSasUrl = container.Uri + sasToken;

			// Setup our Forms Recognizer client
			using (FormRecognizerClient frClient = new FormRecognizerClient(new ApiKeyServiceClientCredentials(formRecogniserKey))
			{
				Endpoint = formRecogniserEndpoint
			})
			{
				Console.WriteLine("Training model...");

				TrainResult frModel = frClient.TrainCustomModelAsync(new TrainRequest(fullSasUrl)).Result;

				Console.WriteLine($"Model with id '{frModel.ModelId}' has been trained\n");
				Console.WriteLine(FormatTrainingDocumentsString(frModel.TrainingDocuments));
				Console.ReadLine();
			}
			
		}

		private static string FormatTrainingDocumentsString(IList<FormDocumentReport> documents) =>
			string.Join('\n', documents.Select(x => $" > {x.DocumentName} - {x.Status}, {x.Pages ?? 1} pages; {string.Join(", ", x.Errors)}"));
	}
}

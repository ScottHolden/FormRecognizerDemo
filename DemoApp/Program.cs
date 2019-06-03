using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.CognitiveServices.FormRecognizer;
using Microsoft.Azure.CognitiveServices.FormRecognizer.Models;
using Newtonsoft.Json;

namespace DemoApp
{
	class Program
	{
		static async Task Main()
		{
			// Setup what images to analyze, and FR settings
			string pathToImages = @"C:\temp\images";
			string formRecogniserKey = "";
			string formRecogniserEndpoint = "";
			string formRecogniserModelId = "";

			using (FormRecognizerClient frClient = new FormRecognizerClient(new ApiKeyServiceClientCredentials(formRecogniserKey))
			{
				Endpoint = formRecogniserEndpoint
			})
			{
				Guid formRecogniserModelGuid = Guid.Parse(formRecogniserModelId);
				foreach (string imageFile in Directory.EnumerateFiles(pathToImages))
				{
					string imageFileName = Path.GetFileName(imageFile);

					Console.WriteLine("Analyzing " + imageFileName);

					using(Stream fs = File.OpenRead(imageFile))
					{
						AnalyzeResult details = await frClient.AnalyzeWithCustomModelAsync(formRecogniserModelGuid, fs);

						Console.WriteLine($" > {details.Status} {string.Join(", ", details.Errors.Select(x=>x.ErrorMessage))}");

						foreach(ExtractedPage p in details.Pages)
						{
							Console.WriteLine($"---- Page {p.Number??-1} ----");
							Console.WriteLine(JsonConvert.SerializeObject(p));
						}

					}
					Console.WriteLine("----------------");
				}

				Console.WriteLine("Done!");
				Console.ReadLine();
			}
		}
	}
}
